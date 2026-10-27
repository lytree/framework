// ─────────────────────────────────────────────────────────────────────────────
// 汉字拼音表（Pinyin Table）
//
// 覆盖范围：CJK 统一汉字基本区 U+4E00–U+9FFF，共 20992 字，其中 20924 字有读音
// 数据来源：https://github.com/mozillazg/pinyin-data （由 Unicode Unihan kMandarin 整理，
//           随 Unicode 数据以 Unicode License 发布；此处仅内嵌「主读音」的派生结果）
// 编码方式：每字一个 ushort —— 高 9 位为音节序号（1 基，0 表示无数据），
//           低 3 位为声调（1–4，0 表示轻声）；数组按小端序打包后 Deflate + Base64 内嵌，
//           首次访问时才解压，因此不引入任何外部依赖与资源文件。
//
// 局限：只保留最常用读音，多音字一律按主读音输出（「重」→ zhòng、「长」→ zhǎng）；
//       需要按上下文纠音时，请在调用侧覆盖结果。
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

internal static partial class Helper
{
    /// <summary>
    /// 判断字符是否为 CJK 统一汉字（U+4E00–U+9FFF）。
    /// </summary>
    /// <param name="c">待判断的字符</param>
    /// <returns>是汉字返回 <see langword="true"/></returns>
    public static bool IsChinese(char c) => c >= PinyinTable.First && c <= PinyinTable.Last;

    /// <summary>
    /// 判断字符串是否全部由汉字组成（<see langword="null"/> 与空串返回 <see langword="false"/>）。
    /// </summary>
    /// <param name="text">待判断的字符串</param>
    /// <returns>全部为汉字返回 <see langword="true"/></returns>
    public static bool IsChinese(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        foreach (var c in text)
        {
            if (!IsChinese(c)) return false;
        }
        return true;
    }

    /// <summary>
    /// 尝试获取单个汉字的拼音。
    /// </summary>
    /// <param name="c">汉字</param>
    /// <param name="pinyin">拼音；字符无读音时为空字符串</param>
    /// <param name="format">拼音格式，默认带声调符号</param>
    /// <returns>取到读音返回 <see langword="true"/>；非汉字或无读音返回 <see langword="false"/></returns>
    /// <example>
    /// <code>
    /// Helper.TryGetPinyin('中', out var pinyin);                          // pinyin = "zhōng"
    /// Helper.TryGetPinyin('国', out pinyin, PinyinFormat.ToneNumber);     // pinyin = "guo2"
    /// </code>
    /// </example>
    public static bool TryGetPinyin(char c, out string pinyin, PinyinFormat format = PinyinFormat.ToneMark)
    {
        var code = PinyinTable.GetCode(c);
        if (code == 0)
        {
            pinyin = string.Empty;
            return false;
        }

        pinyin = FormatPinyin(PinyinTable.GetSyllable(code >> 3), code & 0b111, format);
        return true;
    }

    /// <summary>
    /// 获取单个字符的拼音；非汉字（或无读音的冷僻字）原样返回。
    /// </summary>
    /// <param name="c">待转换字符</param>
    /// <param name="format">拼音格式，默认带声调符号</param>
    /// <returns>拼音或原字符</returns>
    public static string GetPinyin(char c, PinyinFormat format = PinyinFormat.ToneMark)
        => TryGetPinyin(c, out var pinyin, format) ? pinyin : c.ToString();

