namespace Crm.Bpm.Abstractions;

public interface IServiceTaskRegistry
{
    IServiceTaskHandler Resolve(string key);
}
