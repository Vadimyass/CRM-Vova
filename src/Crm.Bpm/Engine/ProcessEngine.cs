using Crm.Bpm.Abstractions;
using Crm.Bpm.Expressions;
using Crm.Bpm.Model;
using Crm.Bpm.Runtime;

namespace Crm.Bpm.Engine;

/// Token-based executor over a BPMN subset. One step = take an active token, run its element,
/// move the token, append to the log. Steps are idempotent so a retry after a crash cannot
/// run the same service task twice.
public sealed class ProcessEngine : IProcessEngine
{
    private readonly IProcessDefinitionStore _definitions;
    private readonly IProcessInstanceStore _instances;
    private readonly IServiceTaskRegistry _serviceTasks;
    private readonly IUserTaskGateway _userTasks;
    private readonly ITimerScheduler _timers;
    private readonly IExpressionEvaluator _expressions;
    private readonly IProcessLogWriter _log;
    private readonly IClock _clock;
    private readonly ProcessEngineOptions _options;

    public ProcessEngine(
        IProcessDefinitionStore definitions,
        IProcessInstanceStore instances,
        IServiceTaskRegistry serviceTasks,
        IUserTaskGateway userTasks,
        ITimerScheduler timers,
        IExpressionEvaluator expressions,
        IProcessLogWriter log,
        IClock clock,
        ProcessEngineOptions? options = null)
    {
        _definitions = definitions;
        _instances = instances;
        _serviceTasks = serviceTasks;
        _userTasks = userTasks;
        _timers = timers;
        _expressions = expressions;
        _log = log;
        _clock = clock;
        _options = options ?? new ProcessEngineOptions();
    }

    public async Task<ProcessInstance> StartAsync(
        string definitionKey,
        EntityReference? subject = null,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var definition = await _definitions.GetActiveAsync(definitionKey, cancellationToken)
            ?? throw new InvalidOperationException($"No active process definition for key '{definitionKey}'.");

        var instance = new ProcessInstance
        {
            DefinitionKey = definition.Key,
            DefinitionVersion = definition.Version,
            Subject = subject,
            StartedOn = _clock.UtcNow
        };

        if (variables is not null)
        {
            foreach (var (name, value) in variables)
            {
                instance.Variables[name] = value;
            }
        }

        instance.Tokens.Add(new ProcessToken { InstanceId = instance.Id, ElementId = definition.Start.Id });

        await _instances.AddAsync(instance, cancellationToken);
        await LogAsync(instance.Id, null, definition.Start.Id, "InstanceStarted", definition.Key, cancellationToken);

        await RunAsync(instance, definition, cancellationToken);
        return instance;
    }

    public async Task<ProcessInstance> ResumeAsync(
        Guid tokenId,
        IDictionary<string, object?>? outputs = null,
        CancellationToken cancellationToken = default)
    {
        var instance = await _instances.GetByTokenAsync(tokenId, cancellationToken)
            ?? throw new InvalidOperationException($"No process instance holds token '{tokenId}'.");

        var token = instance.Tokens.Single(t => t.Id == tokenId);

        if (token.Status != TokenStatus.Waiting)
        {
            // Resuming a token twice is a normal race between a user action and a timer, not an error.
            return instance;
        }

        if (outputs is not null)
        {
            foreach (var (name, value) in outputs)
            {
                instance.Variables[name] = value;
            }
        }

        var definition = await GetDefinitionAsync(instance, cancellationToken);
        var waited = token.WaitKind;

        token.Status = TokenStatus.Active;
        token.WaitKind = WaitKind.None;
        token.WaitKey = null;

        var current = definition.GetElement(token.ElementId);
        var targets = current.Outgoing.Select(f => f.TargetElementId).ToList();

        await LogAsync(instance.Id, token.Id, token.ElementId, $"Resumed:{waited}", null, cancellationToken);

        // A resumed wait element is already done - move straight to its successors.
        Apply(ElementOutcome.Continue(targets), token, instance);

        instance.Status = ProcessInstanceStatus.Running;
        await RunAsync(instance, definition, cancellationToken);
        return instance;
    }

