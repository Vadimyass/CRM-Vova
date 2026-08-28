using Crm.Bpm.Abstractions;
using Crm.Bpm.Engine;
using Crm.Bpm.Expressions;
using Crm.Bpm.Model;
using Crm.Bpm.Runtime;
using Crm.Bpm.Storage;
using Crm.Bpm.Tests;

Console.WriteLine("Crm.Bpm engine tests");
Console.WriteLine();

await TestHarness.RunAsync("linear process runs to completion", async () =>
{
    var handler = new DelegateServiceTaskHandler("SetVar", _ => new Dictionary<string, object?> { ["greeted"] = true });
    var fixture = new Fixture(handler);

    fixture.Publish(new ProcessDefinition("linear", 1, "Linear", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("work")] },
        new ServiceTaskElement { Id = "work", HandlerKey = "SetVar", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("linear");

    TestHarness.AreEqual(ProcessInstanceStatus.Completed, instance.Status, "instance status");
    TestHarness.AreEqual(1, handler.Calls, "handler call count");
    TestHarness.AreEqual(true, instance.Variables["greeted"], "variable written by handler");
});

await TestHarness.RunAsync("exclusive gateway takes the matching branch", async () =>
{
    var big = new DelegateServiceTaskHandler("Big", _ => null);
    var small = new DelegateServiceTaskHandler("Small", _ => null);
    var fixture = new Fixture(big, small);

    fixture.Publish(new ProcessDefinition("deal", 1, "Deal routing", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("gate")] },
        new ExclusiveGateway
        {
            Id = "gate",
            Outgoing =
            [
                Flows.To("big", "vars.amount > 100000"),
                Flows.To("small", isDefault: true)
            ]
        },
        new ServiceTaskElement { Id = "big", HandlerKey = "Big", Outgoing = [Flows.To("end")] },
        new ServiceTaskElement { Id = "small", HandlerKey = "Small", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var large = await fixture.Engine.StartAsync("deal", variables: new Dictionary<string, object?> { ["amount"] = 250000d });
    var modest = await fixture.Engine.StartAsync("deal", variables: new Dictionary<string, object?> { ["amount"] = 5000d });

    TestHarness.AreEqual(ProcessInstanceStatus.Completed, large.Status, "large deal completes");
    TestHarness.AreEqual(ProcessInstanceStatus.Completed, modest.Status, "small deal completes");
    TestHarness.AreEqual(1, big.Calls, "big branch calls");
    TestHarness.AreEqual(1, small.Calls, "default branch calls");
});

await TestHarness.RunAsync("user task suspends the instance until it is completed", async () =>
{
    var after = new DelegateServiceTaskHandler("After", _ => null);
    var fixture = new Fixture(after);

    fixture.Publish(new ProcessDefinition("approval", 1, "Approval", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("approve")] },
        new UserTaskElement { Id = "approve", TitleTemplate = "Согласовать сделку", RoleCode = "SalesManager", Outgoing = [Flows.To("notify")] },
        new ServiceTaskElement { Id = "notify", HandlerKey = "After", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("approval");

    TestHarness.AreEqual(ProcessInstanceStatus.Waiting, instance.Status, "instance waits on the user task");
    TestHarness.AreEqual(1, fixture.UserTasks.Created.Count, "one user task created");
    TestHarness.AreEqual(0, after.Calls, "downstream task has not run yet");

    var waitingToken = instance.Tokens.Single(t => t.Status == TokenStatus.Waiting);
    var resumed = await fixture.Engine.ResumeAsync(waitingToken.Id, new Dictionary<string, object?> { ["approved"] = true });

    TestHarness.AreEqual(ProcessInstanceStatus.Completed, resumed.Status, "instance completes after approval");
    TestHarness.AreEqual(1, after.Calls, "downstream task ran once");
    TestHarness.AreEqual(true, resumed.Variables["approved"], "task output merged into variables");
});

await TestHarness.RunAsync("parallel gateway forks and joins exactly once", async () =>
{
    var left = new DelegateServiceTaskHandler("Left", _ => null);
    var right = new DelegateServiceTaskHandler("Right", _ => null);
    var afterJoin = new DelegateServiceTaskHandler("AfterJoin", _ => null);
    var fixture = new Fixture(left, right, afterJoin);

    fixture.Publish(new ProcessDefinition("parallel", 1, "Parallel", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("fork")] },
        new ParallelGateway { Id = "fork", Outgoing = [Flows.To("left"), Flows.To("right")] },
        new ServiceTaskElement { Id = "left", HandlerKey = "Left", Outgoing = [Flows.To("join")] },
        new ServiceTaskElement { Id = "right", HandlerKey = "Right", Outgoing = [Flows.To("join")] },
        new ParallelGateway { Id = "join", Outgoing = [Flows.To("after")] },
        new ServiceTaskElement { Id = "after", HandlerKey = "AfterJoin", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("parallel");

    TestHarness.AreEqual(ProcessInstanceStatus.Completed, instance.Status, "instance status");
    TestHarness.AreEqual(1, left.Calls, "left branch runs once");
    TestHarness.AreEqual(1, right.Calls, "right branch runs once");
    TestHarness.AreEqual(1, afterJoin.Calls, "join continues exactly once");
});

await TestHarness.RunAsync("timer parks the token and schedules a wake-up", async () =>
{
    var afterTimer = new DelegateServiceTaskHandler("AfterTimer", _ => null);
    var fixture = new Fixture(afterTimer);

    fixture.Publish(new ProcessDefinition("followup", 1, "Follow-up", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("wait")] },
        new TimerElement { Id = "wait", Delay = TimeSpan.FromDays(3), Outgoing = [Flows.To("remind")] },
        new ServiceTaskElement { Id = "remind", HandlerKey = "AfterTimer", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("followup");

    TestHarness.AreEqual(ProcessInstanceStatus.Waiting, instance.Status, "instance waits on the timer");
    TestHarness.AreEqual(1, fixture.Timers.Scheduled.Count, "one timer scheduled");
    TestHarness.AreEqual(fixture.Clock.UtcNow.AddDays(3), fixture.Timers.Scheduled[0].FireAt, "timer fire time");

    var resumed = await fixture.Engine.ResumeAsync(fixture.Timers.Scheduled[0].TokenId);

    TestHarness.AreEqual(ProcessInstanceStatus.Completed, resumed.Status, "instance completes after the timer fires");
    TestHarness.AreEqual(1, afterTimer.Calls, "reminder ran once");
});

await TestHarness.RunAsync("resuming a token twice is a no-op", async () =>
{
    var after = new DelegateServiceTaskHandler("After", _ => null);
    var fixture = new Fixture(after);

    fixture.Publish(new ProcessDefinition("idempotent", 1, "Idempotent", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("task")] },
        new UserTaskElement { Id = "task", TitleTemplate = "Проверить", Outgoing = [Flows.To("after")] },
        new ServiceTaskElement { Id = "after", HandlerKey = "After", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("idempotent");
    var tokenId = instance.Tokens.Single(t => t.Status == TokenStatus.Waiting).Id;

    await fixture.Engine.ResumeAsync(tokenId);
    await fixture.Engine.ResumeAsync(tokenId);

    TestHarness.AreEqual(1, after.Calls, "downstream service task ran exactly once");
});

await TestHarness.RunAsync("a throwing handler fails the instance and is logged", async () =>
{
    var boom = new DelegateServiceTaskHandler("Boom", _ => throw new InvalidOperationException("smtp is down"));
    var fixture = new Fixture(boom);

    fixture.Publish(new ProcessDefinition("failing", 1, "Failing", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("boom")] },
        new ServiceTaskElement { Id = "boom", HandlerKey = "Boom", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("failing");

    TestHarness.AreEqual(ProcessInstanceStatus.Failed, instance.Status, "instance status");
    TestHarness.AreEqual("smtp is down", instance.Error, "instance error");
    TestHarness.IsTrue(fixture.Log.Entries.Any(e => e.Event == "Fail"), "failure is present in the process log");
});

await TestHarness.RunAsync("running instances stay on the version they started with", async () =>
{
    var v1 = new DelegateServiceTaskHandler("V1", _ => null);
    var v2 = new DelegateServiceTaskHandler("V2", _ => null);
    var fixture = new Fixture(v1, v2);

    fixture.Publish(new ProcessDefinition("versioned", 1, "Versioned", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("task")] },
        new UserTaskElement { Id = "task", TitleTemplate = "Ждём", Outgoing = [Flows.To("after")] },
        new ServiceTaskElement { Id = "after", HandlerKey = "V1", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    var instance = await fixture.Engine.StartAsync("versioned");
    var tokenId = instance.Tokens.Single(t => t.Status == TokenStatus.Waiting).Id;

    fixture.Publish(new ProcessDefinition("versioned", 2, "Versioned", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("task")] },
        new UserTaskElement { Id = "task", TitleTemplate = "Ждём", Outgoing = [Flows.To("after")] },
        new ServiceTaskElement { Id = "after", HandlerKey = "V2", Outgoing = [Flows.To("end")] },
        new EndEvent { Id = "end" }
    ]));

    await fixture.Engine.ResumeAsync(tokenId);

    TestHarness.AreEqual(1, v1.Calls, "old instance finished on version 1");
    TestHarness.AreEqual(0, v2.Calls, "version 2 was not used by the running instance");
});

await TestHarness.RunAsync("validation reports dangling flows and missing end events", async () =>
{
    var definition = new ProcessDefinition("broken", 1, "Broken", [
        new StartEvent { Id = "start", Outgoing = [Flows.To("nowhere")] }
    ]);

    var errors = definition.Validate();

    TestHarness.IsTrue(errors.Any(e => e.Contains("unknown element")), "dangling flow reported");
    TestHarness.IsTrue(errors.Any(e => e.Contains("no end event")), "missing end event reported");
});

await TestHarness.RunAsync("expression evaluator handles comparisons and logic", () =>
{
    var evaluator = new SimpleExpressionEvaluator();
    var context = new ExpressionContext
    {
        Variables = new Dictionary<string, object?> { ["amount"] = 1500d, ["stage"] = "Negotiation", ["approved"] = false },
        Now = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero)
    };

    TestHarness.IsTrue(evaluator.EvaluateBoolean("vars.amount > 1000", context), "numeric comparison");
    TestHarness.IsTrue(evaluator.EvaluateBoolean("vars.stage == 'Negotiation'", context), "string equality");
    TestHarness.IsTrue(evaluator.EvaluateBoolean("vars.amount > 1000 && !vars.approved", context), "logical and with negation");
    TestHarness.IsTrue(!evaluator.EvaluateBoolean("vars.missing == 'x'", context), "missing variable is null, not an error");
    TestHarness.IsTrue(evaluator.EvaluateBoolean("(vars.amount < 100) || vars.stage == 'Negotiation'", context), "parentheses and or");

    return Task.CompletedTask;
});

return TestHarness.Report();

internal sealed class Fixture
{
    public Fixture(params IServiceTaskHandler[] handlers)
    {
        Clock = new FixedClock(new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero));
        Definitions = new InMemoryProcessDefinitionStore();
        Instances = new InMemoryProcessInstanceStore();
        Log = new InMemoryProcessLogWriter();
        UserTasks = new RecordingUserTaskGateway();
        Timers = new RecordingTimerScheduler();

        Engine = new ProcessEngine(
            Definitions,
            Instances,
            new ServiceTaskRegistry(handlers),
            UserTasks,
            Timers,
            new SimpleExpressionEvaluator(),
            Log,
            Clock);
    }

    public FixedClock Clock { get; }
    public InMemoryProcessDefinitionStore Definitions { get; }
    public InMemoryProcessInstanceStore Instances { get; }
    public InMemoryProcessLogWriter Log { get; }
    public RecordingUserTaskGateway UserTasks { get; }
    public RecordingTimerScheduler Timers { get; }
    public ProcessEngine Engine { get; }

    public void Publish(ProcessDefinition definition) => Definitions.Publish(definition);
}
