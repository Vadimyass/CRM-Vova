using Crm.Application.Abstractions;
using Crm.Application.Processes;
using Crm.Application.Sales;
using Crm.Bpm.Abstractions;
using Crm.Bpm.Engine;
using Crm.Bpm.Expressions;
using Crm.Bpm.Storage;
using Crm.Infrastructure.Handlers;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Processes;
using Crm.Infrastructure.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Crm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCrmInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryDatabase>();
        services.AddSingleton<IOutbox, InMemoryOutbox>();
        services.AddSingleton<InMemoryProcessDefinitionStore>();
        services.AddSingleton<IProcessDefinitionStore>(sp => sp.GetRequiredService<InMemoryProcessDefinitionStore>());
        services.AddSingleton<InMemoryProcessInstanceStore>();
        services.AddSingleton<IProcessInstanceStore>(sp => sp.GetRequiredService<InMemoryProcessInstanceStore>());
        services.AddSingleton<InMemoryProcessLogWriter>();
        services.AddSingleton<IProcessLogWriter>(sp => sp.GetRequiredService<InMemoryProcessLogWriter>());
        services.AddSingleton<InMemoryTimerScheduler>();
        services.AddSingleton<ITimerScheduler>(sp => sp.GetRequiredService<InMemoryTimerScheduler>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IExpressionEvaluator, SimpleExpressionEvaluator>();
        services.AddSingleton<ProcessEngineOptions>();

        services.AddScoped<DomainEventCollector>();
        services.AddScoped(typeof(IRepository<>), typeof(InMemoryRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentUser, DemoCurrentUser>();

        services.AddScoped<IServiceTaskHandler, SetFieldHandler>();
        services.AddScoped<IServiceTaskHandler, CreateActivityHandler>();
        services.AddScoped<IServiceTaskHandler, LogHandler>();
        services.AddScoped<IServiceTaskRegistry, ServiceTaskRegistry>();

        services.AddScoped<IUserTaskGateway, UserTaskGateway>();
        services.AddScoped<IProcessEngine, ProcessEngine>();
        services.AddScoped<IDomainEventDispatcher, ProcessTriggerDispatcher>();

        services.AddScoped<LeadService>();
        services.AddScoped<OpportunityService>();
        services.AddScoped<UserTaskService>();
        services.AddScoped<DemoDataSeeder>();

        services.AddHostedService<TimerHostedService>();
        services.AddHostedService<OutboxHostedService>();

        return services;
    }
}