    public async Task<ProcessInstance> CancelAsync(Guid instanceId, string reason, CancellationToken cancellationToken = default)
    {
        var instance = await _instances.GetAsync(instanceId, cancellationToken)
            ?? throw new InvalidOperationException($"Process instance '{instanceId}' not found.");

        foreach (var token in instance.Tokens.Where(t => t.Status is TokenStatus.Active or TokenStatus.Waiting))
        {
            if (token.WaitKind == WaitKind.Timer)
            {
                await _timers.CancelAsync(token.Id, cancellationToken);
            }

            token.Status = TokenStatus.Completed;
        }

        instance.Status = ProcessInstanceStatus.Cancelled;
        instance.CompletedOn = _clock.UtcNow;
        instance.Error = reason;

        await _instances.SaveAsync(instance, cancellationToken);
        await LogAsync(instance.Id, null, string.Empty, "InstanceCancelled", reason, cancellationToken);
        return instance;
    }

    private async Task<ProcessDefinition> GetDefinitionAsync(ProcessInstance instance, CancellationToken cancellationToken) =>
        await _definitions.GetAsync(instance.DefinitionKey, instance.DefinitionVersion, cancellationToken)
        ?? throw new InvalidOperationException(
            $"Process definition '{instance.DefinitionKey}' v{instance.DefinitionVersion} is missing; running instances pin their version and it must never be deleted.");

    private async Task RunAsync(ProcessInstance instance, ProcessDefinition definition, CancellationToken cancellationToken)
    {
        var steps = 0;

        while (instance.Status == ProcessInstanceStatus.Running)
        {
            var token = instance.Tokens.FirstOrDefault(t => t.Status == TokenStatus.Active);
            if (token is null)
            {
                break;
            }

            if (++steps > _options.MaxStepsPerRun)
            {
                instance.Status = ProcessInstanceStatus.Failed;
                instance.Error = $"Step limit of {_options.MaxStepsPerRun} exceeded - the process is probably looping.";
                await LogAsync(instance.Id, token.Id, token.ElementId, "StepLimitExceeded", instance.Error, cancellationToken);
                break;
            }

            var element = definition.GetElement(token.ElementId);
            ElementOutcome outcome;

            try
            {
                outcome = await ExecuteAsync(element, token, instance, definition, cancellationToken);
            }
            catch (Exception exception)
            {
                outcome = ElementOutcome.Fail(exception.Message);
            }

            await LogAsync(instance.Id, token.Id, element.Id, outcome.Kind.ToString(), outcome.Error, cancellationToken);
            Apply(outcome, token, instance);
        }

        UpdateStatus(instance);
        await _instances.SaveAsync(instance, cancellationToken);
    }

    private async Task<ElementOutcome> ExecuteAsync(
        ProcessElement element,
        ProcessToken token,
        ProcessInstance instance,
        ProcessDefinition definition,
        CancellationToken cancellationToken)
    {
        var targets = element.Outgoing.Select(f => f.TargetElementId).ToList();

        switch (element)
        {
            case StartEvent:
                return ElementOutcome.Continue(targets);

            case EndEvent endEvent:
                return ElementOutcome.Finish(endEvent.IsTerminate);

            case ServiceTaskElement serviceTask:
                return await ExecuteServiceTaskAsync(serviceTask, instance, targets, cancellationToken);

            case UserTaskElement userTask:
                return await ExecuteUserTaskAsync(userTask, token, instance, cancellationToken);

            case TimerElement timer:
                return await ExecuteTimerAsync(timer, token, instance, cancellationToken);

            case ExclusiveGateway gateway:
                return ExecuteExclusiveGateway(gateway, instance);

            case ParallelGateway parallel:
                return ExecuteParallelGateway(parallel, token, instance, definition, targets);

            default:
                return ElementOutcome.Fail($"Element type '{element.GetType().Name}' is not supported.");
        }
    }

