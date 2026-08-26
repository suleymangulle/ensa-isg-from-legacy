using Ensa.Domain.Common;
using Ensa.Domain.Services;
using Ensa.Domain.Shared.Exceptions;

namespace Ensa.Domain.Companies;

/// <summary>
/// Domain service that enforces the business rules of the <see cref="CompanyEmployee"/>
/// lifecycle.
/// </summary>
public interface ICompanyEmployeeManager : IDomainService
{
    /// <summary>Creates an employee; the record is persisted only after every business rule has passed.</summary>
    Task<CompanyEmployee> CreateAsync(CompanyEmployee employee, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing employee; the record is persisted only after every business rule has passed.</summary>
    Task<CompanyEmployee> UpdateAsync(CompanyEmployee employee, CancellationToken cancellationToken = default);

    /// <summary>Terminates the employee (deactivates them) and records the termination date.</summary>
    Task TerminateAsync(CompanyEmployee employee, DateTime exitDate, CancellationToken cancellationToken = default);

    /// <summary>Reinstates a terminated employee.</summary>
    Task ReinstateAsync(CompanyEmployee employee, CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="ICompanyEmployeeManager"/>
public class CompanyEmployeeManager : DomainService, ICompanyEmployeeManager
{
    private readonly ICompanyEmployeeRepository _companyEmployeeRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IClock _clock;

    public CompanyEmployeeManager(
        ICompanyEmployeeRepository companyEmployeeRepository,
        ICompanyRepository companyRepository,
        IClock clock)
    {
        _companyEmployeeRepository = companyEmployeeRepository;
        _companyRepository = companyRepository;
        _clock = clock;
    }

    public async Task<CompanyEmployee> CreateAsync(CompanyEmployee employee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        Normalize(employee);

        await ValidateCompanyAsync(employee, cancellationToken);
        ValidateRequiredFields(employee);
        ValidateNationalId(employee.NationalId);
        ValidateDates(employee);
        await EnsureNationalIdUniqueAsync(employee, cancellationToken);
        await ValidateNotActiveAtAnotherCompanyAsync(employee, cancellationToken);

        return await _companyEmployeeRepository.InsertAsync(employee, autoSave: true, cancellationToken);
    }

    public async Task<CompanyEmployee> UpdateAsync(CompanyEmployee employee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (employee.Id <= 0)
        {
            throw new BusinessException(
                "The employee record to update could not be identified.",
                "Ensa:CompanyEmployee:InvalidRecord");
        }

        Normalize(employee);

        await ValidateCompanyAsync(employee, cancellationToken);
        ValidateRequiredFields(employee);
        ValidateNationalId(employee.NationalId);
        ValidateDates(employee);
        await EnsureNationalIdUniqueAsync(employee, cancellationToken);
        await ValidateNotActiveAtAnotherCompanyAsync(employee, cancellationToken);

        return await _companyEmployeeRepository.UpdateAsync(employee, autoSave: true, cancellationToken);
    }

    public async Task TerminateAsync(CompanyEmployee employee, DateTime exitDate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (employee.HireDate.HasValue && exitDate.Date < employee.HireDate.Value.Date)
        {
            throw new BusinessException(
                "The termination date cannot be earlier than the hire date.",
                "Ensa:CompanyEmployee:ExitDateBeforeHireDate");
        }

        if (exitDate.Date > _clock.Now.Date)
        {
            throw new BusinessException(
                "The termination date cannot be in the future.",
                "Ensa:CompanyEmployee:ExitDateInFuture");
        }

        employee.TerminationDate = exitDate;
        employee.IsActive = false;

        await _companyEmployeeRepository.UpdateAsync(employee, autoSave: true, cancellationToken);
    }

    public async Task ReinstateAsync(CompanyEmployee employee, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);

        employee.TerminationDate = null;
        employee.IsActive = true;

        // Reinstating re-runs the "active at only one workplace" rule.
        await ValidateNotActiveAtAnotherCompanyAsync(employee, cancellationToken);

        await _companyEmployeeRepository.UpdateAsync(employee, autoSave: true, cancellationToken);
    }

    // ------------------------------------------------------------------
    // Business rules
    // ------------------------------------------------------------------

    private static void ValidateRequiredFields(CompanyEmployee employee)
    {
        if (string.IsNullOrWhiteSpace(employee.Name))
        {
            throw new EnsaValidationException(nameof(CompanyEmployee.Name), "The employee's first name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(employee.LastName))
        {
            throw new EnsaValidationException(nameof(CompanyEmployee.LastName), "The employee's surname cannot be empty.");
        }

        if (employee.ChildCount is < 0)
        {
            throw new EnsaValidationException(nameof(CompanyEmployee.ChildCount), "The number of children cannot be negative.");
        }
    }

    /// <summary>Verifies that the company the employee is attached to exists and is reachable.</summary>
    private async Task ValidateCompanyAsync(CompanyEmployee employee, CancellationToken cancellationToken)
    {
        if (employee.CompanyId <= 0)
        {
            throw new EnsaValidationException(nameof(CompanyEmployee.CompanyId), "The workplace the employee works at must be selected.");
        }

        var company = await _companyRepository.FindAsync(employee.CompanyId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Company), employee.CompanyId);

        if (!company.IsActive && employee.IsActive)
        {
            throw new BusinessException(
                $"Active employees cannot be added to or updated at '{company.CompanyName}' because the workplace is inactive.",
                "Ensa:CompanyEmployee:CompanyInactive");
        }
    }

    /// <summary>Logical consistency of the date fields.</summary>
    private void ValidateDates(CompanyEmployee employee)
    {
        var today = _clock.Now.Date;

        if (employee.BirthDate.HasValue && employee.BirthDate.Value.Date > today)
        {
            throw new EnsaValidationException(
                nameof(CompanyEmployee.BirthDate),
                "The date of birth cannot be in the future.");
        }

        if (employee.HireDate.HasValue
            && employee.BirthDate.HasValue
            && employee.HireDate.Value.Date < employee.BirthDate.Value.Date)
        {
            throw new EnsaValidationException(
                nameof(CompanyEmployee.HireDate),
                "The hire date cannot be earlier than the date of birth.");
        }

        if (employee.TerminationDate.HasValue)
        {
            if (employee.HireDate.HasValue
                && employee.TerminationDate.Value.Date < employee.HireDate.Value.Date)
            {
                throw new BusinessException(
                    "The termination date cannot be earlier than the hire date.",
                    "Ensa:CompanyEmployee:ExitDateBeforeHireDate");
            }

            if (employee.IsActive)
            {
                throw new BusinessException(
                    "An employee with a termination date cannot be marked as active.",
                    "Ensa:CompanyEmployee:TerminatedEmployeeMarkedActive");
            }
        }
    }

    /// <summary>A national ID cannot be assigned to more than one employee at the same company.</summary>
    private async Task EnsureNationalIdUniqueAsync(CompanyEmployee employee, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(employee.NationalId))
        {
            return;
        }

        var exists = await _companyEmployeeRepository.NationalIdExistsAsync(
            employee.NationalId,
            employee.CompanyId,
            employee.Id > 0 ? employee.Id : null,
            cancellationToken);

        if (exists)
        {
            throw new BusinessException(
                $"National ID '{employee.NationalId}' is already registered to another employee at this workplace.",
                "Ensa:CompanyEmployee:NationalIdAlreadyRegistered");
        }
    }

    /// <summary>
    /// One person cannot be an active employee at more than one workplace at the same time.
    /// The rule does not apply to inactive employees (or those with a termination date).
    /// </summary>
    private async Task ValidateNotActiveAtAnotherCompanyAsync(CompanyEmployee employee, CancellationToken cancellationToken)
    {
        if (!employee.IsActive || string.IsNullOrWhiteSpace(employee.NationalId))
        {
            return;
        }

        var activeRecords = await _companyEmployeeRepository.GetActiveRecordsByNationalIdAsync(
            employee.NationalId,
            employee.Id > 0 ? employee.Id : null,
            cancellationToken);

        var atOtherCompany = activeRecords.FirstOrDefault(p => p.CompanyId != employee.CompanyId);
        if (atOtherCompany is null)
        {
            return;
        }

        var company = await _companyRepository.FindAsync(atOtherCompany.CompanyId, cancellationToken);
        var companyName = company?.CompanyName ?? $"#{atOtherCompany.CompanyId}";

        throw new BusinessException(
            $"The person with national ID '{employee.NationalId}' is already an active employee at '{companyName}'. " +
            "The same person cannot be active at more than one workplace; terminate the other record first.",
            "Ensa:CompanyEmployee:ActiveAtAnotherCompany");
    }

    /// <summary>Throws <see cref="EnsaValidationException"/> when the national ID is invalid.</summary>
    private static void ValidateNationalId(string? nationalId)
    {
        // Foreign nationals may have no Turkish national ID, so an empty value is allowed.
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return;
        }

        if (!IsValidNationalId(nationalId))
        {
            throw new EnsaValidationException(
                nameof(CompanyEmployee.NationalId),
                "The national ID entered is not valid.");
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static void Normalize(CompanyEmployee employee)
    {
        employee.Name = employee.Name.Trim();
        employee.LastName = employee.LastName.Trim();
        employee.NationalId = employee.NationalId?.Replace(" ", string.Empty).Trim();
        employee.Email = employee.Email?.Trim();

        if (string.IsNullOrWhiteSpace(employee.NationalId))
        {
            employee.NationalId = null;
        }
    }

    /// <summary>
    /// Turkish national ID checksum algorithm.
    /// <list type="number">
    /// <item>It must be 11 digits long and consist entirely of digits.</item>
    /// <item>The first digit cannot be 0.</item>
    /// <item>10th digit = ((sum of digits 1, 3, 5, 7, 9 × 7) − (sum of digits 2, 4, 6, 8)) mod 10</item>
    /// <item>11th digit = sum of the first 10 digits mod 10</item>
    /// </list>
    /// </summary>
    public static bool IsValidNationalId(string? nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
        {
            return false;
        }

        var value = nationalId.Trim();

        if (value.Length != 11)
        {
            return false;
        }

        Span<int> digits = stackalloc int[11];
        for (var i = 0; i < 11; i++)
        {
            var character = value[i];
            if (character is < '0' or > '9')
            {
                return false;
            }

            digits[i] = character - '0';
        }

        if (digits[0] == 0)
        {
            return false;
        }

        var oddDigitSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenDigitSum = digits[1] + digits[3] + digits[5] + digits[7];

        // The C# '%' operator can yield a negative result, so it is normalised with +10.
        var tenthDigit = (((oddDigitSum * 7) - evenDigitSum) % 10 + 10) % 10;
        if (tenthDigit != digits[9])
        {
            return false;
        }

        var firstTenDigitSum = oddDigitSum + evenDigitSum + digits[9];
        var eleventhDigit = firstTenDigitSum % 10;

        return eleventhDigit == digits[10];
    }
}
