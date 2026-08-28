using Crm.Bpm.Runtime;

namespace Crm.Bpm.Abstractions;

public interface IProcessLogWriter
{
    Task WriteAsync(ProcessLogEntry entry, CancellationToken cancellationToken = default);
}
