using Ensa.Domain.Common;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Enums;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Companies;

/// <summary>
/// Domain service that enforces the business rules of the <see cref="Company"/> lifecycle.
/// The rules that used to be scattered across entity constructors and the static
/// <c>CompanyOperations</c> class in the legacy system are collected here.
/// </summary>
public interface ICompanyManager : IDomainService
{
    /// <summary>Creates a company; it is persisted only after every business rule has passed.</summary>
    Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing company; it is persisted only after every business rule has passed.</summary>
    Task<Company> UpdateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the company's headquarter link. Passing <c>null</c> for
    /// <paramref name="headquarterCompanyId"/> turns the company into a headquarter itself.
    /// </summary>
    Task AttachToHeadquarterAsync(Company company, int? headquarterCompanyId, CancellationToken cancellationToken = default);

    /// <summary>Normalises the SSI workplace registration number and assigns it after a uniqueness check.</summary>
    Task ChangeSsiNumberAsync(Company company, string? ssiNumber, CancellationToken cancellationToken = default);

    /// <summary>Deactivates the company. A headquarter with active branches cannot be deactivated.</summary>
    Task DeactivateAsync(Company company, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICompanyManager"/>
public class CompanyManager : DomainService, ICompanyManager
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantLimitProvider _tenantLimitProvider;
    private readonly INaceHazardClassProvider _naceHazardClassProvider;
    private readonly ICurrentTenant _currentTenant;

    public CompanyManager(
        ICompanyRepository companyRepository,
        ITenantLimitProvider tenantLimitProvider,
        INaceHazardClassProvider naceHazardClassProvider,
        ICurrentTenant currentTenant)
    {
        _companyRepository = companyRepository;
        _tenantLimitProvider = tenantLimitProvider;
        _naceHazardClassProvider = naceHazardClassProvider;
        _currentTenant = currentTenant;
    }

    public async Task<Company> CreateAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        company.SsiNumber = NormalizeSsiNumber(company.SsiNumber);
        Normalize(company);

        await ValidateRequiredFieldsAsync(company, cancellationToken);
        await EnsureSsiNumberUniqueAsync(company, cancellationToken);
        await ValidateHeadquarterBranchConsistencyAsync(company, cancellationToken);
        await ValidateHazardClassConsistencyAsync(company, cancellationToken);
        await ValidateCompanyLimitAsync(cancellationToken);

        return await _companyRepository.InsertAsync(company, autoSave: true, cancellationToken);
    }

    public async Task<Company> UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        if (company.Id <= 0)
        {
            throw new BusinessException(
                "The company record to update could not be identified.",
                "Ensa:Company:InvalidRecord");
        }

        company.SsiNumber = NormalizeSsiNumber(company.SsiNumber);
        Normalize(company);

        await ValidateRequiredFieldsAsync(company, cancellationToken);
        await EnsureSsiNumberUniqueAsync(company, cancellationToken);
        await ValidateHeadquarterBranchConsistencyAsync(company, cancellationToken);
        await ValidateHazardClassConsistencyAsync(company, cancellationToken);

