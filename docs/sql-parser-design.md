# Framework.SqlParser 设计方案

> 目标包：`src/Framework.SqlParser`（PackageId `YLFramework.SqlParser`）
> 目标框架：`net10.0`
> 参考项目：SqlParser.Net（TripleView）、SQLParser（JaCraig）、JSqlParser（JSQLParser）
> 文档状态：设计稿，尚未落地代码

## 0. 结论摘要

三个参考项目代表了三条不同路线：

| 路线 | 代表 | 本质 |
|------|------|------|
| 手写词法 + 手写语法 + 统一 AST | SqlParser.Net | 完全可控，零依赖，AST 即产品 |
| ANTLR 生成词法/语法 + Listener | JaCraig/SQLParser | 语法覆盖快，但对外暴露的是语法树 |
| JavaCC 语法 + 手写 AST/Visitor/Deparser | JSqlParser | AST 为中心，解析/改写/回写闭环 |

本方案取三者之长：

1. **手写 Lexer + 递归下降（语句）+ Pratt（表达式）**，不引入 ANTLR/JavaCC，保持零 NuGet 依赖、AOT 友好、错误可恢复。
2. **统一 AST 作为唯一公共契约**，不暴露内部解析树；每个节点携带源码区间。
3. **解析（Parse）→ 分析（Analyze）→ 改写（Rewrite）→ 回写（Deparse/Format）闭环**，对齐 JSqlParser 的能力面。
4. **方言用能力表建模**，而不是为每个数据库复制一套语法；无法解析的片段保留为 `SqlRawExpression`，保证批次解析不整体失败。
5. **语义特征三值化**（`No` / `Maybe` / `Proven`），直接支撑只读守卫、语句路由、审计与 SQL 防火墙。

## 1. 目标与边界

### 1.1 目标

1. 把 SQL 文本解析为**稳定、可遍历、可改写、可回写**的 C# 对象树。
2. 覆盖主流 OLTP 语法：`SELECT`（含 CTE、JOIN、子查询、集合运算、窗口、分页）、`INSERT`、`UPDATE`、`DELETE`，逐步扩展 `MERGE` 与常用 DDL。
3. 支持 `Generic` / `TSql` / `MySql` / `PostgreSql` / `Oracle` / `Sqlite` 六种方言配置，共享同一套 AST。
4. 每个节点保留源码区间与行号列号，错误信息可精确到字符位置。
5. 往返契约：`Parse(Deparse(ast))` 结构等价；绑定参数、引号风格、字符串前缀不被丢弃。
6. 零第三方依赖，无反射，支持 Native AOT / trimming。
7. 提供分析能力：表引用、列引用、参数、语句特征（是否读数据、是否改数据、是否返回结果集）。
8. 性能目标：常见 OLTP 语句（<1KB）解析在几十微秒级；词法吞吐 >50MB/s；解析路径不使用正则。

### 1.2 非目标

| 项目 | 原因 / 后续 |
|------|------------|
| 执行 SQL、连接数据库 | 纯解析库，不碰 ADO.NET |
| 基于数据字典的语义校验、类型推导 | 需要 catalog；v2 预留 `ISqlSchemaProvider` |
| 全量 DDL（存储过程、触发器、建表选项） | v1 只做查询与 DML；DDL 单独立项 |
| 100% 覆盖 12+ 种方言 | 按“支持度矩阵”声明，未支持语法走 raw fallback |
| SQL 美化器的全部排版风格 | 提供基础格式化；高级排版可独立包 |
| Fluent SQL Builder | 改写 API 已能覆盖常见构造需求；Builder 后续评估 |

### 1.3 设计假设

- 主要使用场景：多租户/分表 SQL 改写、数据权限与脱敏、SQL 审计与防火墙、血缘与影响分析、SQL 格式化、方言翻译的前置解析。
- 解析器是**无状态可复用**的：`SqlParser` 实例线程安全。
- AST 是公共 API，节点命名与字段变更遵循语义化版本。
- 不复制 JSqlParser 源码；只参考其架构与行为。JSqlParser 为 LGPL-2.1 / Apache-2.0 双许可，若未来移植语法文件，走 Apache-2.0 分支并保留 NOTICE。

## 2. 三个参考项目解剖

### 2.1 SqlParser.Net（TripleView）

**定位**：纯 C#、零依赖的 SQL 解析引擎，MIT，`netstandard2.0`，当前 1.1.x。

**架构**（依据仓库结构）：

```text
SqlParser.Net/
├─ Lexer/SqlLexer.cs         手写词法
├─ Lexer/Token.cs
├─ Ast/SqlParser.cs          手写递归下降
├─ Ast/Expression/           38 个 SqlXxxExpression 节点
├─ Ast/Visitor/              IAstVisitor / BaseAstVisitor
│                            SqlGenerationAstVisitor（回写 SQL）
│                            UnitTestAstVisitor（测试断言）
├─ Ast/AnalyzeContext/       表/列上下文
├─ Ast/TokenNode.cs          语法树与 token 的关联
└─ DbUtils.cs                门面：Parse(sql, DbType)
```

**API 风格**：

```csharp
var ast = DbUtils.Parse("select * from test", DbType.Oracle);
var formatted = ast.ToFormat();
var sql = ast.ToSql();
```

**优点**：

- 词法、语法全部手写，体积小、无依赖，解析路径可控。
- AST 只有一棵树，`SqlExpression` 统一所有节点，学习成本低。
- 自带 Visitor 与 SQL 生成 Visitor，能回写、能格式化。
- 对 Oracle、SQL Server、MySQL、PostgreSQL、SQLite 的常见语法做了方言分支。
- 官方称普通 SQL 解析在 0.3ms 以下，并有 360+ 场景单测与各数据库建表脚本语料。

**局限**：

- 以 DML 为主，DDL/存储过程覆盖有限。
- 节点按“表达式”一刀切，`SqlSelectQueryExpression` 一类节点承担了过多职责，节点字段语义不如分层的 Statement/Query/Clause 清晰。
- 方言差异散落在解析代码的 `if (dbType == ...)` 中，新增方言需要改多处。
- 公开 API 以 `DbType` 枚举驱动，扩展自定义方言较困难。
- AST 节点缺少统一的源码区间契约，错误定位与增量工具支持较弱。

### 2.2 SQLParser（JaCraig）

**定位**：ANTLR4 生成的 C# 词法/语法 + Listener，Apache-2.0，当前 5.0.0，目标 `net10.0` / `net11.0`，依赖 `Antlr4.Runtime.Standard 4.13.1`。

**架构**：

```text
ANTLR/TSQL/TSqlLexer.g4
ANTLR/TSQL/TSqlParser.g4
SQLParser/Parsers/TSql/TSqlLexer.cs        (生成)
SQLParser/Parsers/TSql/TSqlParser.cs       (生成)
SQLParser/Parsers/TSql/TSqlParserBaseListener.cs
```

**API 风格**：

```csharp
Parser.Parse("SELECT * FROM Somewhere", new MyPrinter(), Enums.SQLType.TSql);
```

