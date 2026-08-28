namespace Crm.Bpm.Expressions;

public sealed class ExpressionContext
{
    public IDictionary<string, object?> Variables { get; init; } = new Dictionary<string, object?>();
    public IDictionary<string, object?> Entity { get; init; } = new Dictionary<string, object?>();
    public Guid? UserId { get; init; }
    public DateTimeOffset Now { get; init; } = DateTimeOffset.UtcNow;

    public object? Resolve(IReadOnlyList<string> path)
    {
        if (path.Count == 0)
        {
            return null;
        }

        object? current = path[0] switch
        {
            "vars" => Variables,
            "entity" => Entity,
            "now" => Now,
            "userId" => UserId,
            _ => throw new ExpressionException($"Unknown root '{path[0]}'. Allowed: vars, entity, now, userId.")
        };

        for (var i = 1; i < path.Count; i++)
        {
            if (current is IDictionary<string, object?> map)
            {
                map.TryGetValue(path[i], out current);
                continue;
            }

            return null;
        }

        return current;
    }
}