    private async Task<ElementOutcome> ExecuteServiceTaskAsync(
        ServiceTaskElement element,
        ProcessInstance instance,
        IReadOnlyList<string> targets,
        CancellationToken cancellationToken)
    {
        var handler = _serviceTasks.Resolve(element.HandlerKey);
        var context = BuildExpressionContext(instance);

        var parameters = element.Parameters.ToDictionary(
            pair => pair.Key,
            pair => _expressions.Evaluate(pair.Value, context));

        var outputs = await handler.ExecuteAsync(
            new ServiceTaskContext
            {
                InstanceId = instance.Id,
                ElementId = element.Id,
                Subject = instance.Subject,
                Parameters = parameters,
                Variables = instance.Variables
            },
            cancellationToken);

        if (outputs is not null)
        {
            foreach (var (name, value) in outputs)
            {
                instance.Variables[name] = value;
            }
        }

        return ElementOutcome.Continue(targets);
    }

    private async Task<ElementOutcome> ExecuteUserTaskAsync(
        UserTaskElement element,
        ProcessToken token,
        ProcessInstance instance,
        CancellationToken cancellationToken)
    {
        var context = BuildExpressionContext(instance);

        Guid? assigneeId = null;
        if (!string.IsNullOrWhiteSpace(element.AssigneeExpression))
        {
            var value = _expressions.Evaluate(element.AssigneeExpression, context);
            assigneeId = value switch
            {
                Guid id => id,
                string text when Guid.TryParse(text, out var parsed) => parsed,
                _ => null
            };
        }

        var taskId = await _userTasks.CreateAsync(
            new UserTaskRequest(
                instance.Id,
                token.Id,
                element.Id,
                element.TitleTemplate,
                assigneeId,
                element.RoleCode,
                element.DueInMinutes.HasValue ? _clock.UtcNow.AddMinutes(element.DueInMinutes.Value) : null,
                element.FormKey,
                instance.Subject),
            cancellationToken);

        return ElementOutcome.Wait(WaitKind.UserTask, taskId.ToString());
    }

    private async Task<ElementOutcome> ExecuteTimerAsync(
        TimerElement element,
        ProcessToken token,
        ProcessInstance instance,
        CancellationToken cancellationToken)
    {
        var fireAt = _clock.UtcNow;

        if (!string.IsNullOrWhiteSpace(element.DueDateExpression))
        {
            var value = _expressions.Evaluate(element.DueDateExpression, BuildExpressionContext(instance));
            fireAt = value switch
            {
                DateTimeOffset offset => offset,
                DateTime dateTime => new DateTimeOffset(dateTime.ToUniversalTime(), TimeSpan.Zero),
                string text when DateTimeOffset.TryParse(text, out var parsed) => parsed,
                _ => throw new InvalidOperationException($"Timer '{element.Id}' produced a non-date value.")
            };
        }
        else if (element.Delay.HasValue)
        {
            fireAt = _clock.UtcNow.Add(element.Delay.Value);
        }

        await _timers.ScheduleAsync(instance.Id, token.Id, fireAt, cancellationToken);
        return ElementOutcome.Wait(WaitKind.Timer, fireAt.ToString("O"));
    }

    private ElementOutcome ExecuteExclusiveGateway(ExclusiveGateway element, ProcessInstance instance)
    {
        var context = BuildExpressionContext(instance);

        foreach (var flow in element.Outgoing.Where(f => !f.IsDefault && !string.IsNullOrWhiteSpace(f.Condition)))
        {
            if (_expressions.EvaluateBoolean(flow.Condition!, context))
            {
                return ElementOutcome.Continue([flow.TargetElementId]);
            }
        }

        var defaultFlow = element.Outgoing.FirstOrDefault(f => f.IsDefault);
        return defaultFlow is not null
            ? ElementOutcome.Continue([defaultFlow.TargetElementId])
            : ElementOutcome.Fail($"Gateway '{element.Id}': no condition matched and no default flow is defined.");
    }

