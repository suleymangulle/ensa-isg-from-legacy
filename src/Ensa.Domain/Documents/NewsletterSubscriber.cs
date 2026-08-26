using Ensa.Domain.Common;

namespace Ensa.Domain.Documents;

/// <summary>
/// A newsletter e-mail subscriber.
/// <para>Legacy equivalent: <c>NewsletterEmail_T</c>.</para>
/// <para>
/// The legacy <c>RegistrationDate</c> column is covered by the base class <c>CreationTime</c>; no
/// separate field was added. <see cref="IsActive"/> is not present in legacy — it was added to
/// support unsubscribing.
/// </para>
/// <para>A host table, with no tenant.</para>
/// </summary>
public class NewsletterSubscriber : CreationAuditedEntity
{
    /// <summary>E-mail address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Whether the subscription is still active (unsubscribe support — NEW field).</summary>
    public bool IsActive { get; set; } = true;
}
