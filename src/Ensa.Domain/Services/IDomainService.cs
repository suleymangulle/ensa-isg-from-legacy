namespace Ensa.Domain.Services;

/// <summary>Marker for domain services, used by the DI assembly scan (ABP: <c>IDomainService</c>).</summary>
public interface IDomainService;

/// <summary>Base class for domain services (ABP: <c>DomainService</c>).</summary>
public abstract class DomainService : IDomainService;
