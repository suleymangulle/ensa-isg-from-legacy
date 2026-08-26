using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Documents;
using Ensa.Domain.Risks;
using Ensa.Domain.Risks.Navigations;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Risks;

/// <summary>
/// EF Core implementation of <see cref="IEmergencyActionPlanRepository"/>.
/// Tenant and soft-delete filtering comes from the global query filters.
/// </summary>
public class EmergencyActionPlanRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<EmergencyActionPlan>(context, dataFilter), IEmergencyActionPlanRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> six queries regardless of how many sections or team members the plan
    /// has — plan, company, documents (both ids in one <c>IN</c>), sections, team members, and the
    /// members' employees (one batched <c>IN</c>).
    /// </remarks>
    public async Task<EmergencyActionPlanNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        var navigation = new EmergencyActionPlanNavigation { Plan = plan };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == plan.CompanyId, cancellationToken);

        List<int> documentIds =
            [.. new[] { plan.EvacuationPlanDocumentId, plan.DocumentId }
                .Where(documentId => documentId is > 0)
                .Select(documentId => documentId!.Value)
                .Distinct()];

        if (documentIds.Count > 0)
        {
            var documents = await Context.Set<Document>()
                .AsNoTracking()
                .Where(d => documentIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            navigation.EvacuationPlanDocument = documents.Find(d => d.Id == plan.EvacuationPlanDocumentId);
            navigation.Document = documents.Find(d => d.Id == plan.DocumentId);
        }

        navigation.Sections =
        [
            .. await Context.Set<EmergencyPlanSection>()
                .AsNoTracking()
                .Where(s => s.EmergencyActionPlanId == id)
                .OrderBy(s => s.OrderNo)
                .ToListAsync(cancellationToken)
        ];

        var teamMembers = await Context.Set<EmergencyTeamMember>()
            .AsNoTracking()
            .Where(m => m.EmergencyActionPlanId == id)
            .ToListAsync(cancellationToken);

        List<int> employeeIds =
            [.. teamMembers.Select(m => m.CompanyEmployeeId).Where(employeeId => employeeId > 0).Distinct()];

        var employees = employeeIds.Count == 0
            ? []
            : await Context.Set<CompanyEmployee>()
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToListAsync(cancellationToken);

        navigation.TeamMembers = teamMembers.ConvertAll(member => new EmergencyTeamMemberNavigation
        {
            TeamMember = member,
            Employee = employees.Find(e => e.Id == member.CompanyEmployeeId)
        });

        return navigation;
    }
}
