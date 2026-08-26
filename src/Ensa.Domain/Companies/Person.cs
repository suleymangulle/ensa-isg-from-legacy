using Ensa.Domain.Common;

namespace Ensa.Domain.Companies;

/// <summary>
/// A natural person recorded in the system who is not a company employee
/// (employer's representative, visitor, contract counterparty, referee, and so on).
/// <para>Legacy equivalent: <c>Person_T</c>.</para>
/// </summary>
public class Person : FullAuditedTenantEntity, IActivatable
{
    public string Name { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NationalId { get; set; }

    public string? FatherName { get; set; }

    /// <summary>(Legacy: <c>AnneAdi</c>)</summary>
    public string? MotherName { get; set; }

    public int CityId { get; set; }

    public int DistrictId { get; set; }

    public int NeighborhoodId { get; set; }

    public string? Address { get; set; }

    /// <summary>(Legacy: <c>Aktif</c>)</summary>
    public bool IsActive { get; set; } = true;
}
