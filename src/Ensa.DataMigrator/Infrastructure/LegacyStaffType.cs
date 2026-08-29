namespace Ensa.DataMigrator.Infrastructure;

/// <summary>
/// Reads the legacy staff type — <c>Kullanici_T.PersonelTuru</c> — and answers the one question the
/// company scope turns on: is this account a customer contact, or is it our own staff?
///
/// <para>
/// It exists as a single function because the answer is a security boundary. <c>CompanyId</c> is the
/// key of a query filter that fails closed, so a "yes" here pins somebody to one workplace and a
/// "no" leaves them with their organization's. The migration step that writes the value and the
/// repair step that removes a wrongly written one must not be able to disagree about it — two copies
/// of a list of strings is exactly how they would.
/// </para>
///
/// <para>
/// <b>Why the staff type and not <c>FirmaId</c>.</b> The presence of a legacy <c>FirmaId</c> proves
/// nothing: 731 of the 766 legacy <c>Admin</c> accounts had one, and 728 of those pointed at the
/// organization's own company record rather than at a customer's workplace. The legacy application
/// itself never used the column that way either — it asked
/// <c>PersonelTuru == "Müşteri"</c> (<c>GenelMethodsController.MusteriMi()</c>,
/// <c>BaseController.Subeler</c>) — and <c>UserSplitStep.StaffTypes</c> maps the same column onto
/// <c>UserType</c>. This is that same authority, in code.
/// </para>
/// </summary>
public static class LegacyStaffType
{
    /// <summary>
    /// The values <c>Kullanici_T.PersonelTuru</c> holds for a customer contact.
    /// <para>
    /// The column's complete value set was enumerated from the legacy database — nine distinct
    /// values across 3,706 rows — and exactly one of them is a customer:
    /// <c>Müşteri</c>. <c>Musteri</c> is carried as well because
    /// <c>TenancyStep.StaffRoles</c> already accepts both spellings and a second legacy dump may
    /// not be as consistent as this one.
    /// </para>
    /// </summary>
    public static readonly string[] CustomerValues = ["Müşteri", "Musteri"];

    /// <summary>
    /// Whether a legacy staff type marks a customer contact.
    ///
    /// <para>
    /// This mirrors the legacy predicate, which was a plain equality:
    /// <c>GenelMethodsController.MusteriMi()</c> is <c>Kullanici.PersonelTuru == "Müşteri"</c> and
    /// nothing else. Everything the legacy application did for customers hung off that one
    /// comparison, so anything it did not match — <c>Admin</c>, <c>Uzman</c>, <c>Doktor</c>, the
    /// single <c>NCE</c> row, a null — was staff, and is staff here too.
    /// </para>
    ///
    /// <para>
    /// It is deliberately a little more tolerant than the original: leading and trailing space is
    /// ignored and the comparison is case-insensitive. That direction is the safe one, because it
    /// can only recognise <i>more</i> people as customers, never fewer — and a customer who is not
    /// recognised loses their company scope and sees their whole provider's book of workplaces.
    /// The comparison stays <see cref="StringComparison.OrdinalIgnoreCase"/> rather than a
    /// culture-aware one: a security decision should not change with the thread's culture, and
    /// Turkish casing (<c>i</c>/<c>İ</c>) is precisely where a culture-aware comparison would make
    /// it do so. The consequence is that a hypothetical <c>MÜŞTERİ</c> would not be matched; no such
    /// value exists in the data, and adding a culture to this comparison would be the larger risk.
    /// </para>
    /// </summary>
    public static bool IsCustomer(string? personelTuru)
        => !string.IsNullOrWhiteSpace(personelTuru)
           && Array.Exists(
               CustomerValues,
               value => string.Equals(value, personelTuru.Trim(), StringComparison.OrdinalIgnoreCase));
}
