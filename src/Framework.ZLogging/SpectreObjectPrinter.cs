using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Framework.ZLogging;

/// <summary>对象转文本表格时的排布方向。</summary>
public enum SpectreTableLayout { Auto, Vertical, Horizontal }

/// <summary>对象转文本表格时的边框样式。</summary>
public enum SpectreTableBorderStyle { Rounded, Square, Ascii, None, Heavy, Double, Minimal, Markdown }

/// <summary>集合被截断时保留哪一段。</summary>
public enum SpectreTruncationMode { Head, Tail, HeadAndTail }

/// <summary>成员可见性筛选。</summary>
public sealed class SpectreMemberOptions
{
    /// <summary>是否读取属性。</summary>
    public bool IncludeProperties { get; set; } = true;

    /// <summary>是否读取字段。</summary>
    public bool IncludeFields { get; set; }

    /// <summary>是否读取非公开成员。</summary>
    public bool IncludeNonPublicMembers { get; set; }

    /// <summary>是否读取公开成员。</summary>
    public bool IncludePublicMembers { get; set; } = true;

    /// <summary>是否读取虚成员（属性 getter / 索引器）。</summary>
    public bool IncludeVirtualMembers { get; set; } = true;

    /// <summary>成员过滤器，返回 <c>false</c> 跳过该成员。</summary>
    public Func<MemberInfo, bool>? MemberFilter { get; set; }
}

/// <summary>表格外观配置。</summary>
public sealed class SpectreTableOptions
{
    /// <summary>排布方向。</summary>
    public SpectreTableLayout Layout { get; set; } = SpectreTableLayout.Vertical;

    /// <summary>边框样式。</summary>
    public SpectreTableBorderStyle BorderStyle { get; set; } = SpectreTableBorderStyle.Rounded;

    /// <summary>是否为成员列显示类型名。</summary>
    public bool ShowMemberTypes { get; set; }

    /// <summary>是否显示行分隔线。</summary>
    public bool ShowRowSeparators { get; set; }

    /// <summary>是否显示表头。</summary>
    public bool ShowTableHeaders { get; set; } = true;

    /// <summary>是否为集合项显示索引列。</summary>
    public bool ShowRowIndices { get; set; } = true;

    /// <summary>是否禁止列内换行。</summary>
    public bool NoColumnWrapping { get; set; }

    /// <summary>是否撑满可用宽度。</summary>
    public bool ExpandTables { get; set; }
}

/// <summary>类型名呈现配置。</summary>
public sealed class SpectreTypeNameOptions
{
    /// <summary>是否显示类型名。</summary>
    public bool ShowTypeNames { get; set; } = true;

    /// <summary>是否使用 <c>int</c> 之类的别名而非 <c>Int32</c>。</summary>
    public bool UseAliases { get; set; } = true;

    /// <summary>是否使用带命名空间的完整类型名。</summary>
    public bool UseFullName { get; set; }

    /// <summary>是否把匿名类型简化为 <c>Anonymous</c>。</summary>
    public bool SimplifyAnonymousObjectNames { get; set; } = true;

    /// <summary>泛型参数之间是否用空格分隔。</summary>
    public bool SeparateTypesWithSpace { get; set; } = true;
}

/// <summary>标量值的呈现配置。</summary>
public sealed class SpectreTypeRenderingOptions
{
    /// <summary>是否给字符串加引号。</summary>
    public bool QuoteStringValues { get; set; }

    /// <summary>是否给字符加引号。</summary>
    public bool QuoteCharValues { get; set; }

    /// <summary>字符串引号字符。</summary>
    public char StringQuotationChar { get; set; } = '"';

    /// <summary>字符引号字符。</summary>
    public char CharQuotationChar { get; set; } = '\'';
}

/// <summary>集合截断配置。</summary>
public sealed class SpectreTruncationOptions
{
    int _maxCollectionCount = 50;

    /// <summary>集合最多展示多少项。默认 50，避免大集合把日志撑爆。</summary>
    public int MaxCollectionCount
    {
        get => _maxCollectionCount;
        set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); _maxCollectionCount = value; }
    }

    /// <summary>截断时保留哪一段。</summary>
    public SpectreTruncationMode Mode { get; set; }

    /// <summary>多维数组是否按维度分别截断。</summary>
    public bool PerDimension { get; set; } = true;
}

/// <summary>对象转储的总体配置。</summary>
public sealed class SpectreDumpOptions
{
    int _maxDepth = 7;

    /// <summary>递归最大深度。默认 7。</summary>
    public int MaxDepth
    {
        get => _maxDepth;
        set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); _maxDepth = value; }
    }

    /// <summary>成员筛选配置。</summary>
    public SpectreMemberOptions Members { get; } = new();

    /// <summary>表格配置。</summary>
    public SpectreTableOptions Table { get; } = new();

    /// <summary>类型名配置。</summary>
    public SpectreTypeNameOptions TypeNames { get; } = new();

    /// <summary>标量呈现配置。</summary>
    public SpectreTypeRenderingOptions TypeRendering { get; } = new();

    /// <summary>截断配置。</summary>
    public SpectreTruncationOptions Truncation { get; } = new();
}

