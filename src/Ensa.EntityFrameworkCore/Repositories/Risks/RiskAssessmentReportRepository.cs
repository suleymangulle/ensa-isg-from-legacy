using Ensa.Domain.Common;
using Ensa.Domain.Companies;
using Ensa.Domain.Membership;
using Ensa.Domain.Risks;
using Ensa.Domain.Risks.Navigations;
using Ensa.Domain.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ensa.EntityFrameworkCore.Repositories.Risks;

/// <summary>
/// EF Core implementation of <see cref="IRiskAssessmentReportRepository"/>.
/// <para>
/// Tenant and soft-delete filtering comes from the global query filters on <see cref="EnsaDbContext"/>; it is
/// not repeated here.
/// </para>
/// </summary>
public class RiskAssessmentReportRepository(EnsaDbContext context, IDataFilter? dataFilter = null)
    : EfCoreRepository<RiskAssessmentReport>(context, dataFilter), IRiskAssessmentReportRepository
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>N+1 PREVENTION:</b> the child lists are fetched in one go with <c>Contains</c> rather than
    /// per hazard, and grouped in memory. The total query count is constant regardless of the report
    /// content: report(1) + company(1) + users(1) + hazards(1) + control measures(1) + categories(1) +
    /// library hazards(1) + 6 enum child tables = at most 13 queries.
    /// </remarks>
    public async Task<RiskAssessmentReportNavigation?> GetWithNavigationAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var report = await GetReadOnlyQueryable()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return null;
        }

        var navigation = new RiskAssessmentReportNavigation { Report = report };

        navigation.Company = await Context.Set<Company>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == report.CompanyId, cancellationToken);

        // The specialist and the physician are fetched in a single query; both may be null.
        var userIds = new List<int>(2);
        if (report.SpecialistUserId is { } specialistId)
        {
            userIds.Add(specialistId);
        }

        if (report.PhysicianUserId is { } physicianId)
        {
            userIds.Add(physicianId);
        }

        if (userIds.Count > 0)
        {
            var users = await Context.Set<User>()
                .AsNoTracking()
                .Where(k => userIds.Contains(k.Id))
                .ToListAsync(cancellationToken);

            navigation.Specialist = users.Find(k => k.Id == report.SpecialistUserId);
            navigation.Physician = users.Find(k => k.Id == report.PhysicianUserId);
        }

        // --- Hazards + control measures: 2 queries in total (no query per hazard) ---
        var hazards = await Context.Set<IdentifiedHazard>()
            .AsNoTracking()
            .Where(t => t.RiskAssessmentReportId == id)
            .OrderByDescending(t => t.RiskScore)
            .ToListAsync(cancellationToken);

        var hazardIds = hazards.ConvertAll(t => t.Id);

        List<ControlMeasure> measures = hazardIds.Count == 0
            ? []
            : await Context.Set<ControlMeasure>()
                .AsNoTracking()
                .Where(o => hazardIds.Contains(o.IdentifiedHazardId))
                .ToListAsync(cancellationToken);

        var measureGroups = measures
            .GroupBy(o => o.IdentifiedHazardId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Library lookups are fetched in bulk as well.
        var categoryIds = hazards
            .Where(t => t.HazardCategoryId.HasValue)
            .Select(t => t.HazardCategoryId!.Value)
            .Distinct()
            .ToList();

        List<HazardCategory> categories = categoryIds.Count == 0
            ? []
            : await Context.Set<HazardCategory>()
                .AsNoTracking()
                .Where(k => categoryIds.Contains(k.Id))
                .ToListAsync(cancellationToken);

        var libraryIds = hazards
            .Where(t => t.HazardId.HasValue)
            .Select(t => t.HazardId!.Value)
            .Distinct()
            .ToList();

        List<Hazard> libraryHazards = libraryIds.Count == 0
            ? []
            : await Context.Set<Hazard>()
                .AsNoTracking()
                .Where(t => libraryIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

        navigation.IdentifiedHazards = hazards.ConvertAll(hazard => new IdentifiedHazardNavigation
        {
            IdentifiedHazard = hazard,
            Category = categories.Find(k => k.Id == hazard.HazardCategoryId),
            LibraryHazard = libraryHazards.Find(k => k.Id == hazard.HazardId),
            ControlMeasures = measureGroups.TryGetValue(hazard.Id, out var list) ? list : []
        });

        // --- Enum-based child tables ---
        navigation.ExposedGroups = await Context.Set<RiskAssessmentExposedGroup>()
            .AsNoTracking()
            .Where(x => x.RiskAssessmentReportId == id)
            .ToListAsync(cancellationToken);

        navigation.ProtectionMeasures = await Context.Set<RiskAssessmentControlMeasure>()
            .AsNoTracking()
            .Where(x => x.RiskAssessmentReportId == id)
            .ToListAsync(cancellationToken);

        navigation.ImprovementActions = await Context.Set<RiskAssessmentImprovementAction>()
            .AsNoTracking()
            .Where(x => x.RiskAssessmentReportId == id)
            .ToListAsync(cancellationToken);

        navigation.SpecialGroups = await Context.Set<RiskAssessmentProtectedGroup>()
            .AsNoTracking()
            .Where(x => x.RiskAssessmentReportId == id)
            .ToListAsync(cancellationToken);

        navigation.Participants = await Context.Set<RiskAssessmentParticipant>()
            .AsNoTracking()
            .Where(x => x.RiskAssessmentReportId == id)
            .ToListAsync(cancellationToken);

        navigation.HistoryRecords = await Context.Set<RiskAssessmentHistoryRecord>()
            .AsNoTracking()
            .Where(x => x.RiskAssessmentReportId == id)
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);

        return navigation;
    }

    /// <inheritdoc />
    public Task<RiskAssessmentReport?> GetActiveReportAsync(
        int companyId,
        DateTime? referenceDate = null,
        CancellationToken cancellationToken = default)
    {
        var reference = (referenceDate ?? DateTime.Now).Date;

        return GetReadOnlyQueryable()
            .Where(r => r.CompanyId == companyId
                        && r.ApprovalStatus == ApprovalStatus.Approved
                        && r.ValidityDate >= reference
                        && r.PerformedDate <= reference)
            .OrderByDescending(r => r.PerformedDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<RiskAssessmentReport>> GetDurationExpiredAsync(
        DateTime reference,
        int remainingDayThreshold = 0,
        int? companyId = null,
        CancellationToken cancellationToken = default)
    {
        // No function is applied to the date column; the upper bound is computed up front so that the index can be used.
        var upperBound = reference.Date.AddDays(remainingDayThreshold);

        var query = GetReadOnlyQueryable().Where(r => r.ValidityDate <= upperBound);

        if (companyId is { } value)
        {
            query = query.Where(r => r.CompanyId == value);
        }

        return query
            .OrderBy(r => r.ValidityDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Because the risk level threshold depends on the report method, the method is read first, the
    /// threshold is converted to a score with <see cref="LevelThreshold"/> and the comparison happens
    /// <b>in the database</b>. Lines with no residual score are always treated as "open".
    /// </remarks>
    public async Task<List<IdentifiedHazard>> GetOpenHighRiskHazardsAsync(
        int riskAssessmentReportId,
        RiskLevel minimumLevel = RiskLevel.High,
        CancellationToken cancellationToken = default)
    {
        var method = await GetReadOnlyQueryable()
            .Where(r => r.Id == riskAssessmentReportId)
            .Select(r => (RiskAssessmentMethod?)r.ReportMethod)
            .FirstOrDefaultAsync(cancellationToken);

        var query = Context.Set<IdentifiedHazard>()
            .AsNoTracking()
            .Where(t => t.RiskAssessmentReportId == riskAssessmentReportId);

        var threshold = method is { } value ? LevelThreshold(value, minimumLevel) : null;

        if (threshold is { } bound)
        {
            query = bound.Inclusive
                ? query.Where(t => t.ResidualRiskScore == null
                                   || t.ResidualRiskScore >= bound.Score)
                : query.Where(t => t.ResidualRiskScore == null
                                   || t.ResidualRiskScore > bound.Score);
        }

        return await query
            .OrderByDescending(t => t.RiskScore)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the minimum score required to reach a given risk level.
    /// Uses exactly the same thresholds as <c>RiskAssessmentManager.DetermineLevel</c>; when
    /// <c>Inclusive</c> is <c>false</c> the comparison is <c>&gt;</c>, otherwise <c>&gt;=</c>.
    /// Returning <c>null</c> means no level filter is applied (methods that produce no numeric score).
    /// </summary>
    private static (decimal Score, bool Inclusive)? LevelThreshold(
        RiskAssessmentMethod method,
        RiskLevel level)
    {
        if (level is RiskLevel.Unspecified or RiskLevel.Negligible)
        {
            return null;
        }

        return method switch
        {
            RiskAssessmentMethod.FineKinney => level switch
            {
                RiskLevel.Low => (20m, false),
                RiskLevel.Medium => (70m, false),
                RiskLevel.High => (200m, false),
                RiskLevel.Intolerable => (400m, false),
                _ => null
            },

            RiskAssessmentMethod.LMatrixFiveByFive => level switch
            {
                RiskLevel.Low => (3m, true),
                RiskLevel.Medium => (8m, true),
                RiskLevel.High => (15m, true),
                RiskLevel.Intolerable => (25m, true),
                _ => null
            },

            RiskAssessmentMethod.LMatrixThreeByThree => level switch
            {
                RiskLevel.Low => (2m, true),
                RiskLevel.Medium => (3m, true),
                RiskLevel.High => (6m, true),
                RiskLevel.Intolerable => (9m, true),
                _ => null
            },

            // For methods that produce no numeric score (FMEA / checklist) a level filter is meaningless.
            _ => null
        };
    }
}
