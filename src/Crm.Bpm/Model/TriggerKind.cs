namespace Crm.Bpm.Model;

public enum TriggerKind
{
    Manual = 0,
    RecordCreated = 1,
    FieldChanged = 2,
    Signal = 3,
    Schedule = 4
}
