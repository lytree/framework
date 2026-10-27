using TUnit.Core;

namespace Framework.Tests.Helpers;

public class HelperPinyinTests
{
    // ── IsChinese ────────────────────────────────────────────────────────────

    [Test]
    [Arguments('中', true)]
    [Arguments('龘', true)]
    [Arguments('a', false)]
    [Arguments('，', false)]
    [Arguments('1', false)]
    [Arguments('㐀', false)] // CJK 扩展 A，不在表覆盖范围
    public async Task IsChinese_Char(char c, bool expected)
        => await Assert.That(Helper.IsChinese(c)).IsEqualTo(expected);

    [Test]
    public async Task IsChinese_String_ReturnsTrueOnlyWhenAllHanzi()
    {
        await Assert.That(Helper.IsChinese("中国")).IsTrue();
        await Assert.That(Helper.IsChinese("中国a")).IsFalse();
        await Assert.That(Helper.IsChinese("")).IsFalse();
        await Assert.That(Helper.IsChinese(null)).IsFalse();
    }

    // ── 单字拼音 ──────────────────────────────────────────────────────────────

    [Test]
    public async Task TryGetPinyin_Hanzi_ReturnsTrue()
    {
        var ok = Helper.TryGetPinyin('中', out var pinyin);

        await Assert.That(ok).IsTrue();
        await Assert.That(pinyin).IsEqualTo("zhōng");
    }

    [Test]
    public async Task TryGetPinyin_NonHanzi_ReturnsFalseWithEmptyResult()
    {
        var ok = Helper.TryGetPinyin('A', out var pinyin);

        await Assert.That(ok).IsFalse();
        await Assert.That(pinyin).IsEqualTo(string.Empty);
    }

    [Test]
    [Arguments('中', "zhōng", "zhong1", "zhong")]
    [Arguments('国', "guó", "guo2", "guo")]
    [Arguments('好', "hǎo", "hao3", "hao")]
    [Arguments('会', "huì", "hui4", "hui")]  // ui 标在 i 上
    [Arguments('六', "liù", "liu4", "liu")]  // iu 标在 u 上
    [Arguments('学', "xué", "xue2", "xue")]
    public async Task GetPinyin_Char_AppliesToneByFormat(char c, string toneMark, string toneNumber, string plain)
    {
        await Assert.That(Helper.GetPinyin(c)).IsEqualTo(toneMark);
        await Assert.That(Helper.GetPinyin(c, PinyinFormat.ToneNumber)).IsEqualTo(toneNumber);
        await Assert.That(Helper.GetPinyin(c, PinyinFormat.Plain)).IsEqualTo(plain);
    }

    [Test]
    [Arguments('女', "nǚ", "nü3", "nü")]
    [Arguments('绿', "lǜ", "lü4", "lü")]
    [Arguments('略', "lüè", "lüe4", "lüe")]   // 声调标在 e 上
    [Arguments('虐', "nüè", "nüe4", "nüe")]
    public async Task GetPinyin_Char_KeepsUmlaut(char c, string toneMark, string toneNumber, string plain)
    {
        await Assert.That(Helper.GetPinyin(c)).IsEqualTo(toneMark);
        await Assert.That(Helper.GetPinyin(c, PinyinFormat.ToneNumber)).IsEqualTo(toneNumber);
        await Assert.That(Helper.GetPinyin(c, PinyinFormat.Plain)).IsEqualTo(plain);
    }

    [Test]
    public async Task GetPinyin_NeutralTone_HasNoMarkAndNoDigit()
    {
        await Assert.That(Helper.GetPinyin('的')).IsEqualTo("de");
        await Assert.That(Helper.GetPinyin('的', PinyinFormat.ToneNumber)).IsEqualTo("de");
    }

