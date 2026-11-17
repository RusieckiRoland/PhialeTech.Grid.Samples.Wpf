using System.Linq.Expressions;

namespace PhialeTech.Grid.Samples.HttpRest.Server;

// Small plumbing for combining typed lambdas. Business conditions live in StockFilters.
internal static class PredicateComposer
{
    public static Expression<Func<T, bool>> ForField<T, TValue>(
        Expression<Func<T, TValue>> field, Expression<Func<TValue, bool>> condition)
    {
        var body = new ReplaceParameter(condition.Parameters[0], field.Body).Visit(condition.Body)!;
        return Expression.Lambda<Func<T, bool>>(body, field.Parameters);
    }

    public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, matchAll: true);

    public static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Combine(left, right, matchAll: false);

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left, Expression<Func<T, bool>> right, bool matchAll)
    {
        var rightBody = new ReplaceParameter(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        var body = matchAll ? Expression.AndAlso(left.Body, rightBody) : Expression.OrElse(left.Body, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, left.Parameters);
    }

    private sealed class ReplaceParameter(ParameterExpression parameter, Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == parameter ? replacement : base.VisitParameter(node);
    }
}
