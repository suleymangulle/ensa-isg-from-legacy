namespace Ensa.HttpApi.Filters;

/// <summary>
/// Marks an endpoint that must be reachable <b>without</b> a valid office context.
///
/// <para>
/// Office resolution refuses a request whose <c>X-Ensa-OfficeId</c> header names an office the
/// caller may not use. That is exactly right everywhere except on the endpoint that tells the caller
/// which offices they may use: a client holding a stale selection would be refused there too, and
/// the only way out of the refusal is the answer it was refused. This attribute breaks that cycle by
/// skipping resolution entirely for the endpoint, which then runs with no office context.
/// </para>
///
/// <para>
/// It is not a way to opt out of office scoping. An endpoint carrying it gets
/// <c>ICurrentOffice.IsSpecified == false</c>, so it must not read
/// an office from anywhere else either.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreOfficeContextAttribute : Attribute;
