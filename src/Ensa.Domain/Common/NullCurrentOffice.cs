namespace Ensa.Domain.Common;

/// <summary>
/// The "no office context" <see cref="ICurrentOffice"/>.
/// <para>
/// Used wherever there is no HTTP request to read a header from: a unit test that builds an
/// application service by hand, the data migrator, a background job. Every property answers the same
/// way an ordinary request with no <c>X-Ensa-OfficeId</c> header does, so such a caller behaves
/// exactly as it did before the office context existed — scoped by tenant, and by nothing else.
/// </para>
/// <para>
/// Deliberately not "every office": a null object that widened a query would turn a missing
/// registration into a silent data leak instead of a visible one.
/// </para>
/// </summary>
public sealed class NullCurrentOffice : ICurrentOffice
{
    /// <summary>Singleton instance.</summary>
    public static readonly NullCurrentOffice Instance = new();

    private NullCurrentOffice() { }

    /// <inheritdoc />
    public bool IsSpecified => false;

    /// <inheritdoc />
    public bool HasOffice => false;

    /// <inheritdoc />
    public int? CurrentOfficeId => null;

    /// <inheritdoc />
    public bool IsAllOffices => false;

    /// <inheritdoc />
    public IReadOnlyList<int> OfficeIds => [];
}
