namespace Crm.Bpm.Model;

public sealed class ProcessDefinition
{
    private readonly Dictionary<string, ProcessElement> _elements;
    private readonly Dictionary<string, int> _incomingCounts;

    public ProcessDefinition(string key, int version, string name, IEnumerable<ProcessElement> elements)
    {
        Key = key;
        Version = version;
        Name = name;

        _elements = elements.ToDictionary(e => e.Id);
        _incomingCounts = _elements.Values
            .SelectMany(e => e.Outgoing)
            .GroupBy(f => f.TargetElementId)
            .ToDictionary(g => g.Key, g => g.Count());

        Start = _elements.Values.OfType<StartEvent>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Process '{key}' has no start event.");
    }

    public string Key { get; }
    public int Version { get; }
    public string Name { get; }
    public StartEvent Start { get; }

    public IReadOnlyCollection<ProcessElement> Elements => _elements.Values;

    public ProcessElement GetElement(string id) =>
        _elements.TryGetValue(id, out var element)
            ? element
            : throw new InvalidOperationException($"Element '{id}' is not part of process '{Key}' v{Version}.");

    public int IncomingCount(string elementId) => _incomingCounts.GetValueOrDefault(elementId);

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        foreach (var element in _elements.Values)
        {
            foreach (var flow in element.Outgoing.Where(flow => !_elements.ContainsKey(flow.TargetElementId)))
            {
                errors.Add($"Flow '{flow.Id}' from '{element.Id}' points to unknown element '{flow.TargetElementId}'.");
            }

            if (element is not EndEvent && element.Outgoing.Count == 0)
            {
                errors.Add($"Element '{element.Id}' has no outgoing flow.");
            }

            if (element is ExclusiveGateway gateway && gateway.Outgoing.Count(f => f.IsDefault) > 1)
            {
                errors.Add($"Gateway '{element.Id}' has more than one default flow.");
            }
        }

        if (!_elements.Values.OfType<EndEvent>().Any())
        {
            errors.Add("Process has no end event.");
        }

        return errors;
    }
}