    [Test]
    public async Task GetPinyin_SyllabicNasal_KeepsToneMark()
    {
        // 成音节鼻音没有元音，声调用组合符号标在鼻音上：嗯 ń、呣 ḿ
        await Assert.That(Helper.GetPinyin('嗯')).IsEqualTo("n\u0301");
        await Assert.That(Helper.GetPinyin('呣')).IsEqualTo("m\u0301");
    }

    [Test]
    public async Task GetPinyin_NonHanzi_ReturnsCharItself()
        => await Assert.That(Helper.GetPinyin('A')).IsEqualTo("A");

    // ── 字符串拼音 ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPinyin_String_JoinsBySpace()
        => await Assert.That(Helper.GetPinyin("拼音")).IsEqualTo("pīn yīn");

    [Test]
    public async Task GetPinyin_String_KeepsNonChineseAsSingleUnit()
        => await Assert.That(Helper.GetPinyin("中国2024")).IsEqualTo("zhōng guó 2024");

    [Test]
    public async Task GetPinyin_String_CompactIsUsableAsSortKey()
    {
        var left = Helper.GetPinyin("李四", PinyinFormat.Plain, string.Empty);
        var right = Helper.GetPinyin("张三", PinyinFormat.Plain, string.Empty);

        await Assert.That(left).IsEqualTo("lisi");
        await Assert.That(right).IsEqualTo("zhangsan");
        await Assert.That(string.CompareOrdinal(left, right) < 0).IsTrue();
    }

    [Test]
    public async Task GetPinyin_String_CanDropNonChinese()
        => await Assert.That(Helper.GetPinyin("中a国", PinyinFormat.Plain, " ", keepNonChinese: false)).IsEqualTo("zhong guo");

    [Test]
    [Arguments("")]
    [Arguments(null)]
    public async Task GetPinyin_String_EmptyInputReturnsEmpty(string? text)
        => await Assert.That(Helper.GetPinyin(text)).IsEqualTo(string.Empty);

    // ── 首字母 ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetPinyinInitials_ReturnsUpperLettersByDefault()
        => await Assert.That(Helper.GetPinyinInitials("中华人民共和国")).IsEqualTo("ZHRMGHG");

    [Test]
    public async Task GetPinyinInitials_KeepsNonChinese()
        => await Assert.That(Helper.GetPinyinInitials("ABC中国")).IsEqualTo("ABCZG");

    [Test]
    public async Task GetPinyinInitials_SupportsLowerCase()
        => await Assert.That(Helper.GetPinyinInitials("中文", upperCase: false)).IsEqualTo("zw");

    [Test]
    public async Task GetPinyinFirstLetter_ReturnsUpperLetter()
    {
        await Assert.That(Helper.GetPinyinFirstLetter('中')).IsEqualTo('Z');
        await Assert.That(Helper.GetPinyinFirstLetter('女')).IsEqualTo('N');
        await Assert.That(Helper.GetPinyinFirstLetter('1')).IsEqualTo('\0');
    }

    // ── 表完整性 ──────────────────────────────────────────────────────────────

    [Test]
    public async Task PinyinTable_CoversWholeBasicBlock()
    {
        var total = 0;
        var readable = 0;
        for (var c = (int)PinyinTable.First; c <= PinyinTable.Last; c++)
        {
            total++;
            if (!Helper.TryGetPinyin((char)c, out var pinyin, PinyinFormat.Plain)) continue;
            if (pinyin.Length > 0) readable++;
        }

        await Assert.That(total).IsEqualTo(PinyinTable.Count);
        await Assert.That(readable).IsGreaterThan(20000);
    }

    [Test]
    public async Task PinyinTable_OutOfRangeChar_HasNoCode()
    {
        await Assert.That(PinyinTable.GetCode('A')).IsEqualTo((ushort)0);
        await Assert.That(PinyinTable.GetCode('㐀')).IsEqualTo((ushort)0);
        await Assert.That(PinyinTable.GetCode('中')).IsNotEqualTo((ushort)0);
    }
}
