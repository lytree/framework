using System.Collections.Immutable;
using Framework.SqlParser.Ast;

namespace Framework.SqlParser.Parsing;

internal sealed partial class SqlParserEngine
{
    private const int OrBindingPower = 10;
    private const int AndBindingPower = 20;
    private const int NotBindingPower = 30;
    private const int ComparisonBindingPower = 40;
    private const int CollateBindingPower = 45;
    private const int ConcatBindingPower = 50;
    private const int AdditiveBindingPower = 60;
    private const int MultiplicativeBindingPower = 70;
    private const int UnaryBindingPower = 80;
    private const int PostfixBindingPower = 90;

    /// <summary>Pratt 表达式解析：<paramref name="minBindingPower"/> 以下的运算符不进入当前层。</summary>
    private SqlExpression? ParseExpressionCore(int minBindingPower)
    {
        if (_context.TooManyErrors)
        {
            return null;
        }

        if (!_context.TryEnterDepth())
        {
            return null;
        }

        try
        {
            var startIndex = _context.Index;
            var left = ParsePrefixExpression();
            if (left is null)
            {
                return null;
            }

            while (true)
            {
                if (_context.TooManyErrors || _context.IsAtEnd)
                {
                    break;
                }

                if (_context.At(SqlTokenKind.ColonColon))
                {
                    if (PostfixBindingPower < minBindingPower)
                    {
                        break;
                    }

                    if (!_dialect.Supports(SqlDialectCapabilities.DoubleColonCast))
                    {
                        _context.Report(
                            SqlDiagnosticCodes.UnsupportedSyntax,
                            _context.Current,
                            $"方言 {_dialect.Name} 不支持 '::' 类型转换。");
                        break;
                    }

                    _context.Advance();
                    var targetType = ParseDataType();
                    if (targetType is null)
                    {
                        break;
                    }

                    left = new SqlCastExpression(left, targetType) { Span = _context.SpanFrom(startIndex) };
                    continue;
                }

                var op = GetInfixOperator(_context.Current);
                if (op.Kind == InfixKind.None || op.LeftBindingPower < minBindingPower)
                {
                    break;
                }

                if (op.Kind == InfixKind.Binary)
                {
                    _context.Advance();
                    var right = IsComparisonOperator(op.Operator) && IsQuantifierStart()
                        ? ParseQuantifiedSubquery()
                        : ParseExpressionCore(op.RightBindingPower);

                    if (right is null)
                    {
                        return left;
                    }

                    left = new SqlBinaryExpression(left, op.Operator, right) { Span = _context.SpanFrom(startIndex) };

                    if (op.LeftBindingPower == ComparisonBindingPower && IsComparisonStart(_context.Current))
                    {
                        _context.Report(
                            SqlDiagnosticCodes.NonAssociativeOperator,
                            _context.Current,
                            "比较运算符不能连用，请使用括号明确优先级。");
                    }

                    continue;
                }

                if (op.Kind == InfixKind.Is)
                {
                    _context.Advance();
                    var negated = _context.MatchKeyword(SqlKeywordId.Not);
                    if (_context.MatchKeyword(SqlKeywordId.Null))
                    {
                        left = new SqlIsNullExpression(left) { Negated = negated, Span = _context.SpanFrom(startIndex) };
                        continue;
                    }

                    _context.Report(
                        SqlDiagnosticCodes.UnsupportedSyntax,
                        _context.Current,
                        "仅支持 IS [NOT] NULL 谓词。");
                    return left;
                }

                if (op.Kind == InfixKind.In)
                {
                    if (op.Negated)
                    {
                        _context.Advance();
                    }

                    left = ParseInExpression(left, startIndex, op.Negated);
                    continue;
                }

                if (op.Kind == InfixKind.Between)
                {
                    if (op.Negated)
                    {
                        _context.Advance();
                    }

                    left = ParseBetweenExpression(left, startIndex, op.Negated);
                    continue;
                }

                if (op.Kind == InfixKind.Like)
                {
                    if (op.Negated)
                    {
                        _context.Advance();
                    }

                    left = ParseLikeExpression(left, startIndex, op.Negated, op.CaseInsensitive);
                    continue;
                }

                if (op.Kind == InfixKind.Collate)
                {
                    _context.Advance();
                    var collation = ParseIdentifier();
                    if (collation is null)
                    {
                        _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "COLLATE 后缺少排序规则名。");
                        return left;
                    }

                    left = new SqlCollateExpression(left, collation) { Span = _context.SpanFrom(startIndex) };
                    continue;
                }

                if (op.Kind == InfixKind.AtTimeZone)
                {
                    _context.Advance(); // AT
                    _context.Advance(); // TIME
                    _context.Advance(); // ZONE
                    var zone = ParseExpressionCore(op.RightBindingPower);
                    if (zone is null)
                    {
                        return left;
                    }

                    left = new SqlAtTimeZoneExpression(left, zone) { Span = _context.SpanFrom(startIndex) };
                    continue;
                }

                break;
            }

            return left;
        }
        finally
        {
            _context.ExitDepth();
        }
    }

    private SqlExpression? ParsePrefixExpression()
    {
        var token = _context.Current;
        switch (token.Kind)
        {
            case SqlTokenKind.Number:
                _context.Advance();
                return CreateLiteral(token, SqlLiteralKind.Number);

            case SqlTokenKind.String:
                _context.Advance();
                return CreateLiteral(token, SqlLiteralKind.String);

            case SqlTokenKind.HexString:
                _context.Advance();
                return CreateLiteral(token, SqlLiteralKind.HexString);

            case SqlTokenKind.BitString:
                _context.Advance();
                return CreateLiteral(token, SqlLiteralKind.BitString);

            case SqlTokenKind.Parameter:
            case SqlTokenKind.Variable:
                _context.Advance();
                return CreateParameterReference(token);

            case SqlTokenKind.Star:
                _context.Advance();
                return new SqlColumnRef(ImmutableArray<SqlIdentifier>.Empty)
                {
                    IsStar = true,
                    Span = token.Span,
                };

            case SqlTokenKind.Plus:
                return ParseUnaryExpression(SqlUnaryOperator.Plus);

            case SqlTokenKind.Minus:
                return ParseUnaryExpression(SqlUnaryOperator.Minus);

            case SqlTokenKind.Tilde:
                return ParseUnaryExpression(SqlUnaryOperator.BitwiseNot);

            case SqlTokenKind.LeftParen:
                return ParseParenthesizedExpression();

            case SqlTokenKind.Identifier:
            case SqlTokenKind.QuotedIdentifier:
                return ParseIdentifierExpression();

            case SqlTokenKind.Keyword:
                return ParseKeywordPrefix(token);

            default:
                _context.Report(
                    SqlDiagnosticCodes.SyntaxError,
                    token,
                    $"预期表达式，实际为 {token.Kind}。");
                return null;
        }
    }

    private SqlExpression? ParseKeywordPrefix(SqlToken token)
    {
        switch (token.Keyword)
        {
            case SqlKeywordId.Null:
                _context.Advance();
                return new SqlLiteral("NULL", SqlLiteralKind.Null) { Span = token.Span };

            case SqlKeywordId.True:
            case SqlKeywordId.False:
                _context.Advance();
                return new SqlLiteral(_context.TokenText(token), SqlLiteralKind.Boolean) { Span = token.Span };

            case SqlKeywordId.Case:
                return ParseCaseExpression();

            case SqlKeywordId.Cast:
                return ParseCastExpression(tryCast: false);

            case SqlKeywordId.TryCast:
                return ParseCastExpression(tryCast: true);

            case SqlKeywordId.Exists:
                return ParseExistsExpression(negated: false);

            case SqlKeywordId.Not:
                if (_context.Peek().Is(SqlKeywordId.Exists))
                {
                    var notStart = _context.Index;
                    _context.Advance(); // NOT
                    _context.Advance(); // EXISTS
                    return ParseExistsExpressionCore(negated: true, notStart);
                }

                return ParseUnaryExpression(SqlUnaryOperator.Not);

            default:
                _context.Report(
                    SqlDiagnosticCodes.SyntaxError,
                    token,
                    $"关键字 {token.Keyword.ToString().ToUpperInvariant()} 不能作为表达式起始。");
                return null;
        }
    }

    private SqlExpression? ParseUnaryExpression(SqlUnaryOperator @operator)
    {
        var startIndex = _context.Index;
        _context.Advance();
        var operand = ParseExpressionCore(@operator == SqlUnaryOperator.Not ? NotBindingPower : UnaryBindingPower);
        if (operand is null)
        {
            return null;
        }

        return new SqlUnaryExpression(@operator, operand) { Span = _context.SpanFrom(startIndex) };
    }

    private SqlExpression? ParseParenthesizedExpression()
    {
        var startIndex = _context.Index;
        _context.Advance(); // (

        if (IsQueryStart())
        {
            var query = ParseQueryExpressionCore();
            _context.Expect(SqlTokenKind.RightParen, "')'");
            if (query is null)
            {
                return null;
            }

            return new SqlSubqueryExpression(query) { Span = _context.SpanFrom(startIndex) };
        }

        var first = ParseExpressionCore(0);
        if (first is null)
        {
            _context.Expect(SqlTokenKind.RightParen, "')'");
            return null;
        }

        if (_context.Match(SqlTokenKind.Comma))
        {
            var items = ImmutableArray.CreateBuilder<SqlExpression>();
            items.Add(first);
            while (true)
            {
                var item = ParseExpressionCore(0);
                if (item is null)
                {
                    break;
                }

                items.Add(item);
                if (!_context.Match(SqlTokenKind.Comma))
                {
                    break;
                }
            }

            _context.Expect(SqlTokenKind.RightParen, "')'");
            return new SqlTupleExpression(items.ToImmutable()) { Span = _context.SpanFrom(startIndex) };
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");

        // 括号本身不建节点，但把区间扩展到括号范围，便于回写与定位。
        return first with { Span = _context.SpanFrom(startIndex) };
    }

    private SqlExpression? ParseIdentifierExpression()
    {
        var startIndex = _context.Index;
        var first = ParseIdentifier();
        if (first is null)
        {
            return null;
        }

        if (_context.At(SqlTokenKind.LeftParen))
        {
            return ParseFunctionCall(first, startIndex);
        }

        var parts = ImmutableArray.CreateBuilder<SqlIdentifier>();
        parts.Add(first);
        var isStar = false;

        while (_context.Match(SqlTokenKind.Dot))
        {
            if (_context.Match(SqlTokenKind.Star))
            {
                isStar = true;
                break;
            }

            var part = ParseIdentifier();
            if (part is null)
            {
                _context.Report(SqlDiagnosticCodes.SyntaxError, _context.Current, "'.' 后缺少标识符。");
                break;
            }

            parts.Add(part);
        }

        var span = _context.SpanFrom(startIndex);

        if (parts.Count == 1 && !isStar)
        {
            return first with { Span = span };
        }

        return new SqlColumnRef(parts.ToImmutable()) { IsStar = isStar, Span = span };
    }

    private SqlExpression? ParseFunctionCall(SqlIdentifier name, int startIndex)
    {
        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var distinct = _context.MatchKeyword(SqlKeywordId.Distinct);
        var star = false;
        var arguments = ImmutableArray.CreateBuilder<SqlExpression>();

        if (_context.At(SqlTokenKind.Star))
        {
            _context.Advance();
            star = true;
        }
        else if (!_context.At(SqlTokenKind.RightParen))
        {
            while (true)
            {
                var argument = ParseExpressionCore(0);
                if (argument is null)
                {
                    break;
                }

                arguments.Add(argument);
                if (!_context.Match(SqlTokenKind.Comma))
                {
                    break;
                }
            }
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");

        SqlOrderByClause? withinGroup = null;
        if (_context.MatchKeyword(SqlKeywordId.Within))
        {
            _context.ExpectKeyword(SqlKeywordId.Group, "GROUP");
            _context.Expect(SqlTokenKind.LeftParen, "'('");
            var orderStart = _context.Index;
            _context.ExpectKeyword(SqlKeywordId.Order, "ORDER");
            _context.ExpectKeyword(SqlKeywordId.By, "BY");
            withinGroup = ParseOrderByClause(orderStart);
            _context.Expect(SqlTokenKind.RightParen, "')'");
        }

        SqlFilterClause? filter = null;
        if (_context.AtKeyword(SqlKeywordId.Filter))
        {
            var filterStart = _context.Index;
            _context.Advance();
            _context.Expect(SqlTokenKind.LeftParen, "'('");
            _context.ExpectKeyword(SqlKeywordId.Where, "WHERE");
            var condition = ParseExpressionCore(0);
            _context.Expect(SqlTokenKind.RightParen, "')'");

            if (condition is not null)
            {
                filter = new SqlFilterClause(condition) { Span = _context.SpanFrom(filterStart) };
            }
        }

        var over = _context.MatchKeyword(SqlKeywordId.Over) ? ParseWindowSpecification() : null;

        return new SqlFunctionCall(name)
        {
            Arguments = arguments.ToImmutable(),
            Distinct = distinct,
            Star = star,
            WithinGroup = withinGroup,
            Filter = filter,
            Over = over,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlExpression? ParseQuantifiedSubquery()
    {
        var startIndex = _context.Index;
        var quantifier = _context.Current.Keyword switch
        {
            SqlKeywordId.Some => SqlQuantifier.Some,
            SqlKeywordId.All => SqlQuantifier.All,
            _ => SqlQuantifier.Any,
        };

        _context.Advance();
        _context.Expect(SqlTokenKind.LeftParen, "'('");
        var query = ParseQueryExpressionCore();
        _context.Expect(SqlTokenKind.RightParen, "')'");

        if (query is null)
        {
            return null;
        }

        return new SqlQuantifiedSubqueryExpression(quantifier, query) { Span = _context.SpanFrom(startIndex) };
    }

    private bool IsQuantifierStart() =>
        _context.Current.Kind == SqlTokenKind.Keyword
        && _context.Current.Keyword is SqlKeywordId.Any or SqlKeywordId.Some or SqlKeywordId.All
        && _context.Peek().Kind == SqlTokenKind.LeftParen;

    private static bool IsComparisonOperator(SqlBinaryOperator @operator) =>
        @operator is SqlBinaryOperator.Equal
            or SqlBinaryOperator.NotEqual
            or SqlBinaryOperator.LessThan
            or SqlBinaryOperator.LessThanOrEqual
            or SqlBinaryOperator.GreaterThan
            or SqlBinaryOperator.GreaterThanOrEqual;

    private SqlExpression? ParseCaseExpression()
    {
        var startIndex = _context.Index;
        _context.Advance(); // CASE

        SqlExpression? operand = null;
        if (!_context.AtKeyword(SqlKeywordId.When))
        {
            operand = ParseExpressionCore(0);
        }

        var whens = ImmutableArray.CreateBuilder<SqlWhenClause>();
        while (_context.AtKeyword(SqlKeywordId.When))
        {
            var whenStart = _context.Index;
            _context.Advance(); // WHEN
            var condition = ParseExpressionCore(0);
            _context.ExpectKeyword(SqlKeywordId.Then, "THEN");
            var result = ParseExpressionCore(0);
            if (condition is null || result is null)
            {
                break;
            }

            whens.Add(new SqlWhenClause(condition, result) { Span = _context.SpanFrom(whenStart) });
        }

        SqlExpression? elseExpression = null;
        if (_context.MatchKeyword(SqlKeywordId.Else))
        {
            elseExpression = ParseExpressionCore(0);
        }

        _context.ExpectKeyword(SqlKeywordId.End, "END");

        return new SqlCaseExpression
        {
            Operand = operand,
            Whens = whens.ToImmutable(),
            Else = elseExpression,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlExpression? ParseCastExpression(bool tryCast)
    {
        var startIndex = _context.Index;
        _context.Advance(); // CAST / TRY_CAST
        _context.Expect(SqlTokenKind.LeftParen, "'('");
        var expression = ParseExpressionCore(0);
        _context.ExpectKeyword(SqlKeywordId.As, "AS");
        var dataType = ParseDataType();
        _context.Expect(SqlTokenKind.RightParen, "')'");

        if (expression is null || dataType is null)
        {
            return expression;
        }

        return new SqlCastExpression(expression, dataType)
        {
            TryCast = tryCast,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlDataType? ParseDataType()
    {
        var startIndex = _context.Index;
        var token = _context.Current;
        SqlIdentifier? name;

        if (token.Kind is SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier)
        {
            _context.Advance();
            name = MakeIdentifier(token);
        }
        else if (token.Kind == SqlTokenKind.Keyword && IsTypeKeyword(token.Keyword))
        {
            _context.Advance();
            name = new SqlIdentifier(_context.TokenText(token)) { Span = token.Span };
        }
        else
        {
            _context.Report(SqlDiagnosticCodes.SyntaxError, token, "预期数据类型名。");
            return null;
        }

        var arguments = ImmutableArray<SqlExpression>.Empty;
        if (_context.At(SqlTokenKind.LeftParen))
        {
            _context.Advance();
            var builder = ImmutableArray.CreateBuilder<SqlExpression>();
            if (!_context.At(SqlTokenKind.RightParen))
            {
                while (true)
                {
                    var argument = ParseExpressionCore(0);
                    if (argument is null)
                    {
                        break;
                    }

                    builder.Add(argument);
                    if (!_context.Match(SqlTokenKind.Comma))
                    {
                        break;
                    }
                }
            }

            _context.Expect(SqlTokenKind.RightParen, "')'");
            arguments = builder.ToImmutable();
        }

        return new SqlDataType(name)
        {
            Arguments = arguments,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlExpression? ParseExistsExpression(bool negated)
    {
        var startIndex = _context.Index;
        _context.Advance(); // EXISTS
        return ParseExistsExpressionCore(negated, startIndex);
    }

    private SqlExpression? ParseExistsExpressionCore(bool negated, int startIndex)
    {
        _context.Expect(SqlTokenKind.LeftParen, "'('");
        var query = ParseQueryExpressionCore();
        _context.Expect(SqlTokenKind.RightParen, "')'");

        if (query is null)
        {
            return null;
        }

        return new SqlExistsExpression(query) { Negated = negated, Span = _context.SpanFrom(startIndex) };
    }

    private SqlExpression ParseInExpression(SqlExpression left, int startIndex, bool negated)
    {
        _context.ExpectKeyword(SqlKeywordId.In, "IN");
        _context.Expect(SqlTokenKind.LeftParen, "'('");

        var values = ImmutableArray.CreateBuilder<SqlExpression>();

        if (IsQueryStart())
        {
            var query = ParseQueryExpressionCore();
            if (query is not null)
            {
                values.Add(new SqlSubqueryExpression(query) { Span = query.Span });
            }
        }
        else if (!_context.At(SqlTokenKind.RightParen))
        {
            while (true)
            {
                var value = ParseExpressionCore(0);
                if (value is null)
                {
                    break;
                }

                values.Add(value);
                if (!_context.Match(SqlTokenKind.Comma))
                {
                    break;
                }
            }
        }

        _context.Expect(SqlTokenKind.RightParen, "')'");

        return new SqlInExpression(left)
        {
            Values = values.ToImmutable(),
            Negated = negated,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlExpression ParseBetweenExpression(SqlExpression left, int startIndex, bool negated)
    {
        _context.ExpectKeyword(SqlKeywordId.Between, "BETWEEN");
        var symmetric = _context.MatchKeyword(SqlKeywordId.Symmetric);

        var low = ParseExpressionCore(ComparisonBindingPower + 1);
        _context.ExpectKeyword(SqlKeywordId.And, "AND");
        var high = ParseExpressionCore(ComparisonBindingPower + 1);

        if (low is null || high is null)
        {
            return left;
        }

        return new SqlBetweenExpression(left, low, high)
        {
            Negated = negated,
            Symmetric = symmetric,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private SqlExpression ParseLikeExpression(
        SqlExpression left,
        int startIndex,
        bool negated,
        bool caseInsensitive)
    {
        _context.Advance(); // LIKE / ILIKE
        var pattern = ParseExpressionCore(ComparisonBindingPower + 1);

        SqlExpression? escape = null;
        if (_context.MatchKeyword(SqlKeywordId.Escape))
        {
            escape = ParseExpressionCore(ComparisonBindingPower + 1);
        }

        if (pattern is null)
        {
            return left;
        }

        return new SqlLikeExpression(left, pattern)
        {
            Escape = escape,
            Negated = negated,
            CaseInsensitive = caseInsensitive,
            Span = _context.SpanFrom(startIndex),
        };
    }

    private InfixOperator GetInfixOperator(SqlToken token)
    {
        if (token.Kind == SqlTokenKind.Keyword)
        {
            switch (token.Keyword)
            {
                case SqlKeywordId.Or:
                    return new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Or, OrBindingPower, OrBindingPower + 1);

                case SqlKeywordId.And:
                    return new InfixOperator(InfixKind.Binary, SqlBinaryOperator.And, AndBindingPower, AndBindingPower + 1);

                case SqlKeywordId.Is:
                    return new InfixOperator(InfixKind.Is, default, ComparisonBindingPower, ComparisonBindingPower + 1);

                case SqlKeywordId.In:
                    return new InfixOperator(InfixKind.In, default, ComparisonBindingPower, ComparisonBindingPower + 1);

                case SqlKeywordId.Between:
                    return new InfixOperator(InfixKind.Between, default, ComparisonBindingPower, ComparisonBindingPower + 1);

                case SqlKeywordId.Like:
                    return new InfixOperator(InfixKind.Like, default, ComparisonBindingPower, ComparisonBindingPower + 1);

                case SqlKeywordId.ILike:
                    return new InfixOperator(
                        InfixKind.Like,
                        default,
                        ComparisonBindingPower,
                        ComparisonBindingPower + 1,
                        CaseInsensitive: true);

                case SqlKeywordId.Collate:
                    return new InfixOperator(InfixKind.Collate, default, CollateBindingPower, CollateBindingPower + 1);

                case SqlKeywordId.Not:
                    return GetNegatedInfixOperator(_context.Peek());

                case SqlKeywordId.At:
                    return _context.Peek().Is(SqlKeywordId.Time) && _context.Peek(2).Is(SqlKeywordId.Zone)
                        ? new InfixOperator(InfixKind.AtTimeZone, default, CollateBindingPower, CollateBindingPower + 1)
                        : InfixOperator.None;

                default:
                    return InfixOperator.None;
            }
        }

        return token.Kind switch
        {
            SqlTokenKind.Equal => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Equal, ComparisonBindingPower, ComparisonBindingPower + 1),
            SqlTokenKind.NotEqual => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.NotEqual, ComparisonBindingPower, ComparisonBindingPower + 1),
            SqlTokenKind.LessThan => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.LessThan, ComparisonBindingPower, ComparisonBindingPower + 1),
            SqlTokenKind.LessThanOrEqual => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.LessThanOrEqual, ComparisonBindingPower, ComparisonBindingPower + 1),
            SqlTokenKind.GreaterThan => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.GreaterThan, ComparisonBindingPower, ComparisonBindingPower + 1),
            SqlTokenKind.GreaterThanOrEqual => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.GreaterThanOrEqual, ComparisonBindingPower, ComparisonBindingPower + 1),
            SqlTokenKind.Concat => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Concat, ConcatBindingPower, ConcatBindingPower + 1),
            SqlTokenKind.Plus => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Add, AdditiveBindingPower, AdditiveBindingPower + 1),
            SqlTokenKind.Minus => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Subtract, AdditiveBindingPower, AdditiveBindingPower + 1),
            SqlTokenKind.Star => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Multiply, MultiplicativeBindingPower, MultiplicativeBindingPower + 1),
            SqlTokenKind.Slash => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Divide, MultiplicativeBindingPower, MultiplicativeBindingPower + 1),
            SqlTokenKind.Percent => new InfixOperator(InfixKind.Binary, SqlBinaryOperator.Modulo, MultiplicativeBindingPower, MultiplicativeBindingPower + 1),
            _ => InfixOperator.None,
        };
    }

    private static InfixOperator GetNegatedInfixOperator(SqlToken token)
    {
        if (token.Kind != SqlTokenKind.Keyword)
        {
            return InfixOperator.None;
        }

        return token.Keyword switch
        {
            SqlKeywordId.In => new InfixOperator(InfixKind.In, default, ComparisonBindingPower, ComparisonBindingPower + 1, Negated: true),
            SqlKeywordId.Between => new InfixOperator(InfixKind.Between, default, ComparisonBindingPower, ComparisonBindingPower + 1, Negated: true),
            SqlKeywordId.Like => new InfixOperator(InfixKind.Like, default, ComparisonBindingPower, ComparisonBindingPower + 1, Negated: true),
            SqlKeywordId.ILike => new InfixOperator(
                InfixKind.Like,
                default,
                ComparisonBindingPower,
                ComparisonBindingPower + 1,
                Negated: true,
                CaseInsensitive: true),
            _ => InfixOperator.None,
        };
    }

    private static bool IsComparisonStart(SqlToken token) =>
        token.Kind is SqlTokenKind.Equal
            or SqlTokenKind.NotEqual
            or SqlTokenKind.LessThan
            or SqlTokenKind.LessThanOrEqual
            or SqlTokenKind.GreaterThan
            or SqlTokenKind.GreaterThanOrEqual
        || (token.Kind == SqlTokenKind.Keyword && token.Keyword is SqlKeywordId.Is or SqlKeywordId.In or SqlKeywordId.Between or SqlKeywordId.Like or SqlKeywordId.ILike);

    private SqlIdentifier? ParseIdentifier()
    {
        var token = _context.Current;
        if (token.Kind is not (SqlTokenKind.Identifier or SqlTokenKind.QuotedIdentifier))
        {
            return null;
        }

        _context.Advance();
        return MakeIdentifier(token);
    }

    private SqlIdentifier MakeIdentifier(SqlToken token)
    {
        var quote = token.Flags switch
        {
            SqlTokenFlags.DoubleQuotedIdentifier => '"',
            SqlTokenFlags.BacktickIdentifier => '`',
            SqlTokenFlags.BracketIdentifier => '[',
            _ => (char?)null,
        };

        return new SqlIdentifier(_context.TokenText(token))
        {
            Quote = quote,
            CaseSensitive = quote is not null,
            Span = token.Span,
        };
    }

    private SqlExpression CreateLiteral(SqlToken token, SqlLiteralKind literalKind)
    {
        var prefix = literalKind != SqlLiteralKind.String
            ? null
            : token.Flags switch
            {
                SqlTokenFlags.NationalString => "N",
                SqlTokenFlags.EscapeString => "E",
                SqlTokenFlags.DollarQuotedString => "$",
                SqlTokenFlags.OracleQuotedString => "q",
                _ => null,
            };

        return new SqlLiteral(token.Value ?? _context.TokenText(token), literalKind)
        {
            Prefix = prefix,
            Span = token.Span,
        };
    }

    private SqlExpression CreateParameterReference(SqlToken token)
    {
        var text = token.Value ?? _context.TokenText(token);

        if (text == "?")
        {
            return new SqlParameterRef("?") { Span = token.Span };
        }

        if (text.StartsWith("@@", StringComparison.Ordinal))
        {
            return new SqlParameterRef("@@") { Name = text[2..], Span = token.Span };
        }

        if (text.StartsWith('@') || text.StartsWith(':'))
        {
            return new SqlParameterRef(text[..1]) { Name = text[1..], Span = token.Span };
        }

        if (text.StartsWith('$') && int.TryParse(text.AsSpan(1), out var index))
        {
            return new SqlParameterRef("$") { Index = index, Span = token.Span };
        }

        return new SqlParameterRef(string.Empty) { Name = text, Span = token.Span };
    }

    private static bool IsTypeKeyword(SqlKeywordId keyword) => keyword is
        SqlKeywordId.Int
        or SqlKeywordId.Integer
        or SqlKeywordId.BigInt
        or SqlKeywordId.SmallInt
        or SqlKeywordId.Boolean
        or SqlKeywordId.Decimal
        or SqlKeywordId.Numeric
        or SqlKeywordId.Varchar
        or SqlKeywordId.Char
        or SqlKeywordId.Text
        or SqlKeywordId.Blob
        or SqlKeywordId.Time;

    private bool IsQueryStart() =>
        _context.AtKeyword(SqlKeywordId.Select)
        || _context.AtKeyword(SqlKeywordId.With)
        || _context.AtKeyword(SqlKeywordId.Values);

    private enum InfixKind : byte
    {
        None = 0,
        Binary,
        Is,
        In,
        Between,
        Like,
        Collate,
        AtTimeZone,
    }

    private readonly record struct InfixOperator(
        InfixKind Kind,
        SqlBinaryOperator Operator,
        int LeftBindingPower,
        int RightBindingPower,
        bool Negated = false,
        bool CaseInsensitive = false)
    {
        public static InfixOperator None => new(InfixKind.None, default, 0, 0);
    }
}