    /// <summary>
    /// 将字符串逐字转换为拼音。
    /// </summary>
    /// <param name="text">待转换字符串，可为 <see langword="null"/></param>
    /// <param name="format">拼音格式，默认带声调符号</param>
    /// <param name="separator">转换单元之间的连接符，默认空格；传空串得到紧凑结果。<br/>
    /// 每个汉字是一个单元，连续的标点 / 字母 / 数字整体算一个单元。</param>
    /// <param name="keepNonChinese">是否保留非汉字字符（数字、英文、标点等），默认保留</param>
    /// <returns>转换结果；<paramref name="text"/> 为空时返回空字符串</returns>
    /// <example>
    /// <code>
    /// Helper.GetPinyin("中国2024");                      // "zhōng guó 2024"
    /// Helper.GetPinyin("中国", PinyinFormat.Plain, "");  // "zhongguo"（可用作排序 / 检索键）
    /// </code>
    /// </example>
    public static string GetPinyin(
        string? text,
        PinyinFormat format = PinyinFormat.ToneMark,
        string separator = " ",
        bool keepNonChinese = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var builder = new StringBuilder(text.Length * 5);
        var needSeparator = false;
        var inPlainRun = false;
        foreach (var c in text)
        {
            if (TryGetPinyin(c, out var pinyin, format))
            {
                if (needSeparator) builder.Append(separator);
                builder.Append(pinyin);
                needSeparator = true;
                inPlainRun = false;
            }
            else if (keepNonChinese)
            {
                // 连续的非汉字合并为一个单元，只在起始处插入连接符
                if (needSeparator && !inPlainRun) builder.Append(separator);
                builder.Append(c);
                needSeparator = true;
                inPlainRun = true;
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 获取字符串中每个汉字的拼音首字母，非汉字字符原样保留。
    /// </summary>
    /// <param name="text">待转换字符串，可为 <see langword="null"/></param>
    /// <param name="upperCase">是否输出大写字母，默认 <see langword="true"/></param>
    /// <param name="keepNonChinese">是否保留非汉字字符，默认保留</param>
    /// <returns>首字母序列；<paramref name="text"/> 为空时返回空字符串</returns>
    /// <example>
    /// <code>
    /// Helper.GetPinyinInitials("中华人民共和国"); // "ZHRMGHG"
    /// Helper.GetPinyinInitials("ABC中国");        // "ABCZG"
    /// </code>
    /// </example>
    public static string GetPinyinInitials(string? text, bool upperCase = true, bool keepNonChinese = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (TryGetPinyin(c, out var pinyin, PinyinFormat.Plain))
            {
                var letter = pinyin[0];
                builder.Append(upperCase ? char.ToUpperInvariant(letter) : letter);
            }
            else if (keepNonChinese)
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    /// <summary>
    /// 获取单个汉字拼音的首字母（大写）。
    /// </summary>
    /// <param name="c">待转换字符</param>
    /// <returns>'A'–'Z' 之一；非汉字返回 <c>'\0'</c></returns>
    public static char GetPinyinFirstLetter(char c)
        => TryGetPinyin(c, out var pinyin, PinyinFormat.Plain) ? char.ToUpperInvariant(pinyin[0]) : '\0';

    /// <summary>
    /// 按指定格式输出拼音。
    /// </summary>
    private static string FormatPinyin(string syllable, int tone, PinyinFormat format) => format switch
    {
        PinyinFormat.ToneMark => ApplyTone(syllable, tone),
        PinyinFormat.ToneNumber => tone is >= 1 and <= 4 ? syllable + (char)('0' + tone) : syllable,
        _ => syllable,
    };

    /// <summary>
    /// 声调符号表，按「基本元音 × 四声」排列：a e i o u ü。
    /// </summary>
    private const string TonedVowels = "āáǎàēéěèīíǐìōóǒòūúǔùǖǘǚǜ";

    /// <summary>
    /// 组合用声调符号（阴平、阳平、上声、去声），供成音节鼻音使用。
    /// </summary>
    private const string CombiningToneMarks = "\u0304\u0301\u030C\u0300";

    /// <summary>
    /// 给无声调音节标注声调符号（a / e 优先，其次 ou 的 o，最后落在最后一个元音上）。
    /// </summary>
    private static string ApplyTone(string syllable, int tone)
    {
        if (tone is < 1 or > 4) return syllable;

        var position = TonePosition(syllable);
        if (position < 0)
        {
            // 成音节鼻音没有元音（呣 ḿ、嗯 ń），直接把组合声调符号跟在字母后面
            return syllable + CombiningToneMarks[tone - 1];
        }

        var vowel = "aeiouü".IndexOf(syllable[position]);
        if (vowel < 0) return syllable;

        var mark = TonedVowels[vowel * 4 + tone - 1];
        return string.Concat(syllable.AsSpan(0, position), mark.ToString(), syllable.AsSpan(position + 1));
    }

    /// <summary>
    /// 定位声调符号应标注的元音位置。
    /// </summary>
    private static int TonePosition(string syllable)
    {
        var index = syllable.IndexOf('a');
        if (index >= 0) return index;

        index = syllable.IndexOf('e');
        if (index >= 0) return index;

        index = syllable.IndexOf("ou", StringComparison.Ordinal);
        if (index >= 0) return index;

        for (var i = syllable.Length - 1; i >= 0; i--)
        {
            if ("aeiouü".IndexOf(syllable[i]) >= 0) return i;
        }
        return -1;
    }
}

/// <summary>
/// 汉字拼音格式。
/// </summary>
internal enum PinyinFormat
{
    /// <summary>带声调符号，如 zhōng。</summary>
    ToneMark,

    /// <summary>音节后加声调数字，如 zhong1（轻声不加数字）。</summary>
    ToneNumber,

    /// <summary>不带声调，如 zhong。</summary>
    Plain,
}

/// <summary>
/// 汉字 → 拼音只读表（U+4E00–U+9FFF），供 <see cref="Helper"/> 的拼音 API 使用。
/// </summary>
internal static class PinyinTable
{
    /// <summary>表覆盖的首个字符。</summary>
    internal const char First = '\u4E00';

    /// <summary>表覆盖的最后一个字符。</summary>
    internal const char Last = '\u9FFF';

    /// <summary>表覆盖的字符总数。</summary>
    internal const int Count = Last - First + 1;

    /// <summary>
    /// 取出一个字符的编码：高 9 位为音节序号（1 基），低 3 位为声调；0 表示无数据。
    /// </summary>
    /// <param name="c">待查询字符</param>
    /// <returns>编码值；超出覆盖范围或无读音时为 0</returns>
    internal static ushort GetCode(char c)
        => c >= First && c <= Last ? Data.Codes[c - First] : (ushort)0;

    /// <summary>
    /// 按音节序号（1 基）取无声调音节，如 1 → "a"。
    /// </summary>
    /// <param name="index">音节序号，从 1 开始</param>
    /// <returns>无声调的音节字符串</returns>
    internal static string GetSyllable(int index) => Data.Syllables[index - 1];

    /// <summary>
    /// 音节表，空格分隔；序号从 1 开始，与编码一一对应。
    /// </summary>
    private const string SyllableWordList =
        "a ai an ang ao ba bai ban bang bao bei ben beng bi bian biao bie bin bing bo " +
        "bu ca cai can cang cao ce cen ceng cha chai chan chang chao che chen cheng " +
        "chi chong chou chu chua chuai chuan chuang chui chun chuo ci cong cou cu " +
        "cuan cui cun cuo da dai dan dang dao de den deng di dian diao die ding diu " +
        "dong dou du duan dui dun duo e ei en eng er fa fan fang fei fen feng fiao fo " +
        "fou fu ga gai gan gang gao ge gei gen geng gong gou gu gua guai guan guang " +
        "gui gun guo ha hai han hang hao he hei hen heng hong hou hu hua huai huan " +
        "huang hui hun huo ji jia jian jiang jiao jie jin jing jiong jiu ju juan jue " +
        "jun ka kai kan kang kao ke ken keng kong kou ku kua kuai kuan kuang kui kun " +
        "kuo la lai lan lang lao le lei leng li lia lian liang liao lie lin ling liu " +
        "long lou lu luan lun luo lü lüe m ma mai man mang mao me mei men meng mi " +
        "mian miao mie min ming miu mo mou mu n na nai nan nang nao ne nei nen neng " +
        "ni nian niang niao nie nin ning niu nong nou nu nuan nun nuo nü nüe o ou pa " +
        "pai pan pang pao pei pen peng pi pian piao pie pin ping po pou pu qi qia " +
        "qian qiang qiao qie qin qing qiong qiu qu quan que qun ran rang rao re ren " +
        "reng ri rong rou ru rua ruan rui run ruo sa sai san sang sao se sen seng sha " +
        "shai shan shang shao she shen sheng shi shou shu shua shuai shuan shuang " +
        "shui shun shuo si song sou su suan sui sun suo ta tai tan tang tao te teng " +
        "ti tian tiao tie ting tong tou tu tuan tui tun tunwa tuo wa wai wan wang wei " +
        "wen weng wo wu xi xia xian xiang xiao xie xin xing xiong xiu xu xuan xue xun " +
        "ya yan yang yao ye yi yin ying yo yong you yu yuan yue yun za zai zan zang " +
        "zao ze zei zen zeng zha zhai zhan zhang zhao zhe zhen zheng zhi zhong zhou " +
        "zhu zhua zhuai zhuan zhuang zhui zhun zhuo zi zong zou zu zuan zui zun zuo";

    /// <summary>
    /// 编码数组（小端序 ushort）经 Deflate + Base64 后的数据。
    /// </summary>
    private const string PackedCodes =
        "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" +
        "7uQmKReENEhh/28=";

    /// <summary>
    /// 延迟初始化：只有真正用到拼音时才解压数据。
    /// </summary>
    private static class Data
    {
        internal static readonly ushort[] Codes = DecodeCodes();

        internal static readonly string[] Syllables = SyllableWordList.Split(' ');

        private static ushort[] DecodeCodes()
        {
            var buffer = new byte[Count * 2];
            using (var compressed = new MemoryStream(Convert.FromBase64String(PackedCodes), writable: false))
            using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress))
            {
                var offset = 0;
                while (offset < buffer.Length)
                {
                    var read = deflate.Read(buffer, offset, buffer.Length - offset);
                    if (read <= 0) break;
                    offset += read;
                }

                if (offset != buffer.Length)
                {
                    throw new InvalidOperationException("内置拼音表数据已损坏。");
                }
            }

            var codes = new ushort[Count];
            for (var i = 0; i < codes.Length; i++)
            {
                codes[i] = (ushort)(buffer[i << 1] | (buffer[(i << 1) + 1] << 8));
            }
            return codes;
        }
    }
}
