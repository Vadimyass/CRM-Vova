using System.Globalization;

namespace Crm.Bpm.Expressions;

/// Deliberately small: comparisons, boolean logic and dotted paths, nothing else.
/// User-authored code is never compiled at runtime - that is an assembly leak and a security hole.
public sealed class SimpleExpressionEvaluator : IExpressionEvaluator
{
    public object? Evaluate(string expression, ExpressionContext context)
    {
        var tokens = Tokenize(expression);
        var parser = new Parser(tokens, context);
        var value = parser.ParseExpression();
        parser.ExpectEnd();
        return value;
    }

    public bool EvaluateBoolean(string expression, ExpressionContext context) =>
        Evaluate(expression, context) switch
        {
            null => false,
            bool flag => flag,
            double number => number != 0,
            string text => !string.IsNullOrEmpty(text),
            _ => true
        };

    private static List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        var position = 0;

        while (position < source.Length)
        {
            var current = source[position];

            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            if (char.IsDigit(current))
            {
                var start = position;
                while (position < source.Length && (char.IsDigit(source[position]) || source[position] == '.'))
                {
                    position++;
                }

                tokens.Add(new Token(TokenType.Number, source[start..position]));
                continue;
            }

            if (current is '\'' or '"')
            {
                var quote = current;
                position++;
                var start = position;
                while (position < source.Length && source[position] != quote)
                {
                    position++;
                }

                if (position >= source.Length)
                {
                    throw new ExpressionException("Unterminated string literal.");
                }

                tokens.Add(new Token(TokenType.String, source[start..position]));
                position++;
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                var start = position;
                while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] is '_' or '.'))
                {
                    position++;
                }

                tokens.Add(new Token(TokenType.Identifier, source[start..position]));
                continue;
            }

            var twoChars = position + 1 < source.Length ? source.Substring(position, 2) : string.Empty;
            if (twoChars is "==" or "!=" or ">=" or "<=" or "&&" or "||")
            {
                tokens.Add(new Token(TokenType.Operator, twoChars));
                position += 2;
                continue;
            }

            if (current is '>' or '<' or '!' or '(' or ')')
            {
                tokens.Add(new Token(current is '(' or ')' ? TokenType.Paren : TokenType.Operator, current.ToString()));
                position++;
                continue;
            }

            throw new ExpressionException($"Unexpected character '{current}' at position {position}.");
        }

        tokens.Add(new Token(TokenType.End, string.Empty));
        return tokens;
    }

    private enum TokenType
    {
        Number,
        String,
        Identifier,
        Operator,
        Paren,
        End
    }

    private readonly record struct Token(TokenType Type, string Text);

    private sealed class Parser(List<Token> tokens, ExpressionContext context)
    {
        private int _position;

        public object? ParseExpression() => ParseOr();

        public void ExpectEnd()
        {
            if (Current.Type != TokenType.End)
            {
                throw new ExpressionException($"Unexpected trailing token '{Current.Text}'.");
            }
        }

        private Token Current => tokens[_position];

        private object? ParseOr()
        {
            var left = ParseAnd();
            while (Current is { Type: TokenType.Operator, Text: "||" })
            {
                _position++;
                var right = ParseAnd();
                left = Truthy(left) || Truthy(right);
            }

            return left;
        }

        private object? ParseAnd()
        {
            var left = ParseComparison();
            while (Current is { Type: TokenType.Operator, Text: "&&" })
            {
                _position++;
                var right = ParseComparison();
                left = Truthy(left) && Truthy(right);
            }

            return left;
        }

        private object? ParseComparison()
        {
            var left = ParseUnary();

            while (Current.Type == TokenType.Operator && Current.Text is "==" or "!=" or ">" or "<" or ">=" or "<=")
            {
                var op = Current.Text;
                _position++;
                var right = ParseUnary();
                left = Compare(left, right, op);
            }

            return left;
        }

        private object? ParseUnary()
        {
            if (Current is { Type: TokenType.Operator, Text: "!" })
            {
                _position++;
                return !Truthy(ParseUnary());
            }

            return ParsePrimary();
        }

        private object? ParsePrimary()
        {
            var token = Current;

            switch (token.Type)
            {
                case TokenType.Number:
                    _position++;
                    return double.Parse(token.Text, CultureInfo.InvariantCulture);

                case TokenType.String:
                    _position++;
                    return token.Text;

                case TokenType.Identifier:
                    _position++;
                    return token.Text switch
                    {
                        "true" => true,
                        "false" => false,
                        "null" => null,
                        _ => context.Resolve(token.Text.Split('.'))
                    };

                case TokenType.Paren when token.Text == "(":
                    _position++;
                    var inner = ParseExpression();
                    if (Current.Text != ")")
                    {
                        throw new ExpressionException("Missing closing parenthesis.");
                    }

                    _position++;
                    return inner;

                default:
                    throw new ExpressionException($"Unexpected token '{token.Text}'.");
            }
        }

        private static bool Truthy(object? value) => value switch
        {
            null => false,
            bool flag => flag,
            double number => number != 0,
            string text => !string.IsNullOrEmpty(text),
            _ => true
        };

        private static object Compare(object? left, object? right, string op)
        {
            if (op is "==" or "!=")
            {
                var equal = AreEqual(left, right);
                return op == "==" ? equal : !equal;
            }

            if (!TryToDouble(left, out var leftNumber) || !TryToDouble(right, out var rightNumber))
            {
                throw new ExpressionException($"Operator '{op}' requires numeric operands.");
            }

            return op switch
            {
                ">" => leftNumber > rightNumber,
                "<" => leftNumber < rightNumber,
                ">=" => leftNumber >= rightNumber,
                "<=" => leftNumber <= rightNumber,
                _ => throw new ExpressionException($"Unknown operator '{op}'.")
            };
        }

        private static bool AreEqual(object? left, object? right)
        {
            if (left is null || right is null)
            {
                return left is null && right is null;
            }

            if (TryToDouble(left, out var leftNumber) && TryToDouble(right, out var rightNumber))
            {
                return Math.Abs(leftNumber - rightNumber) < 0.000001;
            }

            return string.Equals(Stringify(left), Stringify(right), StringComparison.Ordinal);
        }

        private static string Stringify(object value) =>
            Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

        private static bool TryToDouble(object? value, out double result)
        {
            switch (value)
            {
                case null:
                    result = 0;
                    return false;
                case bool:
                    result = 0;
                    return false;
                case double number:
                    result = number;
                    return true;
                case IConvertible convertible and not string:
                    try
                    {
                        result = convertible.ToDouble(CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch (Exception)
                    {
                        result = 0;
                        return false;
                    }
                case string text:
                    return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                default:
                    result = 0;
                    return false;
            }
        }
    }
}