/// <summary>
/// 把任意对象渲染为 Spectre.Console 可渲染对象 / 纯文本。
/// 按类型注册呈现方式，后注册的匹配规则优先，并支持沿基类与接口回退。
/// </summary>
public sealed class SpectreObjectPrinter
{
    readonly ConcurrentStack<Func<object, IRenderable?>> _renderers;
    readonly SpectreTableLayout? _layoutOverride;
    readonly int _widthOverride;

    /// <summary>转储配置。</summary>
    public SpectreDumpOptions DumpOptions { get; }

    /// <summary>使用默认宽度（跟随终端）创建打印器。</summary>
    public SpectreObjectPrinter()
    {
        _renderers = new();
        DumpOptions = new();
    }

    SpectreObjectPrinter(SpectreObjectPrinter source, SpectreTableLayout layout, int width)
    {
        _renderers = source._renderers;
        DumpOptions = source.DumpOptions;
        _layoutOverride = layout;
        _widthOverride = width;
    }

    /// <summary>
    /// 派生一个绑定固定渲染宽度的打印器。
    /// 宽度 <c>0</c> 表示不折行，写日志文件时使用可避免表格被切断。
    /// </summary>
    public SpectreObjectPrinter WithWidth(int width)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        return new SpectreObjectPrinter(this, _layoutOverride ?? DumpOptions.Table.Layout, width);
    }

    /// <summary>
    /// 为类型 <typeparamref name="T"/> 注册自定义呈现方式；后注册者优先于先注册者。
    /// </summary>
    public SpectreObjectPrinter Register<T>(Func<T, IRenderable> render)
    {
        ArgumentNullException.ThrowIfNull(render);
        _renderers.Push(value => value is T typed ? render(typed) : null);
        return this;
    }

    /// <summary>是否为可直接内联显示的标量。</summary>
    public static bool IsScalar(object? value) => value is null or string or char or decimal
        or DateTime or DateTimeOffset or DateOnly or TimeOnly or TimeSpan or Guid or Uri
        or Type or Delegate || value.GetType().IsPrimitive || value.GetType().IsEnum;

    /// <summary>按终端实际宽度渲染。</summary>
    public IRenderable Render(object? value) => Render(value, AnsiConsole.Profile.Width);

    /// <summary>按指定排布方向与终端宽度渲染。</summary>
    public IRenderable Render(object? value, SpectreTableLayout layout)
        => new SpectreObjectPrinter(this, layout, AnsiConsole.Profile.Width).RenderCore(value);

    /// <summary>
    /// 按指定宽度渲染。宽度 <c>0</c> 表示不换行——写文件时使用，
    /// 避免表格被日志文件宽度折断。
    /// </summary>
    public IRenderable Render(object? value, int width)
        => new SpectreObjectPrinter(this, _layoutOverride ?? DumpOptions.Table.Layout, width).RenderCore(value);

    /// <summary>本打印器绑定的渲染宽度；<c>0</c> 表示不折行。</summary>
    internal int Width => _widthOverride;

    /// <summary>按当前打印器的宽度渲染另一个对象，供嵌套渲染时传递宽度。</summary>
    public IRenderable Render(object? value, SpectreTableLayout layout, int width)
        => new SpectreObjectPrinter(this, layout, width).RenderCore(value);

    IRenderable RenderCore(object? value) => RenderValue(value, 0,
        new HashSet<object>(ReferenceEqualityComparer.Instance));

    SpectreTableLayout Layout => _layoutOverride ?? DumpOptions.Table.Layout;

    IRenderable RenderValue(object? value, int depth, HashSet<object> ancestors)
    {
        if (value is null) return Hint("null");
        try
        {
            foreach (var renderer in _renderers)
                if (renderer(value) is { } result) return result;
            if (value is IRenderable renderable) return renderable;
            var trackReference = !value.GetType().IsValueType && !IsScalar(value);
            if (trackReference && ancestors.Contains(value)) return Hint($"<循环引用: {TypeName(value.GetType())}>");
            if (!IsScalar(value) && depth >= DumpOptions.MaxDepth) return Hint($"<达到最大深度: {TypeName(value.GetType())}>");
            if (trackReference) ancestors.Add(value);
            try
            {
                if (!(value is Array { Rank: 1 } && Layout is SpectreTableLayout.Horizontal or SpectreTableLayout.Auto)
                    && TryRenderSpecial(value, depth, ancestors, out var special)) return special;
                if (IsScalar(value)) return Scalar(value);
                return value is IEnumerable enumerable
                    ? RenderCollection(value, enumerable, depth, ancestors)
                    : RenderMembers(value, depth, ancestors);
            }
            finally { if (trackReference) ancestors.Remove(value); }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Error("对象打印失败", exception);
        }
    }

    Table NewTable(Type type, params string[] columns)
    {
        var options = DumpOptions.Table;
        var border = options.BorderStyle switch
        {
            SpectreTableBorderStyle.Square => TableBorder.Square,
            SpectreTableBorderStyle.Ascii => TableBorder.Ascii,
            SpectreTableBorderStyle.None => TableBorder.None,
            SpectreTableBorderStyle.Heavy => TableBorder.Heavy,
            SpectreTableBorderStyle.Double => TableBorder.Double,
            SpectreTableBorderStyle.Minimal => TableBorder.Minimal,
            SpectreTableBorderStyle.Markdown => TableBorder.Markdown,
            _ => TableBorder.Rounded
        };
        var table = new Table().Border(border);
        table.ShowRowSeparators = options.ShowRowSeparators;
        table.ShowHeaders = options.ShowTableHeaders;
        table.Expand = options.ExpandTables;
        if (DumpOptions.TypeNames.ShowTypeNames) table.Title = new TableTitle(TypeName(type).EscapeMarkup());
        foreach (var column in columns)
            table.AddColumn(new TableColumn(new Text(column, new Style(Color.Aqua))) { NoWrap = options.NoColumnWrapping });
        return table;
    }

    List<(MemberInfo Member, Type Type, Func<object?> Read)> GetMembers(object value)
    {
        var options = DumpOptions.Members;
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var members = new List<(MemberInfo, Type, Func<object?>)>();
        if (options.IncludeProperties)
            foreach (var property in value.GetType().GetProperties(flags))
            {
                var getter = property.GetGetMethod(true);
                if (getter is null || getter.IsStatic || property.GetIndexParameters().Length != 0
                    || !Visible(getter.IsPublic, getter.IsVirtual) || !Accepted(property)) continue;
                members.Add((property, property.PropertyType, () => property.GetValue(value)));
            }
        if (options.IncludeFields)
            foreach (var field in value.GetType().GetFields(flags))
            {
                if (field.IsStatic || !Visible(field.IsPublic, false) || !Accepted(field)) continue;
                members.Add((field, field.FieldType, () => field.GetValue(value)));
            }
        return members;

        bool Visible(bool isPublic, bool isVirtual)
            => (isPublic ? options.IncludePublicMembers : options.IncludeNonPublicMembers)
               && (!isVirtual || options.IncludeVirtualMembers);

        bool Accepted(MemberInfo member) => options.MemberFilter?.Invoke(member) ?? true;
    }

    IRenderable RenderMember(Func<object?> read, int depth, HashSet<object> ancestors)
    {
        try { return RenderValue(read(), depth, ancestors); }
        catch (Exception exception) when (exception is not OutOfMemoryException) { return Error("成员读取失败", exception); }
    }

    IRenderable RenderMembers(object value, int depth, HashSet<object> ancestors)
    {
        var members = GetMembers(value);
        if (Layout == SpectreTableLayout.Horizontal && members.Count > 0)
        {
            var horizontal = NewTable(value.GetType(),
                members.Select(m => MemberHeader(m.Member.Name, m.Type)).ToArray());
            horizontal.AddRow(members.Select(m => RenderMember(m.Read, depth + 1, ancestors)).ToArray());
            return horizontal;
        }

        var table = DumpOptions.Table.ShowMemberTypes
            ? NewTable(value.GetType(), "成员", "类型", "值")
            : NewTable(value.GetType(), "成员", "值");
        foreach (var member in members)
        {
            var rendered = RenderMember(member.Read, depth + 1, ancestors);
            if (DumpOptions.Table.ShowMemberTypes) table.AddRow(new Text(member.Member.Name), new Text(TypeName(member.Type)), rendered);
            else table.AddRow(new Text(member.Member.Name), rendered);
        }
        if (members.Count == 0) AddNotice(table, Hint("<无可显示成员>"));
        return table;
    }

    string MemberHeader(string name, Type type)
        => DumpOptions.Table.ShowMemberTypes ? $"{name} ({TypeName(type)})" : name;

    static void AddNotice(Table table, IRenderable notice)
        => table.AddRow(Enumerable.Range(0, table.Columns.Count).Select(i => i == 0 ? notice : new Text("")).ToArray());

    // 截断策略：长度未知或懒加载的枚举最多只读取 Max+1 项，绝不把整个集合物化到内存。
    IEnumerable<int> TruncatedIndices(int count)
    {
        var max = DumpOptions.Truncation.MaxCollectionCount;
        if (count <= max) return Enumerable.Range(0, count);
        return DumpOptions.Truncation.Mode switch
        {
            SpectreTruncationMode.Tail => Enumerable.Range(count - max, max),
            SpectreTruncationMode.HeadAndTail => Enumerable.Range(0, max / 2)
                .Concat(Enumerable.Range(count - (max - max / 2), max - max / 2)),
            _ => Enumerable.Range(0, max)
        };
    }

    (List<(int Index, object? Value)> Items, bool Truncated, string? Note, Exception? Error)
        ReadCollection(IEnumerable collection)
    {
        var items = new List<(int, object?)>();
        var max = DumpOptions.Truncation.MaxCollectionCount;
        int? knownCount = collection is ICollection counted ? counted.Count : null;
        if (knownCount is null)
        {
            var countInterface = collection.GetType().GetInterfaces().FirstOrDefault(t =>
                t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
                                    || t.GetGenericTypeDefinition() == typeof(ICollection<>)));
            if (countInterface?.GetProperty("Count")?.GetValue(collection) is int count) knownCount = count;
        }

        var mode = DumpOptions.Truncation.Mode;
        var selected = knownCount is int length ? TruncatedIndices(length).ToHashSet() : null;
        string? note = mode != SpectreTruncationMode.Head && knownCount is null
            ? "<长度未知，已安全回退为前部截断>" : null;

        IEnumerator? iterator = null;
        Exception? error = null;
        var truncated = knownCount > max;
        try
        {
            iterator = collection.GetEnumerator();
            var lastNeeded = selected?.DefaultIfEmpty(-1).Max() ?? max - 1;
            for (var index = 0; index <= lastNeeded; index++)
            {
                if (!iterator.MoveNext()) break;
                if (selected is null || selected.Contains(index)) items.Add((index, iterator.Current));
            }
            if (knownCount is null && items.Count == max) truncated = iterator.MoveNext();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException) { error = exception; }
        finally
        {
            try { (iterator as IDisposable)?.Dispose(); }
            catch (Exception exception) when (exception is not OutOfMemoryException) { error ??= exception; }
        }
        return (items, truncated, note, error);
    }

    IRenderable RenderCollection(object value, IEnumerable collection, int depth, HashSet<object> ancestors)
    {
        var dictionary = value is IDictionary || value.GetType().GetInterfaces().Any(t =>
            t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                                || t.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>)));
        var result = ReadCollection(collection);
        if (!dictionary && Layout is SpectreTableLayout.Horizontal or SpectreTableLayout.Auto
            && TryHorizontalCollection(value, result.Items, depth, ancestors, out var horizontal))
        {
            AppendCollectionNotices(horizontal, result.Truncated, result.Note, result.Error, result.Items.Count);
            return horizontal;
        }

        var indexed = !dictionary && DumpOptions.Table.ShowRowIndices;
        var table = dictionary
            ? NewTable(value.GetType(), "键", "值")
            : indexed ? NewTable(value.GetType(), "索引", "值") : NewTable(value.GetType(), "值");
        var previous = -1;
        foreach (var (index, item) in result.Items)
        {
            if (index > previous + 1) AddNotice(table, Hint("…"));
            previous = index;
            if (dictionary)
            {
                try
                {
                    var key = item is DictionaryEntry entry ? entry.Key : item?.GetType().GetProperty("Key")?.GetValue(item);
                    var entryValue = item is DictionaryEntry pair ? pair.Value : item?.GetType().GetProperty("Value")?.GetValue(item);
                    table.AddRow(RenderValue(key, depth + 1, ancestors), RenderValue(entryValue, depth + 1, ancestors));
                }
                catch (Exception exception) when (exception is not OutOfMemoryException)
                {
                    AddNotice(table, Error("字典读取失败", exception));
                }
            }
            else if (indexed)
                table.AddRow(new Text(index.ToString(CultureInfo.InvariantCulture)), RenderValue(item, depth + 1, ancestors));
            else
                table.AddRow(RenderValue(item, depth + 1, ancestors));
        }
        AppendCollectionNotices(table, result.Truncated, result.Note, result.Error, result.Items.Count);
        return table;
    }

    bool TryHorizontalCollection(object value, List<(int Index, object? Value)> items, int depth,
        HashSet<object> ancestors, out Table table)
    {
        table = null!;
        var prototype = items.FirstOrDefault(i => i.Value is not null).Value;
        if (prototype is null || IsScalar(prototype) || prototype is IEnumerable or IRenderable || !_renderers.IsEmpty) return false;
        var type = prototype.GetType();
        if (prototype is Task or System.Runtime.CompilerServices.ITuple or DataTable or DataSet or MemberInfo or DBNull) return false;
        for (var baseType = type; baseType is not null; baseType = baseType.BaseType)
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Lazy<>)) return false;
        if (items.Any(i => i.Value is { } instance && instance.GetType() != type)) return false;

        var members = GetMembers(prototype);
        if (members.Count == 0) return false;

        var indexed = DumpOptions.Table.ShowRowIndices;
        var columns = members.Select(m => MemberHeader(m.Member.Name, m.Type));
        table = NewTable(value.GetType(), (indexed ? new[] { "索引" }.Concat(columns) : columns).ToArray());

        var previous = -1;
        foreach (var (index, instance) in items)
        {
            if (index > previous + 1) AddNotice(table, Hint("…"));
            previous = index;
            var cells = new List<IRenderable>();
            if (indexed) cells.Add(new Text(index.ToString(CultureInfo.InvariantCulture)));
            if (instance is null || ancestors.Contains(instance) || depth + 1 >= DumpOptions.MaxDepth)
            {
                cells.Add(instance is null ? Hint("null")
                    : Hint(ancestors.Contains(instance) ? "<循环引用>" : "<达到最大深度>"));
                while (cells.Count < table.Columns.Count) cells.Add(new Text(""));
            }
            else
            {
                var track = !type.IsValueType;
                if (track) ancestors.Add(instance);
                try
                {
                    foreach (var member in members)
                        cells.Add(RenderMember(() => member.Member switch
                        {
                            PropertyInfo property => property.GetValue(instance),
                            FieldInfo field => field.GetValue(instance),
                            _ => null
                        }, depth + 2, ancestors));
                }
                finally { if (track) ancestors.Remove(instance); }
            }
            table.AddRow(cells.ToArray());
        }
        return true;
    }

    static void AppendCollectionNotices(Table table, bool truncated, string? note, Exception? error, int count)
    {
        if (truncated) AddNotice(table, Hint($"<已截断，仅显示 {count} 项>"));
        if (note is not null) AddNotice(table, Hint(note));
        if (error is not null) AddNotice(table, Error("集合读取失败", error));
        if (count == 0 && error is null) AddNotice(table, Hint("<空集合>"));
    }

    Text Scalar(object value)
    {
        var options = DumpOptions.TypeRendering;
        var text = value switch
        {
            string textValue when options.QuoteStringValues
                => $"{options.StringQuotationChar}{textValue}{options.StringQuotationChar}",
            string textValue => textValue,
            char character when options.QuoteCharValues
                => $"{options.CharQuotationChar}{character}{options.CharQuotationChar}",
            Type type => TypeName(type),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
            _ => value.ToString() ?? ""
        };
        return new Text(text, new Style(value is string or char ? Color.Green : Color.Yellow));
    }

    static Text Hint(string text) => new(text, new Style(Color.Grey));

    static Text Error(string prefix, Exception exception)
    {
        if (exception is TargetInvocationException { InnerException: { } inner }) exception = inner;
        return new Text($"<{prefix}: {exception.GetType().Name}: {exception.Message}>", new Style(Color.Red));
    }

    string TypeName(Type type)
    {
        var options = DumpOptions.TypeNames;
        if (type.IsArray) return TypeName(type.GetElementType()!) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        if (options.UseAliases)
        {
            var alias = Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => "bool",
                TypeCode.Byte => "byte",
                TypeCode.SByte => "sbyte",
                TypeCode.Int16 => "short",
                TypeCode.UInt16 => "ushort",
                TypeCode.Int32 => "int",
                TypeCode.UInt32 => "uint",
                TypeCode.Int64 => "long",
                TypeCode.UInt64 => "ulong",
                TypeCode.Single => "float",
                TypeCode.Double => "double",
                TypeCode.Decimal => "decimal",
                TypeCode.Char => "char",
                TypeCode.String => "string",
                _ => null
            };
            if (!type.IsEnum && alias is not null) return alias;
            if (type == typeof(object)) return "object";
            if (type == typeof(void)) return "void";
        }
        if (options.SimplifyAnonymousObjectNames && type.Name.Contains("AnonymousType", StringComparison.Ordinal)) return "Anonymous";
        var name = options.UseFullName ? type.FullName ?? type.Name : type.Name;
        if (!type.IsGenericType) return name;
        var tick = name.IndexOf('`');
        return (tick < 0 ? name : name[..tick]) + "<"
            + string.Join(options.SeparateTypesWithSpace ? ", " : ",", type.GetGenericArguments().Select(TypeName)) + ">";
    }

    bool TryRenderSpecial(object value, int depth, HashSet<object> ancestors, out IRenderable rendered)
    {
        rendered = value switch
        {
            Array array => RenderSpecialArray(array, depth, ancestors),
            DataTable table => RenderDataTable(table, depth, ancestors),
            DataSet dataSet => RenderDataSet(dataSet, depth, ancestors),
            System.Runtime.CompilerServices.ITuple tuple => RenderTuple(tuple, value.GetType(), depth, ancestors),
            Enum enumeration => SpecialText($"{TypeName(value.GetType())}.{enumeration:G} ({enumeration:D})"),
            Guid guid => SpecialText($"[{guid:D}]"),
            DateTime date => SpecialText($"[{date.ToString("O", CultureInfo.InvariantCulture)}; {date.Kind}]"),
            DateTimeOffset date => SpecialText($"[{date.ToString("O", CultureInfo.InvariantCulture)}]"),
            DateOnly date => SpecialText($"[{date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}]"),
            TimeOnly time => SpecialText($"[{time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture)}]"),
            TimeSpan time => SpecialText($"[{time.ToString("c", CultureInfo.InvariantCulture)}]"),
            Type type => SpecialText($"typeof({TypeName(type)})"),
            MemberInfo member => RenderMemberSignature(member),
            Task task => RenderTask(task, depth, ancestors),
            DBNull => Hint("null (DBNull)"),
            _ => null!
        };
        if (rendered is not null) return true;
        for (var type = value.GetType(); type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Lazy<>)) continue;
            rendered = RenderLazy(value, type, depth, ancestors);
            return true;
        }
        return false;
    }

    static Text SpecialText(string text) => new(text, new Style(Color.Yellow));

    IRenderable SpecialCell(Func<object?> read, int depth, HashSet<object> ancestors)
    {
        try { return RenderValue(read(), depth + 1, ancestors); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        { return Error("值读取失败", exception); }
    }

    IRenderable RenderSpecialArray(Array array, int depth, HashSet<object> ancestors)
    {
        // 同类型元素的数组（典型如对象数组）应按「列 = 成员」布局，
        // 否则只会得到一堆 [i] 坐标行，完全看不出是张表。
        if (TryRenderArrayAsTable(array, depth, ancestors, out var asTable)) return asTable;

        var table = NewTable(array.GetType(), "坐标", "值");
        if (array.Length == 0)
        {
            table.AddRow(Hint("<空数组>"), new Text(""));
            return table;
        }

        var limit = DumpOptions.Truncation.MaxCollectionCount;
        var coordinates = new int[array.Rank];
        var displayed = 0;
        if (DumpOptions.Truncation.PerDimension)
        {
            var selected = Enumerable.Range(0, array.Rank)
                .Select(axis => TruncatedIndices(array.GetLength(axis)).ToArray()).ToArray();
            var positions = new int[array.Rank];
            while (displayed < limit)
            {
                for (var axis = 0; axis < array.Rank; axis++)
                    coordinates[axis] = array.GetLowerBound(axis) + selected[axis][positions[axis]];
                AddCoordinate();
                var dimension = array.Rank - 1;
                while (dimension >= 0 && ++positions[dimension] == selected[dimension].Length)
                    positions[dimension--] = 0;
                if (dimension < 0) break;
            }
        }
        else
        {
            foreach (var flatIndex in TruncatedIndices(array.Length))
            {
                var remainder = flatIndex;
                for (var axis = array.Rank - 1; axis >= 0; axis--)
                {
                    coordinates[axis] = array.GetLowerBound(axis) + remainder % array.GetLength(axis);
                    remainder /= array.GetLength(axis);
                }
                AddCoordinate();
            }
        }

        if (displayed < array.Length)
            table.AddRow(Hint("…"), Hint($"<显示 {displayed}/{array.Length} 个元素；总上限 {limit}，PerDimension={DumpOptions.Truncation.PerDimension}>"));
        return table;

        void AddCoordinate()
        {
            table.AddRow(new Text("[" + string.Join(",", coordinates) + "]"),
                SpecialCell(() => array.GetValue(coordinates), depth, ancestors));
            displayed++;
        }
    }

    /// <summary>
    /// 把元素类型一致的对象数组渲染成「列 = 成员、行 = 元素」的表格。
    /// 仅在元素都能反射出成员时启用，否则退回坐标视图。
    /// </summary>
    bool TryRenderArrayAsTable(Array array, int depth, HashSet<object> ancestors, out IRenderable rendered)
    {
        rendered = null!;
        var elementType = array.GetType().GetElementType();
        if (elementType is null || elementType.IsPrimitive || IsScalarType(elementType)) return false;
        if (array.Length == 0) return false;

        var limit = DumpOptions.Truncation.MaxCollectionCount;
        var indices = TruncatedIndices(array.Length).ToArray();
        var first = array.GetValue(indices[0]);
        if (first is null) return false;
        if (indices.Any(i => array.GetValue(i)?.GetType() != first.GetType())) return false;

        var members = GetMembers(first);
        if (members.Count == 0) return false;

        var indexed = DumpOptions.Table.ShowRowIndices;
        var columns = members.Select(m => MemberHeader(m.Member.Name, m.Type));
        var table = NewTable(array.GetType(), (indexed ? new[] { "索引" }.Concat(columns) : columns).ToArray());

        foreach (var index in indices)
        {
            var instance = array.GetValue(index);
            var cells = new List<IRenderable>();
            if (indexed) cells.Add(new Text(index.ToString(CultureInfo.InvariantCulture)));

            if (instance is null || ancestors.Contains(instance))
            {
                cells.Add(instance is null ? Hint("null") : Hint("<循环引用>"));
                while (cells.Count < table.Columns.Count) cells.Add(new Text(""));
            }
            else
            {
                var track = !instance.GetType().IsValueType;
                if (track) ancestors.Add(instance);
                try
                {
                    foreach (var member in members)
                        cells.Add(RenderMember(() => member.Member switch
                        {
                            PropertyInfo property => property.GetValue(instance),
                            FieldInfo field => field.GetValue(instance),
                            _ => null
                        }, depth + 2, ancestors));
                }
                finally { if (track) ancestors.Remove(instance); }
            }
            table.AddRow(cells.ToArray());
        }

        if (indices.Length < array.Length)
            table.AddRow(Enumerable.Range(0, table.Columns.Count)
                .Select(i => i == 0 ? (IRenderable)Hint($"<仅显示 {indices.Length}/{array.Length} 项>") : new Text(""))
                .ToArray());

        rendered = table;
        return true;
    }

    static bool IsScalarType(Type type) => type.IsPrimitive || type.IsEnum
        || type == typeof(string) || type == typeof(char) || type == typeof(decimal)
        || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset)
        || type == typeof(TimeSpan) || type == typeof(Uri) || type == typeof(Type);

    IRenderable RenderTuple(System.Runtime.CompilerServices.ITuple tuple, Type type, int depth, HashSet<object> ancestors)
    {
        var table = NewTable(type, "Item", "值");
        foreach (var index in TruncatedIndices(tuple.Length))
            table.AddRow(new Text($"Item{index + 1}"), SpecialCell(() => tuple[index], depth, ancestors));
        if (tuple.Length == 0) table.AddRow(Hint("<空元组>"), new Text(""));
        else if (tuple.Length > DumpOptions.Truncation.MaxCollectionCount)
            table.AddRow(Hint("…"), Hint($"<元组共 {tuple.Length} 项>"));
        return table;
    }

    IRenderable RenderDataTable(DataTable source, int depth, HashSet<object> ancestors)
    {
        var limit = DumpOptions.Truncation.MaxCollectionCount;
        var columnIndices = TruncatedIndices(source.Columns.Count).ToArray();
        var columnNames = new List<string> { "行" };
        columnNames.AddRange(columnIndices.Select(i => source.Columns[i].ColumnName));
        var truncatedColumns = columnIndices.Length < source.Columns.Count;
        columnNames.Add("…");
        var table = NewTable(source.GetType(), columnNames.ToArray());
        if (source.TableName.Length > 0) table.Title = new TableTitle(source.TableName.EscapeMarkup());

        var cellCount = 0;
        var rowsShown = 0;
        var totalBudget = DumpOptions.Truncation.PerDimension ? (long)limit * limit : limit;
        foreach (var rowIndex in TruncatedIndices(source.Rows.Count))
        {
            if (cellCount >= totalBudget) break;
            var cells = new List<IRenderable> { new Text(rowIndex.ToString(CultureInfo.InvariantCulture)) };
            foreach (var columnIndex in columnIndices)
            {
                if (cellCount >= totalBudget) cells.Add(Hint("…"));
                else
                {
                    cells.Add(SpecialCell(() => source.Rows[rowIndex][columnIndex], depth, ancestors));
                    cellCount++;
                }
            }
            if (truncatedColumns) cells.Add(Hint("<列已截断>"));
            table.AddRow(cells.ToArray());
            rowsShown++;
        }

        if (rowsShown == 0 || rowsShown < source.Rows.Count || truncatedColumns)
        {
            var text = source.Rows.Count == 0
                ? "<空表>"
                : $"<显示 {rowsShown}/{source.Rows.Count} 行、{columnIndices.Length}/{source.Columns.Count} 列>";
            table.AddRow(Enumerable.Range(0, columnNames.Count)
                .Select(i => i == 0 ? (IRenderable)Hint(text) : new Text("")).ToArray());
        }
        return table;
    }

    IRenderable RenderDataSet(DataSet source, int depth, HashSet<object> ancestors)
    {
        var table = NewTable(source.GetType(), "表", "值");
        if (source.DataSetName.Length > 0) table.Title = new TableTitle(source.DataSetName.EscapeMarkup());
        foreach (var index in TruncatedIndices(source.Tables.Count))
        {
            var dataTable = source.Tables[index];
            table.AddRow(new Text(dataTable.TableName), SpecialCell(() => dataTable, depth, ancestors));
        }
        if (source.Tables.Count == 0) table.AddRow(Hint("<空 DataSet>"), new Text(""));
        else if (source.Tables.Count > DumpOptions.Truncation.MaxCollectionCount)
            table.AddRow(Hint("…"), Hint($"<共 {source.Tables.Count} 张表>"));
        return table;
    }

    IRenderable RenderTask(Task task, int depth, HashSet<object> ancestors)
    {
        // 不 Wait、不触碰 Result（除非已成功完成），避免把日志动作变成阻塞点。
        var table = NewTable(task.GetType(), "成员", "值");
        var status = task.Status;
        Add("Id", task.Id);
        Add("Status", status);
        Add("IsCompleted", task.IsCompleted);
        Add("IsCanceled", task.IsCanceled);
        Add("IsFaulted", task.IsFaulted);
        Add("CreationOptions", task.CreationOptions);
        Add("AsyncState", task.AsyncState);
        if (status == TaskStatus.Faulted)
            table.AddRow(new Text("Exception"), SpecialCell(() => task.Exception, depth, ancestors));

        for (var type = task.GetType(); type is not null; type = type.BaseType)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Task<>)) continue;
            if (status == TaskStatus.RanToCompletion)
                table.AddRow(new Text("Result"), SpecialCell(() => type.GetProperty("Result")!.GetValue(task), depth, ancestors));
            else
                table.AddRow(new Text("Result"), Hint(status switch
                {
                    TaskStatus.Faulted => "<Faulted>",
                    TaskStatus.Canceled => "<Canceled>",
                    _ => "<Not Completed>"
                }));
            break;
        }
        return table;

        void Add(string name, object? item)
            => table.AddRow(new Text(name), RenderValue(item, depth + 1, ancestors));
    }

    IRenderable RenderLazy(object lazy, Type lazyType, int depth, HashSet<object> ancestors)
    {
        // 不强制求值未创建的 Lazy。
        var table = NewTable(lazy.GetType(), "成员", "值");
        var created = (bool)lazyType.GetProperty("IsValueCreated")!.GetValue(lazy)!;
        table.AddRow(new Text("IsValueCreated"), RenderValue(created, depth + 1, ancestors));
        table.AddRow(new Text("Value"), created
            ? SpecialCell(() => lazyType.GetProperty("Value")!.GetValue(lazy), depth, ancestors)
            : Hint("<Not Evaluated>"));
        return table;
    }

    IRenderable RenderMemberSignature(MemberInfo member)
    {
        // 只输出签名而非反射全部元数据属性，避免沿元数据递归回 Type。
        var text = member switch
        {
            PropertyInfo property => $"{TypeName(property.PropertyType)} {property.Name}{IndexParameters(property)} {{ {(property.CanRead ? "get; " : "")}{(property.CanWrite ? "set; " : "")} }}",
            ConstructorInfo constructor => $"ctor {TypeName(constructor.DeclaringType!)}({Parameters(constructor.GetParameters())})",
            MethodInfo method => $"{TypeName(method.ReturnType)} {method.Name}{GenericParameters(method)}({Parameters(method.GetParameters())})",
            FieldInfo field => $"{TypeName(field.FieldType)} {field.Name};",
            EventInfo @event => $"event {TypeName(@event.EventHandlerType ?? typeof(void))} {@event.Name};",
            _ => $"{member.MemberType} {member.DeclaringType?.FullName}.{member.Name}"
        };
        return SpecialText(text);

        string Parameters(ParameterInfo[] parameters) => string.Join(", ",
            parameters.Take(DumpOptions.Truncation.MaxCollectionCount)
                .Select(p => $"{TypeName(p.ParameterType)} {p.Name}"))
            + (parameters.Length > DumpOptions.Truncation.MaxCollectionCount ? ", …" : "");

        string IndexParameters(PropertyInfo property)
            => property.GetIndexParameters() is { Length: > 0 } parameters ? "[" + Parameters(parameters) + "]" : "";

        string GenericParameters(MethodInfo method)
            => method.IsGenericMethod
                ? "<" + string.Join(", ", method.GetGenericArguments()
                    .Take(DumpOptions.Truncation.MaxCollectionCount).Select(TypeName)) + ">"
                : "";
    }

    /// <summary>按文本渲染器输出类型、缩进成员和集合项，不生成 ANSI 控制字符。</summary>
    public string DumpText(object? value) => FormatText(value, 0,
        new HashSet<object>(ReferenceEqualityComparer.Instance));

    string FormatText(object? value, int depth, HashSet<object> ancestors)
    {
        if (value is null) return "null";
        try
        {
            foreach (var renderer in _renderers)
                if (renderer(value) is { } custom) return ExportText(custom);
            if (value is IRenderable renderable) return ExportText(renderable);
            if (value is DataTable or DataSet or Task or MemberInfo || IsLazy(value.GetType()))
                return ExportText(RenderValue(value, depth, ancestors));
            if (IsScalar(value)) return ExportText(Scalar(value));
            if (depth >= DumpOptions.MaxDepth) return "<达到最大深度>";

            var track = !value.GetType().IsValueType;
            if (track && !ancestors.Add(value)) return $"<循环引用: {TypeName(value.GetType())}>";
            try
            {
                if (value is System.Runtime.CompilerServices.ITuple tuple)
                    return FormatItems(Enumerable.Range(0, tuple.Length).Select(i => tuple[i]));
                if (value is IEnumerable items) return FormatItems(items);

                var builder = new StringBuilder(DumpOptions.TypeNames.ShowTypeNames
                    ? "{" + TypeName(value.GetType()) + "}" : "{}");
                var indent = new string(' ', (depth + 1) * 2);
                foreach (var member in GetMembers(value))
                {
                    string text;
                    try { text = FormatText(member.Read(), depth + 1, ancestors); }
                    catch (Exception exception) when (exception is not OutOfMemoryException)
                    { text = ExportText(Error("成员读取失败", exception)); }
                    builder.AppendLine().Append(indent).Append(member.Member.Name);
                    if (DumpOptions.Table.ShowMemberTypes)
                        builder.Append(" (").Append(TypeName(member.Type)).Append(')');
                    builder.Append(": ").Append(text);
                }
                return builder.ToString();
            }
            finally { if (track) ancestors.Remove(value); }

            string FormatItems(IEnumerable items)
            {
                var selection = ReadCollection(items);
                var parts = new List<string>();
                var previous = -1;
                var gap = false;
                foreach (var item in selection.Items)
                {
                    if (item.Index > previous + 1) { parts.Add("<省略项>"); gap = true; }
                    parts.Add(FormatText(item.Value, depth + 1, ancestors));
                    previous = item.Index;
                }
                if (selection.Truncated && !gap) parts.Add("<其余项已截断>");
                if (selection.Note is { } note) parts.Add(note);
                if (selection.Error is { } error) parts.Add(ExportText(Error("集合读取失败", error)));
                if (parts.Count == 0) return "[]";
                var indent = new string(' ', (depth + 1) * 2);
                return "[" + Environment.NewLine + indent
                    + string.Join("," + Environment.NewLine + indent, parts) + Environment.NewLine
                    + new string(' ', depth * 2) + "]";
            }
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ExportText(Error("文本打印失败", exception));
        }
    }

    static bool IsLazy(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Lazy<>)) return true;
        return false;
    }

    /// <summary>
    /// 把任意可渲染对象导出为无 ANSI 的纯文本。
    /// 宽度 <c>0</c> 表示使用一个足够大的宽度而不折行——写日志文件时必须这样，
    /// 否则表格和面板会被日志文件的读取宽度切断，看不出完整结构。
    /// </summary>
    public static string ExportText(IRenderable renderable, int width = 0)
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            Ansi = AnsiSupport.No,
            Interactive = InteractionSupport.No
        });
        console.Profile.Width = width > 0 ? width : NoWrapWidth;
        console.Write(renderable);
        return writer.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>渲染「不折行」用的宽度。取较大值以容纳表格，但避免 Spectre 的 int.MaxValue 溢出。</summary>
    const int NoWrapWidth = 20_000;
}

/// <summary>对象打印的便捷入口。</summary>
public static class SpectreObjectPrintingExtensions
{
    /// <summary>直接打印对象到控制台。</summary>
    public static void Print(this object? value, SpectreObjectPrinter? printer = null, IAnsiConsole? console = null)
    {
        var output = console ?? AnsiConsole.Console;
        output.Write((printer ?? new SpectreObjectPrinter()).Render(value, output.Profile.Width));
        output.WriteLine();
    }
}