调用方继承 `TSqlParserBaseListener`，在 `EnterXxx` 回调里处理解析树。

**优点**：

- 语法由 `.g4` 描述，覆盖 T-SQL 的能力来自成熟的 ANTLR 语法。
- 生成代码不需要自己维护左递归、优先级和前瞻。
- Listener 模式对“只扫描某类语法”的需求很直接。

**局限**：

- 对外暴露的是**ANTLR 解析树**，不是面向业务的 AST；节点形状随语法文件变化，API 稳定性差。
- 没有独立的 Deparser、Rewriter、语义分析层，需要使用者自己遍历解析树再映射。
- 仓库内当前只保留 T-SQL 语法；跨方言需要多套 `.g4`，方言差异变成语法文件分叉。
- 生成代码体量大、可读性差，难以针对热点路径手写优化；运行时依赖 ANTLR。
- 解析错误恢复与错误信息受生成器约束，难以给出“缺哪个标记、建议补什么”的诊断。

### 2.3 JSqlParser（JSQLParser）

**定位**：JVM 上的 RDBMS 无关 SQL 解析器，JavaCC 生成语法，双许可 LGPL-2.1 / Apache-2.0。

**架构**：

- JavaCC 语法文件生成 Parser。
- 手写、分层的 AST：`Statement` → `Select` / `PlainSelect` / `SetOperationList` → `SelectItem` / `FromItem` / `Expression`。
- Visitor 体系：`ExpressionVisitorAdapter`、`SelectVisitor` 等。
- DeParser 体系：`StatementDeParser`、`SelectDeParser`、`ExpressionDeParser`，实现 AST → SQL。
- `StatementFeatures`：不二次解析即可回答“是否返回结果集 / 是否修改数据”，且为三值语义。
- `TablesNamesFinder`：提取表名。
- 方言通过 `parser.withDialect(Dialect.XXX)` 与细粒度开关（如 `withLegacyMySqlGroupBy`）控制。

**优点**：

- “SQL 文本 → 可遍历对象树 → 再回写 SQL”是最完整的产品化路径。
- AST 以语句/子句/表达式分层，节点语义清晰，适合改写与血缘。
- 有回写、格式化、特征分析、表名提取等上层能力。
- 官方基准称新版本较 5.3 快 11 倍，说明其在对象分配与语法路径上做过大量优化。

**局限**：

- JavaCC 生成代码与手写 AST 之间需要适配层，语法演进成本高。
- 方言开关数量多，行为矩阵复杂，使用者需要理解“哪个开关对应哪种语法”。
- 对象模型偏重（大量 getter/setter、可变对象），跨语言移植成本高。
- JVM 生态绑定，无法直接用于 .NET。

### 2.4 横向对比

| 维度 | SqlParser.Net | JaCraig/SQLParser | JSqlParser | 本方案 |
|------|---------------|-------------------|-----------|--------|
| 运行时 | .NET Standard 2.0 | .NET 10/11 | JVM 11+ | .NET 10 |
| 解析技术 | 手写 Lexer + 递归下降 | ANTLR4 生成 | JavaCC 生成 | 手写 Lexer + 递归下降 + Pratt |
| 对外模型 | 统一 `SqlExpression` AST | ANTLR 解析树 | 分层 AST | 分层 AST |
| 依赖 | 0 | ANTLR Runtime | JavaCC 生成物 | 0 |
| 方言 | 5 种，代码内分支 | 仓库内仅 T-SQL | 12+ 种，开关控制 | 6 种，能力表控制 |
| 回写 SQL | 有（生成 Visitor） | 无 | 有（DeParser） | 有（方言感知 DeParser） |
| 改写 | 可用 Visitor 手写 | 需自行遍历解析树 | Visitor + 可变 AST | Visitor/Rewriter，默认返回新树 |
| 语义分析 | 有上下文雏形 | 无 | 有 `StatementFeatures`、表名提取 | 表/列/参数 + 三值特征 |
| 源码位置 | token 关联 | 解析树上下文 | 部分 | 全节点 `SqlSpan` + 行号列号 |
| 错误恢复 | 抛异常 | 受 ANTLR 约束 | 抛异常为主 | 诊断集合 + 同步集恢复 + raw fallback |
| AOT | 友好 | 需实测生成代码 | 不适用 | 友好 |
| 许可证 | MIT | Apache-2.0 | LGPL-2.1 / Apache-2.0 | MIT |

### 2.5 可借鉴与应避免

**借鉴**：

- SqlParser.Net：统一 AST、Visitor 驱动回写、按数据库准备语料、零依赖。
- JaCraig：用一份可读的语法规范（EBNF）约束实现，语法与实现分离，便于 review 与补测。
- JSqlParser：分层 AST、DeParser 闭环、`StatementFeatures` 三值语义、表名/列名提取、方言能力开关。

**避免**：

- 避免把解析树直接当公共 API（JaCraig）。
- 避免所有节点都塞进一个“表达式”基类导致语义模糊（SqlParser.Net）。
- 避免方言判断散落在解析函数中（SqlParser.Net），也避免开关无限膨胀（JSqlParser）。
- 避免“必须抛异常才能报错”，批量解析与 IDE 场景需要诊断集合。

## 3. 总体架构

```text
                 ┌─────────────────────────────────────────────┐
   SQL 文本 ────► │ 1. Tokenizer  单遍扫描，产出 Token + Trivia  │
                 └──────────────────┬──────────────────────────┘
                                    │ SqlToken[]（池化）
                 ┌──────────────────▼──────────────────────────┐
                 │ 2. Parser     语句分派 + 递归下降 + Pratt    │
                 │    失败：诊断 + 同步集恢复 + raw 节点        │
                 └──────────────────┬──────────────────────────┘
                                    │ SqlStatement（统一 AST）
        ┌───────────────────────────┼───────────────────────────┐
        │                           │                           │
┌───────▼────────┐        ┌─────────▼─────────┐       ┌─────────▼─────────┐
│ 3a. Analyzer   │        │ 3b. Rewriter      │       │ 3c. Deparser      │
│ 表/列/参数/特征 │        │ 返回新 AST        │       │ AST → SQL 文本     │
└───────┬────────┘        └─────────┬─────────┘       └─────────┬─────────┘
        │                           │                           │
        └───────────────► 4. Formatter（排版） ◄────────────────┘
```

**阶段契约**：

| 阶段 | 输入 | 输出 | 失败行为 |
|------|------|------|---------|
| Tokenizer | `string` + 方言 | `SqlToken` 列表 + 诊断 | 未闭合字符串/注释：报诊断并从行尾恢复 |
| Parser | Token 列表 | `SqlStatement` + 诊断 | 语句级恢复，产出 `SqlInvalidStatement` 或 raw 节点 |
| Analyzer | AST | `SqlAnalysis` | 不抛异常，未知函数进入 `UnresolvedFunctions` |
| Rewriter | AST + 规则 | 新 AST | 规则异常向上抛，AST 不原地修改 |
| Deparser | AST + 方言 | SQL 文本 | 未知节点抛 `SqlDeparseException`（内部错误） |

