using Ensa.DataMigrator.Infrastructure;

namespace Ensa.DataMigrator.Tests;

/// <summary>
/// The classification that decides who gets a company scope.
///
/// <para>
/// It is one string comparison, and it is worth pinning because of what sits on the other side of
/// it: <c>UserProfile.CompanyId</c> is the key of a query filter that fails closed. Answer "customer"
/// for a member of staff and that person can see one workplace instead of their organization's
/// thousand — which is exactly the defect this classifier was extracted to prevent, and which went
/// unnoticed for a whole migration because nothing asserted the rule anywhere.
/// </para>
/// </summary>
public class LegacyStaffTypeTests
{
    [Theory]
    [InlineData("Müşteri")]
    [InlineData("Musteri")]
    public void The_two_spellings_of_customer_are_both_recognised(string personelTuru)
    {
        // The legacy database is not consistent about the Turkish letters, and a migration that
        // recognised only one spelling would leave the other half of the customers unscoped.
        Assert.True(LegacyStaffType.IsCustomer(personelTuru));
    }

    [Theory]
    [InlineData("müşteri")]
    [InlineData("MÜŞTERi")]
    [InlineData("  Müşteri  ")]
    public void Case_and_surrounding_space_do_not_change_the_answer(string personelTuru)
    {
        // More tolerant than the legacy predicate, which was a plain `== "Müşteri"`. The direction
        // is deliberate: recognising more people as customers keeps a company scope, failing to
        // recognise one hands them their provider's whole book of workplaces.
        Assert.True(LegacyStaffType.IsCustomer(personelTuru));
    }

    [Fact]
    public void The_comparison_is_ordinal_which_is_where_turkish_casing_stops()
    {
        // Documented boundary rather than a gap. OrdinalIgnoreCase does not fold the dotted capital
        // İ onto i, and it is kept ordinal on purpose: a security decision must not change with the
        // thread's culture, which is exactly what a Turkish-aware comparison would do. The legacy
        // column's nine distinct values across 3,706 rows contain only "Müşteri", so nothing in the
        // data reaches this edge.
        Assert.False(LegacyStaffType.IsCustomer("MÜŞTERİ"));
    }

    [Theory]
    [InlineData("Admin")]              // 713 of these were wrongly company-scoped
    [InlineData("Uzman")]              // 237 of these
    [InlineData("Doktor")]             // 33 of these
    [InlineData("Diğer Sağlık")]
    [InlineData("Ofis personeli")]
    [InlineData("ofis-admin")]
    [InlineData("ser-admin")]
    public void Our_own_staff_are_never_customers(string personelTuru)
    {
        // Every one of these types occurs in the legacy data with a FirmaId set, and every one of
        // them was pinned to a single workplace by reading that column instead of this one.
        Assert.False(LegacyStaffType.IsCustomer(personelTuru));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NCE")]                // a real value in the legacy data with no counterpart
    public void An_unknown_or_missing_type_is_treated_as_staff(string? personelTuru)
    {
        // The safe direction. A value nobody recognised must not silently narrow somebody to one
        // workplace; leaving them with their organization's scope is recoverable, the other way is
        // a person who cannot see their own work.
        Assert.False(LegacyStaffType.IsCustomer(personelTuru));
    }

    [Fact]
    public void The_customer_values_are_the_ones_the_migration_maps_onto_the_customer_user_type()
    {
        // UserSplitStep.StaffTypes maps "Müşteri" onto UserType "MUSTERI", which TenancyStep maps
        // onto StaffRole.Customer. This classifier has to agree with that chain, or the repair and
        // the migration would disagree about the same person.
        Assert.Contains("Müşteri", LegacyStaffType.CustomerValues);
        Assert.Equal(2, LegacyStaffType.CustomerValues.Length);
    }
}
