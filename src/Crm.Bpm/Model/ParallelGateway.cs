namespace Crm.Bpm.Model;

/// Fork or join is inferred from the flow graph: more than one incoming flow means join.
public sealed class ParallelGateway : ProcessElement
{
}