**依赖方向**：`Text` ← `Parsing` ← `Ast` ← `Deparse` / `Analysis` / `Rewrite`。`Ast` 不依赖任何其他层。

## 4. 词法层

### 4.1 Token 模型

```csharp
public enum SqlTokenKind : byte
{
    EndOfFile,
    Identifier,        // 普通标识符
    QuotedIdentifier,  // "x" / `x` / [x]
    Keyword,           // 已知关键字，带 KeywordId
    Number,
    String,            // 含 N'' / E'' / $$...$$ / q'[]'
    HexString,
    BitString,
    Parameter,         // ? / @p / :p / $1
    Variable,          // @@version / @x（按方言区分）
    Comma, Dot, Semicolon,
    LeftParen, RightParen, LeftBracket, RightBracket,
    Plus, Minus, Star, Slash, Percent,
    Equal, NotEqual, LessThan, LessThanOrEqual,
    GreaterThan, GreaterThanOrEqual,
    Concat,            // ||
    ColonColon,        // ::
    Arrow, LongArrow,  // -> / ->>
    Question,          // JSON ?（PostgreSQL）
}

public enum SqlKeywordId : ushort
{
    None = 0, Select, From, Where, Group, By, Having, Order,
    Insert, Into, Values, Update, Set, Delete, Merge,
    With, As, Join, Left, Right, Full, Inner, Outer, Cross, On, Using,
    Union, Intersect, Except, Minus, Limit, Offset, Fetch, Top,
    Case, When, Then, Else, End, Cast, Between, In, Like, Exists,
    Over, Partition, Window, Returning, Output,
    // ...由源生成器维护
}

public readonly struct SqlToken
{
    public SqlTokenKind Kind { get; init; }
    public SqlKeywordId Keyword { get; init; }  // Kind == Keyword 时有效
    public SqlSpan Span { get; init; }
    public int Line { get; init; }              // 1 基
    public int Column { get; init; }            // 1 基
    public string? Value { get; init; }         // 已解码的标识符/字面量；关键字为 null
}
```

要点：

- Token 只保存**区间**与**解码后的值**；关键字不分配字符串。
- 关键字识别产出 `SqlKeywordId` 枚举，解析器全程做枚举比较，不做字符串比较。
- 引号风格、字符串前缀、转义方式记录在 token 的附加标志位中，供回写保真。

### 4.2 扫描状态

词法器是一个 `ref struct` 状态机，直接在 `string.AsSpan()` 上单遍推进：

| 状态 | 处理 |
|------|------|
| Default | 空白、行注释、块注释、标识符/关键字、数字、字符串、参数、运算符 |
| LineComment | `--` / `#`（MySQL）到行尾 |
| BlockComment | `/* ... */`，支持嵌套（PostgreSQL） |
| String | `''` 转义；MySQL `\` 转义；PostgreSQL `E''`、`$tag$...$tag$`；Oracle `q'[...]'` |
| QuotedIdentifier | `""` / ` `` ` / `[]` 转义 |

方言钩子（由 `SqlDialect` 提供）：

- 标识符引号字符集合与转义规则。
- 字符串转义规则、是否支持 `N''` / `E''` / dollar-quoted / `q''`。
- 行注释起始符（`--`，MySQL 额外 `#`）。
- 参数前缀集合（`?` / `@` / `:` / `$n`）。
- 是否允许反斜杠转义。

词法器不判断关键字是否“保留字”，只标记 `SqlKeywordId`；保留性由解析器按方言处理，避免 `limit`、`top`、`user` 这类词在不同上下文中的歧义。

### 4.3 错误恢复

- 未闭合字符串/引号标识符：在行尾或文件尾结束 token，记 `SQL1002`，继续扫描。
- 非法数字（如 `1.2.3`）：取最长合法前缀，余下重新扫描，记 `SQL1003`。
- 未知字符：产出 `Unknown` token，记 `SQL1001`，跳过 1 个字符。
- `MaxInputLength`（默认 1MB）超限：直接返回 `SQL1007`，不做部分解析。

### 4.4 性能要点

- 禁止正则、禁止 `Substring`；标识符值只在构造 AST 时解码一次。
- ASCII 快路径：`c < 128` 直接查表；非 ASCII 仅在标识符/字符串内处理。
- 关键字匹配用源生成器生成“按长度分组 + 文本比较”的 switch，JIT 可内联。
- Token 列表从 `ArrayPool<SqlToken>.Shared` 租用，解析结束归还；对外只暴露 AST 与诊断。

## 5. 语法层

### 5.1 语句分派

解析器入口按首个有效 token 分派，跳过前置注释与空语句：

```text
WITH      → CTE 列表 → SELECT / INSERT / UPDATE / DELETE / MERGE
SELECT    → 查询（可能接 INTO，TSql）
VALUES    → 查询（VALUES 作为查询体）
(         → 查询（括号查询）
INSERT    → INSERT 语句
UPDATE    → UPDATE 语句
DELETE    → DELETE 语句
MERGE     → MERGE 语句
其他      → SQL2001，产出 SqlInvalidStatement（保留原始区间）
```

分派只看一个 token，不做多 token 回溯；`WITH` 后面的目标语句在 CTE 解析完成后再次分派。

### 5.2 表达式：Pratt 解析

单个 `ParseExpression(minBp)` 处理所有表达式，优先级表如下：

| 绑定力 | 运算符 | 结合性 | 备注 |
|--------|--------|--------|------|
| 10 | `OR` | 左 | |
| 20 | `AND` | 左 | |
| 30 | `NOT`（前缀） | 右 | 操作数按 30 解析，从而包含比较、排除 AND |
| 40 | `= <> < <= > >=` | 非结合 | |
| 40 | `IS [NOT] NULL/TRUE/FALSE` | 后缀 | |
| 40 | `[NOT] IN` / `BETWEEN` / `[NOT] LIKE` / `ILIKE` | 非结合 | `BETWEEN` 内的 `AND` 作为终结符，不构造布尔 AND |
| 45 | `AT TIME ZONE` / `COLLATE` | 左 | |
| 50 | `\|\|` | 左 | TSql 的字符串拼接是 `+` |
| 60 | `+ -` | 左 | |
| 70 | `* / %` | 左 | |
| 80 | 一元 `+ - ~` | 右 | |
| 90 | `::`（PostgreSQL）、`[]`、`.`、函数调用 `(...)`、`OVER (...)`、`FILTER (...)`、`WITHIN GROUP (...)` | 后缀 | 函数调用在 Pratt 中按后缀处理 |

实现约定：

- 前缀位置：字面量、列引用、`(`（子查询或括号表达式）、`CASE`、`CAST`、`EXISTS`、`(` 行构造、`ARRAY[...]`、参数、`*`（仅在 select item / count(*) 上下文合法）。
- 中缀循环：读取运算符 → 根据左右绑定力决定是否吞入；后缀运算符优先级最高。
- `NOT IN` / `NOT LIKE` / `NOT BETWEEN` 作为组合中缀处理，避免把 `NOT` 误当独立前缀。
- 深度超过 `MaxDepth`（默认 128）立即记 `SQL1006` 并返回 `null`，由上层逐层退出，防止恶意嵌套拖垮调用栈。

