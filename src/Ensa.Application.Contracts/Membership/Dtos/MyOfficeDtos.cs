using Ensa.Application.Contracts.Common;

namespace Ensa.Application.Contracts.Membership.Dtos;

/// <summary>
/// One office the signed-in user may work in, as the shell's office switcher needs it.
/// <para>
/// Deliberately not <c>OfficeDto</c>: that is the administration record — address, phone, fax,
/// authorized person, audit columns — and this endpoint is reachable by every authenticated user,
/// including one with no office permission at all. A switcher needs a name and an id; handing it the
/// rest would publish the office directory to everyone who can sign in.
/// </para>
/// </summary>
public class MyOfficeDto : EntityDto
{
    /// <summary>Office name, shown in the switcher.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the organization's headquarters office. Only one office per organization
    /// carries the flag, so the switcher can mark it — it is a label, never a default.
    /// </summary>
    public bool IsHeadquarterOffice { get; set; }
}

/// <summary>
/// The signed-in user's office context: which offices they may work in, which one to start on, and
/// whether they may take the "all offices" scope.
/// </summary>
public class MyOfficesDto
{
    /// <summary>
    /// The permitted offices, ordered by name. Empty means the user has no office to switch between,
    /// and the shell shows no switcher at all — which is what the legacy application did when a user
    /// had fewer than two offices.
    /// </summary>
    public IReadOnlyList<MyOfficeDto> Items { get; set; } = [];

    /// <summary>
    /// The office the shell should start on, or <c>null</c> to start on "all offices". Always one of
    /// <see cref="Items"/> when it has a value.
    /// </summary>
    public int? DefaultOfficeId { get; set; }

    /// <summary>
    /// Whether the caller may work across every office they are permitted (the UI's "Tüm Şubeler").
    /// The client must not infer this from the list length — the server decides it, and a client that
    /// sends the all-offices header without it is refused.
    /// </summary>
    public bool AllOfficesAllowed { get; set; }
}