        return await _companyRepository.UpdateAsync(company, autoSave: true, cancellationToken);
    }

    public async Task AttachToHeadquarterAsync(Company company, int? headquarterCompanyId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        company.HeadquarterCompanyId = headquarterCompanyId;
        company.WorkplaceType = headquarterCompanyId.HasValue ? WorkplaceType.Branch : WorkplaceType.Headquarter;

        await ValidateHeadquarterBranchConsistencyAsync(company, cancellationToken);

        await _companyRepository.UpdateAsync(company, autoSave: true, cancellationToken);
    }

    public async Task ChangeSsiNumberAsync(Company company, string? ssiNumber, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        var newSsiNumber = NormalizeSsiNumber(ssiNumber);
        if (string.Equals(company.SsiNumber, newSsiNumber, StringComparison.Ordinal))
        {
            return;
        }

        company.SsiNumber = newSsiNumber;
        await EnsureSsiNumberUniqueAsync(company, cancellationToken);

        await _companyRepository.UpdateAsync(company, autoSave: true, cancellationToken);
    }

    public async Task DeactivateAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        if (company.WorkplaceType == WorkplaceType.Headquarter)
        {
            var branches = await _companyRepository.GetBranchesAsync(company.Id, onlyActive: true, cancellationToken);
            if (branches.Count > 0)
            {
                throw new BusinessException(
                    $"'{company.CompanyName}' cannot be deactivated because {branches.Count} active branches are attached to it. " +
                    "Deactivate the branches first, or attach them to another headquarter.",
                    "Ensa:Company:HasActiveBranches");
            }
        }

        company.IsActive = false;
        await _companyRepository.UpdateAsync(company, autoSave: true, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Business rules
    // ------------------------------------------------------------------

    /// <summary>Format validation of the basic fields such as name, address and coordinates.</summary>
    private static Task ValidateRequiredFieldsAsync(Company company, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(company.CompanyName))
        {
            throw new EnsaValidationException(nameof(Company.CompanyName), "Company name cannot be empty.");
        }

        if (company.CityId <= 0)
        {
            throw new EnsaValidationException(nameof(Company.CityId), "A province must be selected.");
        }

        if (company.DistrictId <= 0)
        {
            throw new EnsaValidationException(nameof(Company.DistrictId), "A district must be selected.");
        }

        if (company.Latitude is < -90m or > 90m)
        {
            throw new EnsaValidationException(nameof(Company.Latitude), "Latitude must be between -90 and 90.");
        }

        if (company.Longitude is < -180m or > 180m)
        {
            throw new EnsaValidationException(nameof(Company.Longitude), "Longitude must be between -180 and 180.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The SSI workplace registration number must be unique within the active tenant.
    /// An empty registration number is allowed (workplaces that are not registered yet).
    /// </summary>
    private async Task EnsureSsiNumberUniqueAsync(Company company, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(company.SsiNumber))
        {
            return;
        }

        var exists = await _companyRepository.SsiNumberExistsAsync(
            company.SsiNumber,
            company.Id > 0 ? company.Id : null,
            cancellationToken);

        if (exists)
        {
            throw new BusinessException(
                $"SSI workplace registration number '{company.SsiNumber}' is already registered to another workplace in this organization.",
                "Ensa:Company:SsiNumberAlreadyRegistered");
        }
    }

    /// <summary>
    /// Headquarter/branch consistency:
    /// <list type="bullet">
    /// <item>A branch must have a <see cref="Company.HeadquarterCompanyId"/>.</item>
    /// <item>A headquarter must have an empty <see cref="Company.HeadquarterCompanyId"/>.</item>
    /// <item>A company cannot be its own headquarter or branch.</item>
    /// <item>The record chosen as headquarter must belong to the same tenant and be of the
    ///       headquarter type (the hierarchy is single-level).</item>
    /// <item>The headquarter chain must not form a cycle.</item>
    /// </list>
    /// </summary>
    private async Task ValidateHeadquarterBranchConsistencyAsync(Company company, CancellationToken cancellationToken)
    {
        if (company.WorkplaceType == WorkplaceType.Branch && !company.HeadquarterCompanyId.HasValue)
        {
            throw new BusinessException(
                "A workplace marked as a branch must have a headquarter workplace selected.",
                "Ensa:Company:HeadquarterRequiredForBranch");
        }

        if (company.WorkplaceType == WorkplaceType.Headquarter && company.HeadquarterCompanyId.HasValue)
        {
            throw new BusinessException(
                "A workplace marked as a headquarter cannot be attached to another headquarter.",
                "Ensa:Company:HeadquarterCannotHaveHeadquarter");
        }

        if (!company.HeadquarterCompanyId.HasValue)
        {
            return;
        }

        var headquarterCompanyId = company.HeadquarterCompanyId.Value;

        if (company.Id > 0 && headquarterCompanyId == company.Id)
        {
            throw new BusinessException(
                "A workplace cannot be its own headquarter.",
                "Ensa:Company:SelfReferencingHeadquarter");
        }

        var headquarter = await _companyRepository.FindAsync(headquarterCompanyId, cancellationToken)
            ?? throw new BusinessException(
                "The selected headquarter workplace was not found.",
                "Ensa:Company:HeadquarterNotFound");

        if (headquarter.TenantId != company.TenantId)
        {
            throw new BusinessException(
                "The headquarter workplace must belong to the same organization as the branch.",
                "Ensa:Company:HeadquarterInDifferentOrganization");
        }

        if (headquarter.WorkplaceType == WorkplaceType.Branch)
        {
            throw new BusinessException(
                $"'{headquarter.CompanyName}' is itself a branch, so it cannot be the headquarter of another branch. " +
                "The headquarter/branch hierarchy is single-level.",
                "Ensa:Company:HeadquarterIsItselfBranch");
        }

        // Cycle check: if the candidate headquarter sits in this company's own branch tree, the
        // chain would close on itself.
        if (company.Id > 0)
        {
            var hasCycle = await _companyRepository.HasCircularHeadquarterChainAsync(
                company.Id,
                headquarterCompanyId,
                cancellationToken);

            if (hasCycle)
            {
                throw new BusinessException(
                    "The selected headquarter is a branch of this workplace, which would create a cycle in the headquarter chain.",
                    "Ensa:Company:CircularHeadquarterChain");
            }
        }
    }

    /// <summary>
    /// Consistency between the NACE (occupation) code and the hazard class. When an occupation
    /// code is selected the hazard class is mandatory and must match the class published in the
    /// official communiqué. A user who deliberately wants a different class must clear the
    /// <see cref="Company.OrganizationTypeVerified"/> flag.
    /// </summary>
    private async Task ValidateHazardClassConsistencyAsync(Company company, CancellationToken cancellationToken)
    {
        if (!company.OccupationCodeId.HasValue)
        {
            // Without an occupation code the free-text activity field is enough, but the class
            // must still be entered.
            if (company.HazardClass == HazardClass.Unspecified && company.IsActive && !company.IsOrganizationRecord)
            {
                throw new BusinessException(
                    "An active workplace must have a hazard class.",
                    "Ensa:Company:HazardClassMandatory");
            }

            return;
        }

        if (company.HazardClass == HazardClass.Unspecified)
        {
            throw new BusinessException(
                "A workplace with a NACE code selected must have a hazard class.",
                "Ensa:Company:HazardClassMandatory");
        }

        var naceClass = await _naceHazardClassProvider.GetHazardClassAsync(
            company.OccupationCodeId.Value,
            cancellationToken);

        if (naceClass is null || naceClass == HazardClass.Unspecified)
        {
            // The consistency check cannot run when the reference table defines no class.
            return;
        }

        if (naceClass != company.HazardClass && company.OrganizationTypeVerified)
        {
            throw new BusinessException(
                $"The official hazard class of the selected NACE code is '{naceClass}', but the workplace " +
                $"was given '{company.HazardClass}'. Clear the verification flag to use a different class.",
                "Ensa:Company:HazardClassNaceMismatch");
        }
    }

    /// <summary>Checks the active company count against the limit of the tenant's plan.</summary>
    private async Task ValidateCompanyLimitAsync(CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.Id;

        var limit = await _tenantLimitProvider.GetCompanyLimitAsync(tenantId, cancellationToken);
        if (!limit.HasValue)
        {
            return;
        }

        var existingCount = await _companyRepository.GetActiveCompanyCountAsync(cancellationToken);
        if (existingCount >= limit.Value)
        {
            throw new BusinessException(
                $"The limit of {limit.Value} workplaces defined by your organization's plan has been reached. " +
                "Upgrade your plan to add another workplace.",
                "Ensa:Company:CompanyLimitExceeded");
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>Tidies up the free-text fields before saving.</summary>
    private static void Normalize(Company company)
    {
        company.CompanyName = company.CompanyName.Trim();
        company.BranchName = company.BranchName?.Trim();
        company.Email = company.Email?.Trim();
        company.TaxNumber = company.TaxNumber?.Trim();

        // The organization's own record is excluded from the client limit and likewise takes no
        // part in the headquarter/branch hierarchy.
        if (company.IsOrganizationRecord)
        {
            company.HeadquarterCompanyId = null;
            company.WorkplaceType = WorkplaceType.Headquarter;
        }
    }

    /// <summary>
    /// Brings the SSI workplace registration number into a single comparable form.
    /// The behaviour of the legacy <c>CompanyOperations.SGKNoAyarla</c> is preserved: when the
    /// number consists of 9 segments, each segment is left-padded with zeros to its fixed length
    /// and the segments are then concatenated.
    /// Format: <c>1-4444-22-22-7777777-333-22-22-333</c>
    /// </summary>
    public static string? NormalizeSsiNumber(string? ssiNumber)
    {
        if (string.IsNullOrWhiteSpace(ssiNumber))
        {
            return null;
        }

        var parts = ssiNumber.Split(
            [' ', '-', '/'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length >= 9)
        {
            // The segment lengths from the legacy code (the first segment — the nature code — is
            // not padded).
            int[] lengths = [0, 4, 2, 2, 7, 3, 2, 2, 3];

            for (var i = 1; i < lengths.Length; i++)
            {
                parts[i] = parts[i].PadLeft(lengths[i], '0');
            }
        }

        return string.Concat(parts);
    }
}