### 5.3 语法草案（EBNF 摘要）

```ebnf
statement      = query | insert | update | delete | merge ;
query          = [ with_clause ] query_body [ order_by ] [ limit ] ;
query_body     = select_core { set_operator select_core } ;
select_core    = "SELECT" [ set_quantifier ] select_list
                 [ from_clause ] [ where ] [ group_by ] [ having ]
                 [ window_clause ] [ qualify ] [ connect_by ] ;
select_list    = "*" | select_item { "," select_item } ;
select_item    = expression [ alias ] ;
from_clause    = "FROM" table_source { "," table_source } ;
table_source   = table_primary { join_clause table_primary [ join_spec ] } ;
table_primary  = named_table | derived_table | table_function
               | values_table | "(" table_source ")" ;
join_clause    = [ "NATURAL" ] [ "INNER" | "LEFT" | "RIGHT" | "FULL" ] [ "OUTER" ]
                 "JOIN" | "CROSS" "JOIN" | "STRAIGHT_JOIN" ;
alias          = [ "AS" ] identifier [ "(" column_alias_list ")" ] ;
expression     = pratt_expression ;
```

DML 侧：

```ebnf
insert  = "INSERT" "INTO" target [ column_list ] insert_source [ returning_or_output ] ;
insert_source = "VALUES" row_list | query | "DEFAULT" "VALUES" ;
update  = "UPDATE" target "SET" set_list [ from_clause ] [ where ] [ returning_or_output ] ;
delete  = "DELETE" [ "FROM" ] target [ using_clause ] [ where ] [ returning_or_output ] ;
merge   = "MERGE" "INTO" target "USING" table_source "ON" expression when_clauses ;
```

### 5.4 歧义处理清单

| 歧义 | 处理策略 |
|------|---------|
| `LIMIT` / `TOP` / `FETCH` 作为标识符 | 只在子句起始位置按关键字处理；其余位置按普通标识符 |
| `(SELECT ...)` vs `(expr, expr)` | 在 `(` 后看一个 token：`SELECT`/`WITH`/`VALUES` 视为子查询，否则按表达式/行构造 |
| `CASE` 结束边界 | 显式维护 `CASE` 深度，`END` 只关闭最内层 |
| `BETWEEN x AND y` | `AND` 由 `BETWEEN` 分支消费，不进入布尔 AND 循环 |
| `OVER (...)` vs 普通函数名 | `OVER` 作为函数调用的后缀，仅当函数名后出现 `OVER` 才消费 |
| `SELECT ... INTO`（TSql） | 仅 TSql 方言在 select list 之后识别 `INTO` |
| `UPDATE ... FROM` vs MySQL 多表 UPDATE | 由方言能力位决定是否允许 `FROM` 子句 |
| `INSERT ... SELECT` | `INSERT` 的源按 `VALUES` / `query` / `DEFAULT VALUES` 三分支判定 |
| `WITH` 前缀 DML | CTE 解析完再分派 `SELECT`/`INSERT`/`UPDATE`/`DELETE`/`MERGE` |
| Oracle `CONNECT BY` | 作为 `select_core` 的可选子句，仅 Oracle 能力位开启 |

### 5.5 错误恢复与诊断

- `Expect(kind)` 失败时：记诊断、返回零宽合成 token、**不抛异常**，让上层继续尝试恢复。
- 语句级同步集：`;`、EOF、以及语句起始关键字（`SELECT`/`INSERT`/`UPDATE`/`DELETE`/`MERGE`/`WITH`）。
- 子句级同步集：`,`, `)`, 下一个子句关键字（`FROM`/`WHERE`/`GROUP`/`ORDER`/`LIMIT` 等）。
- 连续错误达到 `MaxErrors`（默认 8）后，跳过当前语句剩余 token，保证批次解析继续。
- 无法识别的子句/表达式：生成 `SqlRawExpression`（保留原始文本区间），记 `SQL2003` 警告；回写时原样输出。
- `Parse` 在存在 `Error` 级诊断时抛 `SqlParseException`；`TryParse` 返回 `false` 并通过 out 参数返回诊断与部分 AST。

## 6. AST 模型

### 6.1 基类

```csharp
public abstract record SqlNode
{
    public abstract SqlNodeKind Kind { get; }
    public SqlSpan Span { get; init; }
    public abstract void CollectChildren(List<SqlNode> children);
}

public readonly record struct SqlSpan(int Start, int Length)
{
    public int End => Start + Length;
    public static SqlSpan Empty => new(0, 0);
}
```

选择 `record` 的原因：改写时可用 `with` 表达式，节点克隆成本低、意图清晰；跨节点的结构比较由 `SqlAstComparer.StructuralEquals` 提供，不依赖 `record` 自动生成的相等性。

`CollectChildren` 用显式列表收集子节点，遍历器复用池化列表，避免 `yield` 迭代器分配。节点集合统一使用 `ImmutableArray<SqlNode>`（空集合用 `ImmutableArray.Empty`，不出现 `null`）。

### 6.2 节点分层

