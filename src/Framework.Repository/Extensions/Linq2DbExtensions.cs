using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Repository.Extensions;

/// <summary>
/// 用于检查给定表达式体内是否引用了某成员属性的访问者。
/// </summary>
public class TestMemberExpressionVisitor : ExpressionVisitor
{
    public string? MemberExpString;
    public bool Result { get; private set; }

    public static bool IsExists(Expression? selector, Expression memberExp)
    {
        if (selector is null) return false;
        var visitor = new TestMemberExpressionVisitor { MemberExpString = memberExp.ToString() };
        visitor.Visit(selector);
        return visitor.Result;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (!Result && node.ToString() == MemberExpString) Result = true;
        return node;
    }
}

public static class Linq2DbExt
{
    /// <summary>
    /// 投影出除 <paramref name="selector"/> 内出现的字段之外的全部字段。
    /// 当 <paramref name="selector"/> 为 null 时返回完整列表。
    /// </summary>
    public static List<T1> ToListIgnore<T1>(this ITable<T1> that, Expression<Func<T1, object?>>? selector) where T1 : class
    {
        if (selector is null) return that.ToList();

        var param = Expression.Parameter(typeof(T1), "e");
        var bindings = typeof(T1).GetProperties()
            .Select(p =>
            {
                var member = Expression.Property(param, p);
                var ignored = TestMemberExpressionVisitor.IsExists(selector.Body, member);
                return new { p, member, ignored };
            })
            .Where(a => !a.ignored)
            .Select(a => (MemberBinding)Expression.Bind(a.p, a.member))
            .ToArray();

        var init = Expression.MemberInit(Expression.New(typeof(T1)), bindings);
        var lambda = Expression.Lambda<Func<T1, T1>>(init, param);
        return that.Where(_ => true).Select(lambda).ToList();
    }
}