using Crm.Application.Abstractions;
using Crm.Bpm.Storage;
using Crm.Domain.Sales;

namespace Crm.Infrastructure.Seed;

public sealed class DemoDataSeeder
{
    private readonly IRepository<OpportunityStage> _stages;
    private readonly IRepository<Lead> _leads;
    private readonly InMemoryProcessDefinitionStore _definitions;
    private readonly IUnitOfWork _unitOfWork;

    public DemoDataSeeder(
        IRepository<OpportunityStage> stages,
        IRepository<Lead> leads,
        InMemoryProcessDefinitionStore definitions,
        IUnitOfWork unitOfWork)
    {
        _stages = stages;
        _leads = leads;
        _definitions = definitions;
        _unitOfWork = unitOfWork;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _stages.ListAsync(cancellationToken: cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        // Definitions go in first so the seeded leads already fire their RecordCreated trigger.
        _definitions.Publish(DemoProcesses.LeadFollowUp());
        _definitions.Publish(DemoProcesses.StageApproval());

        string[] names = ["Квалификация", "Презентация", "Коммерческое предложение", "Переговоры", "Закрыта успешно", "Закрыта неуспешно"];
        int[] probabilities = [10, 30, 50, 70, 100, 0];

        for (var i = 0; i < names.Length; i++)
        {
            await _stages.AddAsync(new OpportunityStage
            {
                Name = names[i],
                Order = i,
                Probability = probabilities[i],
                IsFinal = i >= 4,
                IsWon = i == 4
            }, cancellationToken);
        }

        await _leads.AddAsync(new Lead("Внедрение CRM в «Аквамарин»")
        {
            ContactName = "Ирина Ковальчук",
            CompanyName = "Аквамарин",
            Email = "i.kovalchuk@example.com",
            EstimatedAmount = 240000
        }, cancellationToken);

        await _leads.AddAsync(new Lead("Замена 1С у «Логистик Про»")
        {
            ContactName = "Дмитрий Савченко",
            CompanyName = "Логистик Про",
            Phone = "+380 67 000 00 00",
            EstimatedAmount = 90000
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