```text
SqlNode
├─ SqlStatement
│  ├─ SqlSelectStatement      { With?, Query, Dialect }
│  ├─ SqlInsertStatement      { With?, Target, Columns, Source, Returning?/Output?, Dialect }
│  ├─ SqlUpdateStatement      { With?, Target, Set, From?, Where?, Returning?, Dialect }
│  ├─ SqlDeleteStatement      { With?, Target, Using?, Where?, Returning?, Dialect }
│  ├─ SqlMergeStatement       { Target, Source, On, WhenClauses, Dialect }
│  └─ SqlInvalidStatement     { RawSpan, Dialect }    // 恢复产物
├─ SqlQueryExpression
│  │                          { OrderBy?, Limit?, Offset?, Fetch? }
│  ├─ SqlSelectQuery          { Quantifier, Items, From, Where, GroupBy, Having,
│  │                            Windows, Qualify, ConnectBy }
│  ├─ SqlSetOperationQuery    { Left, Operator, All, Right }
│  ├─ SqlValuesQuery          { Rows }
│  └─ SqlParenthesizedQuery   { Inner }
├─ SqlTableSource
│  ├─ SqlNamedTable           { Schema?, Name, Alias?, ColumnAliases, Hints }
│  ├─ SqlDerivedTable         { Query, Alias, Lateral }
│  ├─ SqlJoinedTable          { Left, JoinType, Right, On?, Using? }
│  ├─ SqlTableFunction        { Name, Arguments, Alias }
│  └─ SqlValuesTable          { Rows, Alias }
├─ SqlClause
│  ├─ SqlWithClause / SqlCommonTableExpression
│  ├─ SqlSelectItem           { Expression, Alias? }
│  ├─ SqlOrderByClause / SqlOrderByItem
│  ├─ SqlGroupByClause / SqlHavingClause
│  ├─ SqlLimitClause / SqlOffsetClause / SqlFetchClause / SqlTopClause
│  ├─ SqlWindowClause / SqlWindowSpec
│  └─ SqlReturningClause / SqlOutputClause
└─ SqlExpression
   ├─ SqlIdentifier           { Value, Quote?, CaseSensitive }
   ├─ SqlLiteral              { Value, LiteralKind, Prefix }
   ├─ SqlColumnRef            { Parts, IsStar }
   ├─ SqlParameterRef         { Name?, Index?, Prefix }
   ├─ SqlBinaryExpression     { Left, Operator, Right }
   ├─ SqlUnaryExpression      { Operator, Operand }
   ├─ SqlFunctionCall         { Name, Arguments, Distinct, Over?, Filter?, WithinGroup? }
   ├─ SqlCaseExpression       { Operand?, Whens, Else? }
   ├─ SqlCastExpression       { Expression, TargetType, TryCast }
   ├─ SqlSubqueryExpression   { Query }
   ├─ SqlExistsExpression     { Query, Negated }
   ├─ SqlInExpression         { Expression, Values, Negated }
   ├─ SqlBetweenExpression    { Expression, Low, High, Negated, Symmetric }
   ├─ SqlLikeExpression       { Expression, Pattern, Escape?, Negated, CaseInsensitive }
   ├─ SqlIsNullExpression     { Expression, Negated }
   ├─ SqlTupleExpression      { Items }
   ├─ SqlArrayExpression      { Items }
   ├─ SqlSubscriptExpression  { Target, Index }
   ├─ SqlCollateExpression    { Expression, Collation }
   ├─ SqlAtTimeZoneExpression { Expression, Zone }
   └─ SqlRawExpression        { RawSpan }             // 恢复产物
```

### 6.3 节点设计规则

1. **源码区间**：节点 `Span` 覆盖从首 token 到末 token（不含尾部 trivia）；`SqlIdentifier` 额外记录引号风格。
2. **标识符保真**：`SqlIdentifier { string Value; char? Quote; bool CaseSensitive }`，回写默认沿用原引号。
3. **字面量保真**：`SqlLiteral` 记录 `Prefix`（`N`/`E`/`X`/`B`/`$tag$`）与是否含转义，默认回写保持原风格。
4. **参数保真**：`?`、`@p`、`:p`、`$1` 在 AST 中保留前缀与原始写法，不统一改写。
5. **空集合不写 null**：可选子句用 `null` 表示“未出现”，集合用空 `ImmutableArray`。
6. **不缓存派生信息**：不做名称解析、不做类型推导；分析结果放在独立的 `SqlAnalysis`，AST 保持纯粹。
7. **raw 节点是逃生舱**：只在错误恢复时产生，且回写必须能还原原文；正常路径不允许产生 raw 节点。
8. **方言归属**：`SqlStatement.Dialect` 记录解析时使用的方言，`ToSql()` 默认沿用它，允许显式覆盖。

### 6.4 节点数量控制

v1 目标约 **45～55 个节点类型**。新增节点必须满足以下之一：

- 新语句种类；
- 新语法结构且无法用现有节点表达；
- 回写需要区分的结构差异。

仅“语义不同但语法相同”的场景不新增节点，避免节点爆炸（SqlParser.Net 38 个表达式 + JSqlParser 上百个类的取舍中点）。

## 7. 方言建模

### 7.1 模型

```csharp
public sealed class SqlDialect
{
    public static SqlDialect Generic { get; }
    public static SqlDialect TSql { get; }
    public static SqlDialect MySql { get; }
    public static SqlDialect PostgreSql { get; }
    public static SqlDialect Oracle { get; }
    public static SqlDialect Sqlite { get; }

    public string Name { get; }
    public SqlDialectCapabilities Capabilities { get; }
    public IReadOnlySet<SqlKeywordId> ReservedKeywords { get; }
}

[Flags]
public enum SqlDialectCapabilities
{
    None = 0,
    BracketIdentifiers     = 1 << 0,   // [x]
    BacktickIdentifiers    = 1 << 1,   // `x`
    DollarQuotedStrings    = 1 << 2,   // $$...$$
    EscapeStringPrefix     = 1 << 3,   // E'...'
    NationalStringPrefix   = 1 << 4,   // N'...'
    BackslashEscapes       = 1 << 5,
    LimitClause            = 1 << 6,
    LimitOffsetComma       = 1 << 7,   // LIMIT offset, count
    TopClause              = 1 << 8,
    OffsetFetch            = 1 << 9,
    Returning              = 1 << 10,
    OutputClause           = 1 << 11,
    DistinctOn             = 1 << 12,
    Lateral                = 1 << 13,
    ConnectBy              = 1 << 14,
    DoubleColonCast        = 1 << 15,
    CaseInsensitiveLike    = 1 << 16,  // ILIKE
    JsonArrowOperators     = 1 << 17,
    SquareBracketSubscript = 1 << 18,
    HashComments           = 1 << 19,
    AtTimeZone             = 1 << 20,
    QualifyClause          = 1 << 21,
}
```

`SqlParserOptions`：

```csharp
public sealed class SqlParserOptions
{
    public SqlDialect Dialect { get; init; } = SqlDialect.Generic;
    public int MaxDepth { get; init; } = 128;
    public int MaxErrors { get; init; } = 8;
    public int MaxInputLength { get; init; } = 1 << 20;
    public bool EnableRecovery { get; init; } = true;
    public bool AllowLegacyMySqlGroupByOrdinal { get; init; }
    public bool AllowDollarQuotedStringTags { get; init; }
}
```

### 7.2 方言差异表

| 能力 | Generic | TSql | MySQL | PostgreSQL | Oracle | SQLite |
|------|---------|------|-------|------------|--------|--------|
| 标识符引号 | `"` | `[]` / `"` | `` ` `` / `"` | `"` | `"` | `"` / `[]` / `` ` `` |
| 字符串前缀 | — | `N''` | `N''` | `E''` / `$$` | `N''` / `q''` | `X''` |
| 分页 | `LIMIT/OFFSET` | `TOP` / `OFFSET FETCH` | `LIMIT n,m` | `LIMIT/OFFSET` | `OFFSET FETCH` / `ROWNUM` | `LIMIT/OFFSET` |
| 参数 | `?` | `@p` | `?` / `@p` | `$1` / `:p` | `:p` | `?` / `:p` / `@p` |
| 返回结果 | — | `OUTPUT` | — | `RETURNING` | `RETURNING` | `RETURNING` |
| 特有结构 | — | `CROSS/OUTER APPLY`、hints | `ON DUPLICATE KEY UPDATE`、`STRAIGHT_JOIN` | `DISTINCT ON`、`LATERAL`、`::`、`ILIKE` | `CONNECT BY`、`MINUS`、`(+)` | `PRAGMA`、`WITHOUT ROWID` |

### 7.3 为什么不用“一方言一套语法”

