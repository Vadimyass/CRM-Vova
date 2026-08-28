namespace Crm.Bpm.Expressions;

public interface IExpressionEvaluator
{
    object? Evaluate(string expression, ExpressionContext context);

    bool EvaluateBoolean(string expression, ExpressionContext context);
}
