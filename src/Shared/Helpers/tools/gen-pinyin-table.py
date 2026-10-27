r"""重新生成 Helper.Pinyin.cs 中内嵌的汉字拼音表。

用法：
    python gen-pinyin-table.py [pinyin.txt] [Helper.Pinyin.cs]

    pinyin.txt       pinyin-data 的读音表，默认 .\pinyin.txt
                     下载地址：https://raw.githubusercontent.com/mozillazg/pinyin-data/master/pinyin.txt
    Helper.Pinyin.cs 目标文件，默认脚本所在目录的上一级同名文件

脚本只改写 `SyllableWordList` 与 `PackedCodes` 两个常量，其余代码保持原样。

编码规则（与 Helper.Pinyin.cs 头注释一致）：
    每字一个 ushort = 音节序号(1 基，0 表示无数据) << 3 | 声调(1-4，0 为轻声)
    数组按小端序打包 -> 裸 deflate 流 -> Base64
"""
import base64
import os
import re
import sys
import unicodedata
import zlib

FIRST, LAST = 0x4E00, 0x9FFF
TONE_MARKS = {'\u0304': 1, '\u0301': 2, '\u030c': 3, '\u0300': 4}


def strip_tone(pinyin):
    """'zhōng' -> ('zhong', 1)；轻声返回 ('de', 0)"""
    out, tone = [], 0
    for ch in unicodedata.normalize('NFD', pinyin):
        if ch in TONE_MARKS:
            tone = TONE_MARKS[ch]
        elif ch == '\u0308':            # 分音符：u + ̈ => ü
            if out and out[-1] == 'u':
                out[-1] = '\u00fc'
            else:
                out.append('\u00fc')
        else:
            out.append(ch)
    return ''.join(out), tone


def load_readings(path):
    """解析 pinyin.txt，返回 {码点: 第一个读音}（第一个读音即主读音）。"""
    readings = {}
    with open(path, encoding='utf-8') as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith('#'):
                continue
            m = re.match(r'^U\+([0-9A-Fa-f]+):\s*([^#]+)', line)
            if m:
                values = [p.strip() for p in m.group(2).split(',') if p.strip()]
                readings[int(m.group(1), 16)] = values[0]
    return readings


def build_codes(readings):
    """返回 (音节列表, 每字 ushort 编码数组)。"""
    records = {}
    for cp, pinyin in readings.items():
        if FIRST <= cp <= LAST:
            syllable, tone = strip_tone(pinyin)
            if not re.fullmatch('[a-z\u00fc]+', syllable):
                raise ValueError('无法解析的读音 %s: %r' % (hex(cp), pinyin))
            records[cp] = (syllable, tone)

    syllables = sorted({s for s, _ in records.values()})
    if len(syllables) > 511:
        raise ValueError('音节数 %d 超出 9 bit 容量' % len(syllables))

    index = {s: i + 1 for i, s in enumerate(syllables)}
    codes = [0] * (LAST - FIRST + 1)
    for cp, (syllable, tone) in records.items():
        codes[cp - FIRST] = (index[syllable] << 3) | tone
    return syllables, codes


def format_syllables(syllables, indent='        ', width=76):
    lines, current = [], ''
    for s in syllables:
        if current and len(current) + len(s) + 1 > width:
            lines.append(current)
            current = s
        else:
            current = s if not current else current + ' ' + s
    lines.append(current)
    body = [l + ' ' for l in lines[:-1]] + [lines[-1]]
    return '\n'.join('%s"%s"%s' % (indent, l, ' +' if i < len(body) - 1 else ';')
                     for i, l in enumerate(body))


def format_packed(data, indent='        ', width=120):
    payload = base64.b64encode(data).decode('ascii')
    lines = [payload[i:i + width] for i in range(0, len(payload), width)]
    return '\n'.join('%s"%s"%s' % (indent, l, ' +' if i < len(lines) - 1 else ';')
                     for i, l in enumerate(lines))


def main(argv):
    here = os.path.dirname(os.path.abspath(__file__))
    data_path = argv[1] if len(argv) > 1 else os.path.join(os.getcwd(), 'pinyin.txt')
    target = argv[2] if len(argv) > 2 else os.path.join(here, '..', 'Helper.Pinyin.cs')

    syllables, codes = build_codes(load_readings(data_path))
    raw = b''.join(int(v).to_bytes(2, 'little') for v in codes)
    compressor = zlib.compressobj(9, zlib.DEFLATED, -15)   # DeflateStream 要裸 deflate 流
    packed = compressor.compress(raw) + compressor.flush()

    source = open(target, encoding='utf-8').read()
    for name, text in (('SyllableWordList', format_syllables(syllables)),
                       ('PackedCodes', format_packed(packed))):
        pattern = r'(private const string %s =\n).*?;' % name
        source, count = re.subn(pattern, lambda m: m.group(1) + text, source, flags=re.S)
        if count != 1:
            raise SystemExit('未在 %s 中找到唯一的 %s 常量' % (target, name))

    open(target, 'w', encoding='utf-8', newline='\n').write(source)
    print('音节 %d 个，覆盖 %d 字，压缩后 %d 字节' % (len(syllables), sum(1 for c in codes if c), len(packed)))


if __name__ == '__main__':
    main(sys.argv)
