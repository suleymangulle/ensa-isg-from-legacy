using System.Linq.Expressions;

namespace Ensa.Application.Risks;

/// <summary>
/// Combines several filter lambdas into a single predicate that the repository can translate.
/// <para>
/// The Risks module builds list filters from many optional inputs; without a rebinder the
/// separate lambdas would carry different parameter instances and the expression tree would
/// not compile into SQL. Same technique as <c>CompanyAppService</c>, extracted so the six
/// services in this module share one implementation.
/// </para>
/// </summary>
internal static class RiskPredicateBuilder
{
    /// <summary>Logical AND of two predicates over the same entity type.</summary>
    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "e");

        var body = Expression.AndAlso(
            new ParameterRebinder(left.Parameters[0], parameter).Visit(left.Body)!,
            new ParameterRebinder(right.Parameters[0], parameter).Visit(right.Body)!);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>Rewrites two separate lambdas onto a single shared parameter.</summary>
    private sealed class ParameterRebinder(ParameterExpression previous, ParameterExpression replacement)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == previous ? replacement : base.VisitParameter(node);
    }
}

/// <summary>
/// Accumulates optional filter clauses and yields <c>null</c> when nothing was applied
/// (so the repository can skip the WHERE clause entirely).
/// </summary>
/// <typeparam name="T">Entity being filtered.</typeparam>
internal sealed class RiskFilter<T>
{
    private Expression<Func<T, bool>> _predicate = _ => true;
    private bool _applied;

    /// <summary>Adds a clause.</summary>
    public RiskFilter<T> Add(Expression<Func<T, bool>> clause)
    {
        _predicate = _applied ? RiskPredicateBuilder.And(_predicate, clause) : clause;
        _applied = true;
        return this;
    }

    /// <summary>Adds a clause only when <paramref name="condition"/> holds.</summary>
    public RiskFilter<T> AddIf(bool condition, Expression<Func<T, bool>> clause)
        => condition ? Add(clause) : this;

    /// <summary>The combined predicate, or <c>null</c> when no clause was added.</summary>
    public Expression<Func<T, bool>>? Build() => _applied ? _predicate : null;
}
