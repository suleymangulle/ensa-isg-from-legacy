using Ensa.Domain.Common;
using Ensa.Domain.Shared.Enums;

namespace Ensa.Domain.Finance;

/// <summary>
/// An invoice design template; it holds the print layouts for a module.
/// <para>Legacy equivalent: <c>InvoiceSablonlari_T</c>.</para>
/// <para>
/// The legacy UPPERCASE column names (<c>MODUL</c>, <c>ONDEGER</c>, <c>TASARIM_ADI</c>,
/// <c>TASARIM</c>, <c>ANA_TASARIM</c>) were converted to PascalCase. The legacy
/// <c>EKLEYEN_KULLANICI</c>/<c>DEGISTIREN_KULLANICI</c> (string) columns were replaced by the base
/// class <c>CreatorId</c>/<c>LastModifierId</c> (<c>int?</c>) fields.
/// </para>
/// </summary>
public class InvoiceTemplate : AuditedTenantEntity
{
    /// <summary>The module the template belongs to. (Legacy: <c>MODUL</c> string)</summary>
    public SourceModule ModuleType { get; set; } = SourceModule.Unspecified;

    /// <summary>Whether this is the default template for the module. (Legacy: <c>ONDEGER</c> int?)</summary>
    public bool OnValue { get; set; }

    /// <summary>Template name. (Legacy: <c>TASARIM_ADI</c>)</summary>
    public string DesignName { get; set; } = string.Empty;

    /// <summary>Template design content — HTML or a report definition. (Legacy: <c>TASARIM</c>)</summary>
    public string Design { get; set; } = string.Empty;

    /// <summary>Whether this is the organization's primary template. (Legacy: <c>ANA_TASARIM</c> int?)</summary>
    public bool IsPrimaryDesign { get; set; }
}
