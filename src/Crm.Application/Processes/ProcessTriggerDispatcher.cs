using Crm.Application.Abstractions;
using Crm.Bpm.Abstractions;
using Crm.Bpm.Engine;
using Crm.Bpm.Expressions;
using Crm.Bpm.Model;
using Crm.Bpm.Runtime;
using Crm.Domain.Common;
using Crm.Domain.Events;

namespace Crm.Application.Processes;

/// Turns domain events into process starts. Events arrive from the outbox, never inline with the
/// user request, so a slow or failing process can never break the record being saved.
public sealed class ProcessTriggerDispatcher : IDomainEventDispatcher
{
    private readonly IProcessDefinitionStore _definitions;
    private readonly IProcessEngine _engine;
    private readonly IExpressionEvaluator _expressions;

    public ProcessTriggerDispatcher(
        IProcessDefinitionStore definitions,
        IProcessEngine engine,
        IExpressionEvaluator expressions)
    {
        _definitions = definitions;
        _engine = engine;
        _expressions = expressions;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
                case RecordCreatedEvent created:
                    await StartMatchingAsync(TriggerKind.RecordCreated, created.EntityName, null,
                        new EntityReference(created.EntityName, created.RecordId), null, cancellationToken);
                    break;

                case FieldChangedEvent changed:
                    await StartMatchingAsync(TriggerKind.FieldChanged, changed.EntityName, changed.FieldName,
                        new EntityReference(changed.EntityName, changed.RecordId),
                        new Dictionary<string, object?>
                        {
                            ["field"] = changed.FieldName,
                            ["oldValue"] = changed.OldValue,
                            ["newValue"] = changed.NewValue
                        },
                        cancellationToken);
                    break;

                case StageChangedEvent stage:
                    await StartMatchingAsync(TriggerKind.FieldChanged, "Opportunity", "StageId",
                        new EntityReference("Opportunity", stage.OpportunityId),
                        new Dictionary<string, object?>
                        {
                            ["field"] = "StageId",
                            ["oldValue"] = stage.FromStageId,
                            ["newValue"] = stage.ToStageId
                        },
                        cancellationToken);
                    break;
            }
        }
    }

    private async Task StartMatchingAsync(
        TriggerKind kind,
        string entityName,
        string? fieldName,
        EntityReference subject,
        Dictionary<string, object?>? variables,
        CancellationToken cancellationToken)
    {
        var definitions = await _definitions.GetActiveByTriggerAsync(kind, entityName, cancellationToken);

        foreach (var definition in definitions)
        {
            var trigger = definition.Start.Trigger;

            if (fieldName is not null
                && !string.IsNullOrWhiteSpace(trigger.FieldName)
                && !string.Equals(trigger.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(trigger.Condition))
            {
                var context = new ExpressionContext
                {
                    Variables = variables ?? new Dictionary<string, object?>()
                };

                if (!_expressions.EvaluateBoolean(trigger.Condition, context))
                {
                    continue;
                }
            }

            await _engine.StartAsync(definition.Key, subject, variables, cancellationToken);
        }
    }
}
