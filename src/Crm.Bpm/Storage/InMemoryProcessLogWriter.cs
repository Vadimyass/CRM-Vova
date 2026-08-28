using Crm.Bpm.Abstractions;
using Crm.Bpm.Runtime;

namespace Crm.Bpm.Storage;

public sealed class InMemoryProcessLogWriter : IProcessLogWriter
{
    private readonly List<ProcessLogEntry> _entries = [];

    public IReadOnlyList<ProcessLogEntry> Entries => _entries;

    public Task WriteAsync(ProcessLogEntry entry, CancellationToken cancellationToken = default)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }
}
