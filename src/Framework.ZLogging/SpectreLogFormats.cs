using Spectre.Console;
using Spectre.Console.Rendering;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Framework.ZLogging;

/// <summary>
/// 内置的消息模板专用格式（自定义指令）。
/// 只负责产出渲染对象，不直接写控制台或文件——两条输出链路各自决定去向。
/// </summary>
internal static class SpectreLogFormats
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void RegisterDefaults(SpectreLogOptions options)
    {
        // 对象转储家族：obj / table / dump / array / dictionary / tuple … 语义等价，
        // 分开命名是为了让消息模板自解释。
        foreach (var format in new[]
        {
            "obj", "table", "dump", "array", "dictionary", "tuple", "datatable",
            "dataset", "enum", "guid", "time", "type", "task", "lazy"
        })
        {
            options.RegisterFormat(format, static (value, printer) => printer.Render(value, printer.Width));
        }

        options.RegisterFormat("vertical", static (value, printer) => printer.Render(value, SpectreTableLayout.Vertical));
        options.RegisterFormat("horizontal", static (value, printer) => printer.Render(value, SpectreTableLayout.Horizontal));
        options.RegisterFormat("dumptext", static (value, printer) => new Text(printer.DumpText(value)));
        options.RegisterFormat("json", static (value, _) => RenderJson(value));
        options.RegisterFormat("text", static (value, _) => new Text(AsText(value)));
        options.RegisterFormat("markup", static (value, _) => new Markup(AsText(value)));
        options.RegisterFormat("panel", static (value, printer) => new Panel(printer.Render(value, printer.Width)));
        options.RegisterFormat("tree", static (value, printer) => RenderTree(value, printer));
        options.RegisterFormat("hex", static (value, printer) => RenderHex(value, printer));
        options.RegisterFormat("base64", static (value, printer) => RenderBase64(value, printer));
        options.RegisterFormat("exception", static (value, _) => value switch
        {
            null => new Text("null"),
            Exception exception => exception.GetRenderable(),
            _ => throw new ArgumentException(":exception 需要 Exception。")
        });
    }

    static string AsText(object? value) => value switch
    {
        null => "null",
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? "",
        _ => value.ToString() ?? ""
    };

    static JsonDocument ReadJson(object? value) => value switch
    {
        string json => JsonDocument.Parse(json),
        JsonDocument document => JsonDocument.Parse(document.RootElement.GetRawText()),
        JsonElement element => JsonDocument.Parse(element.GetRawText()),
        JsonNode node => JsonDocument.Parse(node.ToJsonString(JsonOptions)),
        _ => JsonSerializer.SerializeToDocument(value, value?.GetType() ?? typeof(object), JsonOptions)
    };

    static IRenderable RenderJson(object? value)
    {
        using var document = ReadJson(value);
        // 不引入 Spectre.Console.Json：缩进后的 JSON 本身就是可读文本。
        // 必须用 Text 而非 Markup —— JSON 里的 " 和 [ 会被标记解析器当成语法。
        return new Text(SyntaxHighlight(document.RootElement));
    }

    static string SyntaxHighlight(JsonElement element)
    {
        var builder = new StringBuilder();
        Write(element, builder, 0);
        return builder.ToString().TrimEnd('\r', '\n');

        void Write(JsonElement node, StringBuilder sb, int indent)
        {
            var pad = new string(' ', indent);
            switch (node.ValueKind)
            {
                case JsonValueKind.Object:
                    sb.Append("{\n");
                    var first = true;
                    foreach (var property in node.EnumerateObject())
                    {
                        if (!first) sb.Append(",\n");
                        first = false;
                        // 键名必须带引号，否则输出不是合法 JSON。
                        sb.Append(pad).Append("  \"").Append(property.Name).Append("\": ");
                        Write(property.Value, sb, indent + 2);
                    }
                    sb.Append('\n').Append(pad).Append('}');
                    break;

                case JsonValueKind.Array:
                    sb.Append("[\n");
                    var firstItem = true;
                    foreach (var item in node.EnumerateArray())
                    {
                        if (!firstItem) sb.Append(",\n");
                        firstItem = false;
                        sb.Append(pad).Append("  ");
                        Write(item, sb, indent + 2);
                    }
                    sb.Append('\n').Append(pad).Append(']');
                    break;

                case JsonValueKind.String:
                    sb.Append('"').Append(node.GetString() ?? "").Append('"');
                    break;

                case JsonValueKind.Number:
                    sb.Append(node.GetRawText());
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    sb.Append(node.GetRawText());
                    break;

                default:
                    sb.Append(node.ValueKind == JsonValueKind.Null ? "null" : node.GetRawText());
                    break;
            }
        }
    }

    static IRenderable RenderTree(object? value, SpectreObjectPrinter printer)
    {
        using var document = ReadJson(value);
        var tree = new Tree(new Text("root"));
        Append(document.RootElement, null, 0);
        return tree;

        void Append(JsonElement element, TreeNode? parent, int depth)
        {
            TreeNode Add(IRenderable label) => parent is null ? tree.AddNode(label) : parent.AddNode(label);
            if (element.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                Add(new Text(Scalar(element)));
                return;
            }
            if (depth >= printer.DumpOptions.MaxDepth)
            {
                Add(new Text("<达到最大深度>", new Style(Color.Grey)));
                return;
            }

            var count = 0;
            var limit = printer.DumpOptions.Truncation.MaxCollectionCount;
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (count++ >= limit) { Add(Truncated(limit)); break; }
                    AddValue(property.Name, property.Value);
                }
                if (count == 0) Add(new Text("{}"));
            }
            else
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (count >= limit) { Add(Truncated(limit)); break; }
                    AddValue($"[{count++}]", item);
                }
                if (count == 0) Add(new Text("[]"));
            }

            void AddValue(string name, JsonElement child)
            {
                if (child.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    Append(child, Add(new Text(name, new Style(Color.Aqua))), depth + 1);
                else
                    Add(new Text($"{name}: {Scalar(child)}"));
            }
        }
    }

    static string Scalar(JsonElement value)
        => value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.GetRawText();

    static ReadOnlyMemory<byte> Bytes(object value, bool allowText = false) => value switch
    {
        byte[] bytes => bytes,
        Memory<byte> memory => memory,
        ReadOnlyMemory<byte> memory => memory,
        ArraySegment<byte> segment => segment.AsMemory(),
        string text when allowText => Encoding.UTF8.GetBytes(text),
        _ => throw new ArgumentException(allowText
            ? "需要字节数组、Memory<byte>、ReadOnlyMemory<byte>、ArraySegment<byte> 或字符串。"
            : "需要字节数组、Memory<byte>、ReadOnlyMemory<byte> 或 ArraySegment<byte>。")
    };

    static IRenderable RenderHex(object? value, SpectreObjectPrinter printer)
    {
        if (value is null) return new Text("null");
        var bytes = Bytes(value);
        var count = Math.Min(bytes.Length, printer.DumpOptions.Truncation.MaxCollectionCount);
        var table = new Table().AddColumn("偏移").AddColumn("十六进制").AddColumn("ASCII");
        for (var offset = 0; offset < count; offset += 16)
        {
            var row = bytes.Span.Slice(offset, Math.Min(16, count - offset));
            var ascii = new StringBuilder(row.Length);
            var hex = new StringBuilder(row.Length * 3);
            foreach (var b in row)
            {
                if (hex.Length > 0) hex.Append(' ');
                hex.Append(b.ToString("X2", CultureInfo.InvariantCulture));
                ascii.Append(b is >= 32 and <= 126 ? (char)b : '.');
            }
            table.AddRow(new Text(offset.ToString("X8", CultureInfo.InvariantCulture)),
                new Text(hex.ToString()), new Text(ascii.ToString()));
        }
        if (count < bytes.Length)
            table.AddRow(new Text("…"), Truncated(count), new Text($"共 {bytes.Length} 字节"));
        if (bytes.Length == 0)
            table.AddRow(new Text("0"), new Text("<空>"), new Text(""));
        return table;
    }

    static IRenderable RenderBase64(object? value, SpectreObjectPrinter printer)
    {
        if (value is null) return new Text("null");
        var bytes = Bytes(value, allowText: true);
        var count = Math.Min(bytes.Length, printer.DumpOptions.Truncation.MaxCollectionCount);
        var text = Convert.ToBase64String(bytes.Span[..count]);
        return new Text(count < bytes.Length
            ? $"{text}{Environment.NewLine}<仅编码前 {count}/{bytes.Length} 字节>"
            : text);
    }

    static Text Truncated(int count) => new($"<仅显示前 {count} 项>", new Style(Color.Grey));
}