- 语句骨架高度一致，分叉会导致 6 份解析代码、6 份测试、6 处 bug。
- 方言差异集中在：引号/转义、分页、DML 返回子句、若干专有子句。
- 采用“**统一语法 + 能力位 + 少量语法结构分支**”：
  - 引号、字符串、参数、注释差异在词法层解决；
  - 分页/返回子句差异在子句解析处按能力位选择；
  - `CONNECT BY`、`QUALIFY`、`APPLY` 等结构性差异才写独立分支。
- 新增方言 = 新增一个 `SqlDialect` 实例 + 一份语料，不改解析器主干。

## 8. 遍历、改写与分析

### 8.1 遍历

```csharp
public abstract class SqlVisitor
{
    public virtual void Visit(SqlNode node)
    {
        VisitNode(node);
        var children = SqlNodeBuffer.Rent();
        node.CollectChildren(children);
        foreach (var child in children)
            Visit(child);
        SqlNodeBuffer.Return(children);
    }

    protected virtual void VisitNode(SqlNode node) { }
}
```

- 需要类型化钩子时，在派生类里用 `switch (node)` 模式匹配；不强制每个节点实现 `Accept`，避免 50 个方法的接口。
- `SqlWalker` 使用显式栈而非递归，避免深树栈溢出；解析深度已限制，遍历同样受 `MaxDepth` 保护。
- 提供 `SqlCollector<T>`：按谓词收集节点，供分析器复用。

### 8.2 改写

```csharp
public abstract class SqlRewriter
{
    public static SqlNode Rewrite(SqlNode root, SqlRewriter rewriter);

    protected virtual SqlNode RewriteNode(SqlNode node) => node;
    protected virtual SqlSelectQuery RewriteSelectQuery(SqlSelectQuery query) => query;
    // ...按节点类型提供虚方法
}
```

约定：

- 默认返回原节点；需要修改时用 `with` 构造新节点。
- 子节点自底向上改写，父节点最后处理。
- 改写不修改原树，便于并发与回滚。
- 常用组合提供扩展方法：`WithWhere`、`WithLimit`、`ReplaceTable`、`AddSelectItem`。

使用示例：

```csharp
sealed class TenantFilter(string predicateSql) : SqlRewriter
{
    protected override SqlSelectQuery RewriteSelectQuery(SqlSelectQuery query)
    {
        var predicate = SqlExpressionParser.Parse(predicateSql);
        var where = query.Where is null
            ? predicate
            : new SqlBinaryExpression
              {
                  Left = query.Where,
                  Operator = SqlBinaryOperator.And,
                  Right = predicate,
              };
        return query with { Where = where };
    }
}

var rewritten = SqlRewriter.Rewrite(statement, new TenantFilter("u.tenant_id = @tenant"));
```

### 8.3 分析

```csharp
public sealed class SqlAnalysis
{
    public ImmutableArray<SqlTableReference> Tables { get; }
    public ImmutableArray<SqlColumnReference> Columns { get; }
    public ImmutableArray<SqlParameterReference> Parameters { get; }
    public SqlStatementFeatures Features { get; }
    public ImmutableArray<string> UnresolvedFunctions { get; }
}

public enum SqlFeatureState : byte
{
    No,       // 语法可证明不会发生
    Maybe,    // 无法排除
    Proven,   // 语法可证明会发生
}

public readonly record struct SqlStatementFeatures(
    SqlFeatureState ReadsData,
    SqlFeatureState ModifiesData,
    SqlFeatureState ReturnsResultSet);
```

特征判定示例：

| SQL | 返回结果集 | 修改数据 |
|-----|-----------|---------|
| `SELECT * FROM t` | Proven | No |
| `INSERT INTO x SELECT * FROM t` | No | Proven |
| `DELETE FROM t RETURNING *` | Proven | Proven |
| `WITH c AS (DELETE FROM t RETURNING *) SELECT * FROM c` | Proven | Proven |
| `SELECT nextval('s')` | Proven | Maybe |

分析边界：

- 只做**语法层**分析：表名、别名、列引用、参数、函数名。
- 不解析 `*` 展开、不解析视图、不做列级血缘的跨语句追踪。
- 函数是否纯由 `ISqlFunctionRegistry` 声明；未声明的函数不参与“无副作用”推断，列入 `UnresolvedFunctions`。

## 9. 反解析与格式化

### 9.1 Deparser

```csharp
public sealed class SqlDeparser
{
    public static string ToSql(SqlNode node, SqlDeparseOptions? options = null);
}

public sealed class SqlDeparseOptions
{
    public SqlDialect? Dialect { get; init; }        // null 时用 statement.Dialect
    public bool PreserveQuoting { get; init; } = true;
    public bool PreserveLiterals { get; init; } = true;
    public bool PreserveComments { get; init; }
}
```

- 使用 `SqlWriter`（内部池化 `StringBuilder` + 缩进状态）输出。
- 输出**最小必要括号**；需要时按优先级补括号，保证 `Parse(ToSql(ast))` 结构等价。
- 方言感知：分页写法、字符串前缀、引号字符、`RETURNING`/`OUTPUT` 按目标方言输出。
- `PreserveComments = true` 时按 token trivia 回填注释；默认关闭，保证输出稳定。

### 9.2 Formatter

```csharp
public sealed class SqlFormatOptions
{
    public SqlKeywordCase KeywordCase { get; init; } = SqlKeywordCase.Upper;
    public int IndentWidth { get; init; } = 4;
    public bool NewLineBeforeFrom { get; init; } = true;
    public bool AlignSelectItems { get; init; }
    public bool CommaFirst { get; init; }
}
```

Formatter 与 Deparser 分离：Deparser 负责“语义正确的 SQL”，Formatter 负责“好看”。两者可组合，也可单独使用。

### 9.3 往返契约

- `Parse(text)` → `ToSql()` → `Parse()`：两棵 AST 通过 `SqlAstComparer.StructuralEquals` 比较必须相等。
- 绑定参数、引号风格、字符串前缀在 `PreserveQuoting/PreserveLiterals` 开启时逐字保留。
- 注释默认不保留；开启后只保证注释存在，不保证位置完全一致。
- 语料库中的每条 SQL 都必须通过往返测试；新增语法必须同时新增语料。

## 10. 公共 API

### 10.1 门面

```csharp
namespace Framework.SqlParser;

public sealed class SqlParser
{
    public static SqlParser Default { get; }
    public SqlParser(SqlParserOptions options);

    public SqlStatement Parse(string sql);
    public bool TryParse(string sql,
                         out SqlStatement? statement,
                         out IReadOnlyList<SqlDiagnostic> diagnostics);

    public SqlBatchResult ParseBatch(string sql);
    public SqlExpression ParseExpression(string sql);
}

public sealed record SqlBatchResult(
    ImmutableArray<SqlStatement> Statements,
    ImmutableArray<SqlDiagnostic> Diagnostics);

public static class Sql
{
    public static SqlStatement Parse(string sql, SqlDialect? dialect = null);
    public static bool TryParse(string sql, out SqlStatement? statement, out IReadOnlyList<SqlDiagnostic> diagnostics, SqlDialect? dialect = null);
    public static string Format(string sql, SqlFormatOptions? options = null);
}

public static class SqlAnalyzer
{
    public static SqlAnalysis Analyze(SqlStatement statement);
}

public static class SqlExpressionParser
{
    public static SqlExpression Parse(string expressionText, SqlDialect? dialect = null);
}

public static class SqlNodeExtensions
{
    public static string ToSql(this SqlNode node, SqlDialect? dialect = null,
                               SqlDeparseOptions? options = null);
    public static string ToFormattedSql(this SqlNode node, SqlFormatOptions? options = null);
}
```

