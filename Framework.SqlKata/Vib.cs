using System.ComponentModel.DataAnnotations.Schema;

namespace Framework.SqlKata;

[Table("d_{}_vib")]
public partial class Vib
{
    [Column("id")]
    public int Id { get; set; }//取值：0—15，主键
    [Column("saveTime")]
    public DateTime SaveTime { get; set; }//保存时间
    [Column("saveTime_Com")]
    public long SaveTimeCom { get; set; }//毫秒数
    [Column("dataId")]
    public int? DataId { get; set; }//数据类型0~15位；月时间，周时间......
    [Column("jc")]
    public int? Jc { get; set; }//报警状态；
    [Column("speed")]
    public float? Speed { get; set; } //转速
    [Column("vib_rms")]
    public float? VibRms { get; set; } //振动有效值
    [Column("vib_p")]
    public float? VibP { get; set; } //振动单峰值
    [Column("vib_pp")]
    public float? VibPp { get; set; } //振动峰峰值
    [Column("vib_vsx1_scale")]
    public float? VibVsx1Scale { get; set; } //VIB_Vsx1与全频谱幅值平方和开方的比值
    [Column("vib_vsx2_scale")]
    public float? VibVsx2Scale { get; set; } //VIB_Vsx2与全频谱幅值平方和开方的比值
    [Column("vib_vsx3_scale")]
    public float? VibVsx3Scale { get; set; }
    [Column("vib_vsx4_scale")]
    public float? VibVsx4Scale { get; set; }
    [Column("vib_vsx5_scale")]
    public float? VibVsx5Scale { get; set; }
    [Column("vib_vsx6_scale")]
    public float? VibVsx6Scale { get; set; }
    [Column("vib_vsx7_scale")]
    public float? VibVsx7Scale { get; set; }
    [Column("vib_vsx8_scale")]
    public float? VibVsx8Scale { get; set; }
    [Column("sv")]
    public float Sv { get; set; }
    [Column("temperature")]
    public float? Temperature { get; set; }  //温度
    [Column("temperature_rise")]
    public float? TemperatureRise { get; set; }  //温升
    [Column("vib_wave_len")]
    public int VibWaveLen { get; set; } = 0;//压缩后的振动波形数据的字节长度
    [Column("vib_wave")]
    public byte[]? VibWave { get; set; }


}