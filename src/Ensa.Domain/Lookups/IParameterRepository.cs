using Ensa.Domain.Repositories;

namespace Ensa.Domain.Lookups;

/// <summary>
/// Module-specific repository contract for <see cref="Parameter"/>.
/// Implementation: <c>Ensa.EntityFrameworkCore\Repositories</c> (phase 2).
/// </summary>
public interface IParameterRepository : IRepository<Parameter>
{
    /// <summary>Returns the parameter value for the given code, or <c>null</c> when it does not exist.</summary>
    Task<string?> GetValueAsync(string code, CancellationToken cancellationToken = default);
}