### 10.2 使用示例

```csharp
using Framework.SqlParser;
using Framework.SqlParser.Ast;

var parser = new SqlParser(new SqlParserOptions { Dialect = SqlDialect.PostgreSql });

// 1) 解析
var stmt = parser.Parse(
    "select u.id, count(*) from public.users u where u.age > $1 group by u.id");
var select = (SqlSelectStatement)stmt;
var query = select.Query;

// 2) 源码位置
Console.WriteLine(stmt.Span);                 // 源码区间
Console.WriteLine(query.From[0].Span.Start);  // 表名起始偏移

// 3) 回写
Console.WriteLine(stmt.ToSql());              // 规范化 SQL
Console.WriteLine(stmt.ToSql(SqlDialect.Generic));

// 4) 分析
var analysis = SqlAnalyzer.Analyze(stmt);
Console.WriteLine(analysis.Tables[0].Name);           // users
Console.WriteLine(analysis.Features.ModifiesData);    // No

// 5) 诊断
if (!parser.TryParse("select from", out var bad, out var diagnostics))
{
    foreach (var d in diagnostics)
        Console.WriteLine($"{d.Code} @{d.Line}:{d.Column} {d.Message}");
}
```

### 10.3 诊断与异常

```csharp
public enum SqlDiagnosticSeverity { Info, Warning, Error }

public sealed record SqlDiagnostic(
    string Code,
    SqlDiagnosticSeverity Severity,
    string Message,
    SqlSpan Span,
    int Line,
    int Column);

public sealed class SqlParseException : Exception
{
    public IReadOnlyList<SqlDiagnostic> Diagnostics { get; }
    public SqlSpan Span { get; }
}
```

诊断码见附录 C。

### 10.4 线程安全与资源

- `SqlParser` 自身不可变，可单例复用。
- 每次 `Parse` 从 `ArrayPool` 租用 token 缓冲与遍历缓冲，`finally` 归还。
- AST 不持有池化资源；`ToSql` 返回普通 `string`。
- 不使用 `CancellationToken`：单条 SQL 解析在微秒级，取消开销大于收益；`ParseBatch` 由调用方按需分批。

## 11. 性能设计

### 11.1 目标

| 指标 | 目标 |
|------|------|
| 词法吞吐 | >50 MB/s（单线程，ASCII 为主的 SQL） |
| 1KB 常见查询解析 | <50µs（Release，热路径） |
| 解析分配 | 除 AST 与诊断外，无额外大对象 |
| 深嵌套 | 128 层内正常，超限快速失败 |
| 批量解析 | 1000 条语句无异常泄漏、无明显 GC 抖动 |

### 11.2 关键手段

- 单遍词法，零正则、零 `Substring`、零 `string.Split`。
- Token 为 `readonly struct`，列表池化。
- 关键字用生成的枚举匹配，不做 `ToLower()`。
- 递归下降 + Pratt，避免回溯与全量重试。
- AST 使用 `record` + `ImmutableArray`，改写共享未修改子树。
- 诊断列表只在有错误时分配。
- `ConfigureAwait` 无关；库本身无异步。

### 11.3 禁止事项

- 禁止在解析热路径使用 LINQ。
- 禁止用异常做控制流（`TryParse` 不抛异常）。
- 禁止为每个 token 生成 `string`（仅标识符/字面量在构造 AST 时解码）。
- 禁止把解析树暴露为公共 API。

### 11.4 基准与门禁

- `test/Framework.SqlParser.Benchmarks/`：基于 `Stopwatch` 的轻量基准，不引入 BenchmarkDotNet 依赖。
- 语料：TPC-C/TPC-H 风格查询、各数据库建表脚本中的 DML、真实业务脱敏语句。
- CI 门禁：基准回归超过 15% 失败；分配量回归超过 20% 失败。
- 单元测试中使用 `[NotInParallel]` 隔离基准与 GC 敏感用例。

## 12. 工程落地

### 12.1 目录结构

```text
src/Framework.SqlParser/
├─ Framework.SqlParser.csproj
├─ SqlParser.cs
├─ SqlParserOptions.cs
├─ SqlDiagnostic.cs
├─ SqlSpan.cs
├─ Text/
│  ├─ SqlTokenizer.cs
│  ├─ SqlToken.cs
│  ├─ SqlKeyword.cs              // 源生成
│  └─ SqlTrivia.cs
├─ Ast/
│  ├─ SqlNode.cs
│  ├─ Statements/
│  ├─ Queries/
│  ├─ Sources/
│  ├─ Clauses/
│  └─ Expressions/
├─ Parsing/
│  ├─ ParserContext.cs
│  ├─ StatementParser.cs
│  ├─ QueryParser.cs
│  ├─ ExpressionParser.cs
│  └─ DmlParser.cs
├─ Dialects/
│  ├─ SqlDialect.cs
│  ├─ SqlDialectCapabilities.cs
│  └─ SqlDialects.cs
├─ Deparse/
│  ├─ SqlDeparser.cs
│  ├─ SqlWriter.cs
│  ├─ SqlDeparseOptions.cs
│  └─ SqlFormatOptions.cs
├─ Analysis/
│  ├─ SqlAnalyzer.cs
│  ├─ SqlAnalysis.cs
│  ├─ SqlStatementFeatures.cs
│  └─ SqlTableCollector.cs
├─ Rewrite/
│  ├─ SqlRewriter.cs
│  └─ SqlRewriterExtensions.cs
└─ Internal/
   ├─ SqlNodeBuffer.cs
   └─ SqlAstComparer.cs
```

### 12.2 csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>True</GenerateDocumentationFile>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <Version>1.0.0-alpha.1</Version>
    <PackageId>YLFramework.SqlParser</PackageId>
    <Description>纯 C# 的 SQL 解析引擎：统一 AST、方言配置、错误恢复、SQL 回写与改写、表/列分析与语句特征判定。</Description>
    <PackageTags>sql;parser;ast;sqlparser;rewrite;deparser;dialect</PackageTags>
  </PropertyGroup>
