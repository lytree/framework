using Framework.SqlParser.Analysis;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Tests;

/// <summary>SqlAnalyzer 分析结果测试。</summary>
public sealed class AnalyzerTests
{
    private static SqlAnalysis Analyze(string sql) => SqlAnalyzer.Analyze(Sql.Parse(sql));

    [Test]
    public async Task Select_CollectsTablesAndColumns()
    {
        var analysis = Analyze(
            "select u.id, count(*) from db.users u join orders o on u.id = o.user_id where u.active = 1");

        await Assert.That(analysis.Tables.Length).IsEqualTo(2);
        await Assert.That(analysis.Tables[0].Schema).IsEqualTo("db");
        await Assert.That(analysis.Tables[0].Name).IsEqualTo("users");
        await Assert.That(analysis.Tables[0].Alias).IsEqualTo("u");
        await Assert.That(analysis.Tables[1].Name).IsEqualTo("orders");

        await Assert.That(analysis.Columns.Any(c =>
            c.Parts.Length == 2 && c.Parts[0] == "u" && c.Parts[1] == "id")).IsTrue();
        await Assert.That(analysis.Columns.Any(c =>
            c.Parts.Length == 2 && c.Parts[1] == "active")).IsTrue();
        await Assert.That(analysis.Columns.Any(c => c.Parts.Length == 1 && c.Parts[0] == "count")).IsFalse();
    }

    [Test]
    public async Task CteName_IsNotReportedAsTable()
    {
        var analysis = Analyze("with cte as (select id from base) select * from cte");

        await Assert.That(analysis.Tables.Length).IsEqualTo(1);
        await Assert.That(analysis.Tables[0].Name).IsEqualTo("base");
        await Assert.That(analysis.Columns.Any(c => c.IsStar)).IsTrue();
    }

    [Test]
    public async Task Parameters_AreCollected()
    {
        var analysis = Analyze("select * from t where id = @id and name = :name and code = ?");

        await Assert.That(analysis.Parameters.Length).IsEqualTo(3);
        await Assert.That(analysis.Parameters.Any(p => p.Prefix == "@" && p.Name == "id")).IsTrue();
        await Assert.That(analysis.Parameters.Any(p => p.Prefix == ":" && p.Name == "name")).IsTrue();
        await Assert.That(analysis.Parameters.Any(p => p.Prefix == "?" && p.Name is null)).IsTrue();
    }

    [Test]
    public async Task SelectFeatures_AreComputed()
    {
        var analysis = Analyze("select * from t");

        await Assert.That(analysis.Features.ReadsData).IsEqualTo(SqlFeatureState.Proven);
        await Assert.That(analysis.Features.ModifiesData).IsEqualTo(SqlFeatureState.No);
        await Assert.That(analysis.Features.ReturnsResultSet).IsEqualTo(SqlFeatureState.Proven);
    }

    [Test]
    public async Task SelectWithoutTables_DoesNotReadData()
    {
        var analysis = Analyze("select 1");

        await Assert.That(analysis.Features.ReadsData).IsEqualTo(SqlFeatureState.No);
    }

    [Test]
    public async Task DmlFeatures_AreComputed()
    {
        var insert = Analyze("insert into t (a) values (1)");
        await Assert.That(insert.Features.ReadsData).IsEqualTo(SqlFeatureState.No);
        await Assert.That(insert.Features.ModifiesData).IsEqualTo(SqlFeatureState.Proven);
        await Assert.That(insert.Features.ReturnsResultSet).IsEqualTo(SqlFeatureState.No);

        var delete = Analyze("delete from t where id = 1 returning id");
        await Assert.That(delete.Features.ModifiesData).IsEqualTo(SqlFeatureState.Proven);
        await Assert.That(delete.Features.ReturnsResultSet).IsEqualTo(SqlFeatureState.Proven);
    }

    [Test]
    public async Task DdlFeatures_AreComputed()
    {
        var create = Analyze("create table x (id int)");
        await Assert.That(create.Features.ModifiesData).IsEqualTo(SqlFeatureState.No);
        await Assert.That(create.Features.ReturnsResultSet).IsEqualTo(SqlFeatureState.No);

        var truncate = Analyze("truncate table x");
        await Assert.That(truncate.Features.ModifiesData).IsEqualTo(SqlFeatureState.Proven);
    }

    [Test]
    public async Task DmlTarget_IsReportedAsTable()
    {
        var analysis = Analyze("delete from db.t where id = 1");

        await Assert.That(analysis.Tables.Length).IsEqualTo(1);
        await Assert.That(analysis.Tables[0].Schema).IsEqualTo("db");
        await Assert.That(analysis.Tables[0].Name).IsEqualTo("t");
    }
}
