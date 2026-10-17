using Microsoft.Extensions.Logging;
using Spectre.Console.Rendering;
using System.Collections;
using System.Globalization;
using System.Text;

namespace Framework.ZLogging;

/// <summary>
/// 一条日志的不可变快照。
/// 在调用线程上完成结构化参数的物化，避免把 <c>TState</c> 的引用泄漏到后台线程
/// ——这是自建 Logger 相对借用第三方框架最容易出错的地方：
/// <see cref="ILogger.Log{TState}"/> 一返回，调用方就可以改写状态对象。
/// </summary>
sealed class SpectreLogEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required LogLevel LogLevel { get; init; }
    public required string Category { get; init; }
    public required EventId EventId { get; init; }

    /// <summary>原始消息模板（来自 <c>{OriginalFormat}</c>）；为 <c>null</c> 表示无模板，走回退消息。</summary>
    public string? MessageTemplate { get; init; }

    /// <summary>与模板占位符对应的实参，已在调用线程拷贝为数组。</summary>
    public object?[] Arguments { get; init; } = [];

    /// <summary>模板缺失时由调用方 formatter 生成的回退消息。</summary>
    public string? FormattedMessage { get; init; }

    public Exception? Exception { get; init; }

    /// <summary>作用域文本，已在调用线程展开完毕。</summary>
    public string? Scopes { get; init; }

    /// <summary>把 <c>TState</c> 快照为不可变条目。整个过程必须同步完成。</summary>
    public static SpectreLogEntry Capture<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter,
        DateTimeOffset timestamp,
        string category,
        string? scopes)
    {
        string? template = null;
        object?[] arguments;

        switch (state)
        {
            // 快路径：Microsoft 的结构化日志状态都实现了 IReadOnlyList<KVP>。
            case IReadOnlyList<KeyValuePair<string, object?>> readOnly:
                template = Extract(readOnly, out arguments!);
                break;

            case IEnumerable<KeyValuePair<string, object?>> enumerable:
                var buffer = enumerable as IReadOnlyList<KeyValuePair<string, object?>>
                             ?? enumerable.ToArray();
                template = Extract(buffer, out arguments!);
                break;

            default:
                return new SpectreLogEntry
                {
                    Timestamp = timestamp,
                    LogLevel = logLevel,
                    Category = category,
                    EventId = eventId,
                    FormattedMessage = formatter(state, exception),
                    Exception = exception,
                    Scopes = scopes
                };
        }

        // 仍持有原始 state，可以直接复用官方 formatter 的语义，无需手工拼字符串。
        return new SpectreLogEntry
        {
            Timestamp = timestamp,
            LogLevel = logLevel,
            Category = category,
            EventId = eventId,
            MessageTemplate = template,
            Arguments = arguments,
            FormattedMessage = template is null ? formatter(state, exception) : null,
            Exception = exception,
            Scopes = scopes
        };
    }

    /// <summary>拆出 <c>{OriginalFormat}</c> 模板，其余键值按顺序成为实参。</summary>
    static string? Extract(IReadOnlyList<KeyValuePair<string, object?>> properties, out object?[] arguments)
    {
        string? template = null;
        List<object?>? values = null;
        foreach (var property in properties)
        {
            if (property.Key == "{OriginalFormat}")
            {
                template = property.Value as string;
                continue;
            }

            (values ??= []).Add(property.Value);
        }

        arguments = values?.ToArray() ?? [];
        return template;
    }
}

/// <summary>
/// 把 <see cref="SpectreLogEntry"/> 渲染成消息正文与结构化对象列表。
/// 控制台与文件两条链路共用此解析，保证同一日志在两端的切分完全一致。
/// </summary>
sealed class SpectreMessageFormatter
{
    readonly SpectreLogOptions options;

    public SpectreMessageFormatter(SpectreLogOptions options) => this.options = options;

    /// <summary>
    /// 解析出一条日志：返回消息正文，以及命中的专用格式参数。
    /// 命中专用格式（如 <c>{Data:panel}</c>）的实参不拼进正文，单独交给渲染器。
    /// </summary>
    public (string? Message, IReadOnlyList<SpectreLogArgument> Objects) Format(SpectreLogEntry entry)
    {
        if (entry.MessageTemplate is null) return (entry.FormattedMessage, []);

        var template = entry.MessageTemplate;
        var arguments = entry.Arguments;
        var objects = new List<SpectreLogArgument>();
        var composite = new StringBuilder(template.Length);
        var values = new List<object?>();
        var argumentIndex = 0;

        for (var position = 0; position < template.Length;)
        {
            var character = template[position];
            if (character is '{' or '}' && position + 1 < template.Length && template[position + 1] == character)
            {
                composite.Append(character, 2);
                position += 2;
                continue;
            }

            if (character != '{')
            {
                composite.Append(character);
                position++;
                continue;
            }

            var closing = template.IndexOf('}', position + 1);
            if (closing < 0 || argumentIndex >= arguments.Length)
            {
                // 模板与实参数量对不上（模板是动态拼接的）：原样输出，不做无谓猜测。
                return (template, objects);
            }

            var token = template[(position + 1)..closing];
            var colon = token.IndexOf(':');
            var comma = token.IndexOf(',');
            var formatStart = colon < 0 ? comma : comma < 0 ? -1 : Math.Min(colon, comma);

            var renderer = default(Func<object?, SpectreObjectPrinter, IRenderable>);
            var specialFormat = colon >= 0 && options.TryGetFormat(token[(colon + 1)..], out renderer);

            if (specialFormat) objects.Add(new(argumentIndex, arguments[argumentIndex], renderer!));

            // 专用格式在正文里留占位提示，避免读者以为参数丢了。
            values.Add(specialFormat
                ? $"<{argumentIndex}: {arguments[argumentIndex]?.GetType().Name ?? "null"}>"
                : arguments[argumentIndex]);

            composite.Append('{').Append(argumentIndex);

            if (formatStart >= 0)
            {
                // 消费专用格式指令（冒号之后的部分），保留其前面的对齐 / 宽度语法。
                var suffixEnd = specialFormat ? colon : token.Length;
                composite.Append(token.AsSpan(formatStart, suffixEnd - formatStart));
            }
            composite.Append('}');
            argumentIndex++;
            position = closing + 1;
        }

        // 没有专用格式时同样走重建后的模板。
        // 不能直接 string.Format(template)：命名占位符（{Index}）只有 LogValuesFormatter 才认识，
        // string.Format 只认位置索引 {0}，直接传会把命名占位符当成格式串而抛异常。
        try
        {
            return (string.Format(CultureInfo.InvariantCulture, composite.ToString(),
                values.Select(FormatPlainArgument).ToArray()), objects);
        }
        catch (FormatException)
        {
            return (template, objects);
        }
    }

    /// <summary>把未命中专用格式的实参转成与 ILogger 原生一致的文本形态。</summary>
    static object FormatPlainArgument(object? value)
    {
        if (value is null) return "(null)";
        if (value is string || value is not IEnumerable enumerable) return value;

        var text = new StringBuilder();
        var first = true;
        foreach (var item in enumerable)
        {
            if (!first) text.Append(", ");
            text.Append(item ?? "(null)");
            first = false;
        }
        return text.ToString();
    }
}

/// <summary>一条命中的专用格式参数。</summary>
/// <param name="Index">在实参数组中的下标。</param>
/// <param name="Value">原始值。</param>
/// <param name="Renderer">专用格式对应的渲染函数。</param>
readonly record struct SpectreLogArgument(
    int Index,
    object? Value,
    Func<object?, SpectreObjectPrinter, IRenderable> Renderer);