</Project>
```

无 NuGet 依赖；`Directory.Packages.props` 不需要新增条目。

### 12.3 测试结构

```text
test/Framework.SqlParser.Tests/
├─ Tokenizer/          关键字、引号、注释、字符串前缀、参数、错误恢复
├─ Expressions/        优先级、结合性、CASE、CAST、IN/BETWEEN/LIKE、窗口
├─ Statements/         SELECT/JOIN/CTE/集合运算/INSERT/UPDATE/DELETE
├─ Dialects/           6 种方言的差异语法与分页
├─ Deparse/            往返一致性、引号/字面量保真
├─ Analysis/           表/列/参数提取、特征三值
├─ Rewrite/            加条件、改表名、加 LIMIT、脱敏
├─ Diagnostics/        错误码、位置、恢复后的 AST
├─ Fuzz/               随机 token 变异不崩溃、不超时
└─ Fixtures/           语料与金样
```

测试约定：

- TUnit，`await Assert.That(...)` 风格。
- 语料按方言分目录，每个 `.sql` 文件对应一个期望结构或期望诊断快照。
- 往返测试用 `SqlAstComparer.StructuralEquals`。
- 模糊测试限制在固定随机种子与超时内，避免 CI 不稳定。

### 12.4 CI 检查项

1. `dotnet build Framework.slnx` 零警告（新增代码不得引入警告）。
2. `dotnet test test/Framework.SqlParser.Tests` 全绿。
3. 往返语料全部通过。
4. 模糊测试 smoke（固定种子，30 秒）。
5. 基准回归门禁。
6. `dotnet pack src/Framework.SqlParser` 成功，包内包含 README 与 XML 文档。

## 13. 里程碑

| 阶段 | 交付物 | 退出条件 |
|------|--------|---------|
| M0 | 项目骨架、`SqlSpan`、Token、Tokenizer、诊断 | 词法语料 100+ 通过；无正则；未闭合场景不崩溃 |
| M1 | Pratt 表达式、SELECT 单表、字面量/列/函数/参数 | 优先级与结合性表全覆盖；错误位置准确 |
| M2 | JOIN、子查询、CTE、集合运算、窗口、分页 | 复杂查询语料通过；深度限制生效 |
| M3 | INSERT/UPDATE/DELETE、RETURNING/OUTPUT | DML 语料通过；`INSERT ... SELECT` 正确分类 |
| M4 | Deparser + Formatter + 往返契约 | 全语料往返结构等价；引号/前缀保真 |
| M5 | 6 种方言能力表与差异语法 | 每方言至少 50 条语料通过；不支持语法给 `SQL2002` |
| M6 | Analyzer、Rewriter、性能与模糊测试 | 特征三值用例通过；基准达标；fuzz smoke 稳定 |

## 14. 风险与对策

| 风险 | 影响 | 对策 |
|------|------|------|
| 语法覆盖无底洞 | 工期不可控 | 明确支持度矩阵；raw fallback；按真实语料驱动补语法 |
| 方言开关膨胀 | 行为矩阵复杂 | 能力位 + 少量结构分支；每方言独立语料；禁止在解析器里到处 `if` |
| 性能与不可变 AST 冲突 | 改写分配偏高 | 共享未修改子树；`ImmutableArray` 结构共享；必要时提供可变 builder 内部使用 |
| AST 公共 API 不稳定 | 下游升级成本 | 节点命名先评审；字段变更走语义化版本；提供 `SqlAstComparer` 降低耦合 |
| 错误恢复产出错误树 | 下游误用 | `SqlInvalidStatement` / `SqlRawExpression` 明确标记；`TryParse` 返回诊断；文档强调检查 `Success` |
| LGPL 许可污染 | 法务风险 | 独立实现，不复制 JSqlParser 代码；如移植走 Apache-2.0 分支并保留 NOTICE |
| 源码区间与 trivia 内存开销 | 大 SQL 内存偏高 | `SqlSpan` 为 8 字节 struct；trivia 默认不挂树，按需从原文切片 |
| 生成器复杂度 | M6 延期 | 首版手写 `CollectChildren` / `With`，源生成器作为后续优化 |

## 附录 A：v1 节点清单

语句：`SqlSelectStatement`、`SqlInsertStatement`、`SqlUpdateStatement`、`SqlDeleteStatement`、`SqlMergeStatement`、`SqlInvalidStatement`。

查询：`SqlSelectQuery`、`SqlSetOperationQuery`、`SqlValuesQuery`、`SqlParenthesizedQuery`。

数据源：`SqlNamedTable`、`SqlDerivedTable`、`SqlJoinedTable`、`SqlTableFunction`、`SqlValuesTable`。

子句：`SqlWithClause`、`SqlCommonTableExpression`、`SqlSelectItem`、`SqlOrderByClause`、`SqlOrderByItem`、`SqlGroupByClause`、`SqlHavingClause`、`SqlLimitClause`、`SqlOffsetClause`、`SqlFetchClause`、`SqlTopClause`、`SqlWindowClause`、`SqlWindowSpec`、`SqlReturningClause`、`SqlOutputClause`、`SqlConnectByClause`、`SqlQualifyClause`。

表达式：`SqlLiteral`、`SqlIdentifier`、`SqlColumnRef`、`SqlParameterRef`、`SqlBinaryExpression`、`SqlUnaryExpression`、`SqlFunctionCall`、`SqlCaseExpression`、`SqlCastExpression`、`SqlSubqueryExpression`、`SqlExistsExpression`、`SqlInExpression`、`SqlBetweenExpression`、`SqlLikeExpression`、`SqlIsNullExpression`、`SqlTupleExpression`、`SqlArrayExpression`、`SqlSubscriptExpression`、`SqlCollateExpression`、`SqlAtTimeZoneExpression`、`SqlRawExpression`。

## 附录 B：表达式优先级速查

```text
低  OR
    AND
    NOT
    = <> < <= > >=  IS  IN  BETWEEN  LIKE  ILIKE
    AT TIME ZONE  COLLATE
    ||
    + -
    * / %
    一元 + - ~
高  ::  []  .  函数调用  OVER  FILTER  WITHIN GROUP
```

规则：

- 比较类运算符非结合，`a = b = c` 报 `SQL2004`（需要显式括号）。
- `NOT` 的操作数按比较级解析，因此 `NOT a = b` 等价于 `NOT (a = b)`。
- `BETWEEN` 的 `AND` 由 `BETWEEN` 分支消费，不参与布尔 AND。
- 后缀运算符（`::`、`[]`、`.`、`OVER`）优先级最高，先于所有二元运算结合。

## 附录 C：诊断码

| 编码 | 含义 | 级别 |
|------|------|------|
| SQL1001 | 无法识别的字符/标记 | Error |
| SQL1002 | 未闭合的字符串、注释或引号标识符 | Error |
| SQL1003 | 非法数字字面量 | Error |
| SQL1004 | 非法参数写法 | Error |
| SQL1005 | 意外的输入结束 | Error |
| SQL1006 | 嵌套深度超限 | Error |
| SQL1007 | 输入长度超限 | Error |
| SQL2001 | 不支持的语句类型 | Error |
| SQL2002 | 当前方言不支持该语法 | Error |
| SQL2003 | 已按原始文本保留的片段 | Warning |
| SQL2004 | 非结合运算符连用，需要括号 | Error |
| SQL2005 | 冗余或缺失的括号（可自动修复） | Warning |