    private static ElementOutcome ExecuteParallelGateway(
        ParallelGateway element,
        ProcessToken token,
        ProcessInstance instance,
        ProcessDefinition definition,
        IReadOnlyList<string> targets)
    {
        var incoming = definition.IncomingCount(element.Id);

        if (incoming <= 1)
        {
            return ElementOutcome.Continue(targets);
        }

        var siblings = instance.Tokens
            .Where(t => t.Id != token.Id && t.ElementId == element.Id && t.Status == TokenStatus.Waiting && t.WaitKind == WaitKind.Join)
            .ToList();

        if (siblings.Count + 1 < incoming)
        {
            return ElementOutcome.Wait(WaitKind.Join, element.Id);
        }

        foreach (var sibling in siblings)
        {
            sibling.Status = TokenStatus.Completed;
            sibling.WaitKind = WaitKind.None;
            sibling.WaitKey = null;
        }

        return ElementOutcome.Continue(targets);
    }

    private static void Apply(ElementOutcome outcome, ProcessToken token, ProcessInstance instance)
    {
        switch (outcome.Kind)
        {
            case OutcomeKind.Continue when outcome.Targets.Count == 1:
                token.ElementId = outcome.Targets[0];
                token.Status = TokenStatus.Active;
                break;

            case OutcomeKind.Continue when outcome.Targets.Count > 1:
                token.Status = TokenStatus.Completed;
                foreach (var target in outcome.Targets)
                {
                    instance.Tokens.Add(new ProcessToken
                    {
                        InstanceId = instance.Id,
                        ParentTokenId = token.Id,
                        ElementId = target
                    });
                }

                break;

            case OutcomeKind.Continue:
                token.Status = TokenStatus.Completed;
                break;

            case OutcomeKind.Wait:
                token.Status = TokenStatus.Waiting;
                token.WaitKind = outcome.WaitKind;
                token.WaitKey = outcome.WaitKey;
                break;

            case OutcomeKind.Finish:
                token.Status = TokenStatus.Completed;
                if (outcome.Terminate)
                {
                    foreach (var other in instance.Tokens.Where(t => t.Status is TokenStatus.Active or TokenStatus.Waiting))
                    {
                        other.Status = TokenStatus.Completed;
                    }
                }

                break;

            case OutcomeKind.Fail:
                token.Status = TokenStatus.Failed;
                token.Error = outcome.Error;
                token.Attempts++;
                instance.Status = ProcessInstanceStatus.Failed;
                instance.Error = outcome.Error;
                break;
        }
    }

    private void UpdateStatus(ProcessInstance instance)
    {
        if (instance.Status is ProcessInstanceStatus.Failed or ProcessInstanceStatus.Cancelled)
        {
            instance.CompletedOn ??= _clock.UtcNow;
            return;
        }

        if (instance.HasActiveTokens)
        {
            instance.Status = ProcessInstanceStatus.Running;
            return;
        }

        if (instance.HasWaitingTokens)
        {
            instance.Status = ProcessInstanceStatus.Waiting;
            return;
        }

        instance.Status = ProcessInstanceStatus.Completed;
        instance.CompletedOn = _clock.UtcNow;
    }

    private ExpressionContext BuildExpressionContext(ProcessInstance instance) => new()
    {
        Variables = instance.Variables,
        Entity = instance.Variables.TryGetValue("entity", out var entity) && entity is IDictionary<string, object?> map
            ? map
            : new Dictionary<string, object?>(),
        Now = _clock.UtcNow
    };

    private Task LogAsync(Guid instanceId, Guid? tokenId, string elementId, string @event, string? details, CancellationToken cancellationToken) =>
        _log.WriteAsync(new ProcessLogEntry(instanceId, tokenId, elementId, @event, details, _clock.UtcNow), cancellationToken);
}
