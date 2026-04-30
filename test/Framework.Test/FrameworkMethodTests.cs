namespace Framework.Test;

public class FrameworkMethodCallTests
{
    [Fact]
    public void MethodCall_1_Extensions_Forget_1Params()
    {
        System.Threading.Extensions.Forget(default!);
    }

    [Fact]
    public void MethodCall_2_Extensions_CompleteOnCurrentThread_1Params()
    {
        System.Threading.Extensions.CompleteOnCurrentThread(default!);
    }

    [Fact]
    public void MethodCall_3_UrlParser_Parse_1Params()
    {
        Framework.Proxy.UrlParser.Parse(default!);
    }

    [Fact]
    public void MethodCall_4_Extensions_IsNumeric_1Params()
    {
        System.Extensions.IsNumeric(default!);
    }

    [Fact]
    public void MethodCall_5_Extensions_TryConvertTo_3Params()
    {
        System.Extensions.TryConvertTo(default!, default!, out _);
    }

    [Fact]
    public void MethodCall_6_Extensions_ConvertTo_2Params()
    {
        System.Extensions.ConvertTo(default!, default!);
    }

    [Fact]
    public void MethodCall_7_Extensions_ChangeType_2Params()
    {
        System.Extensions.ChangeType(default!, default!);
    }

    [Fact]
    public void MethodCall_8_Extensions_IsPrimitive_1Params()
    {
        System.Extensions.IsPrimitive(default!);
    }

    [Fact]
    public void MethodCall_9_Extensions_IsSimpleType_1Params()
    {
        System.Extensions.IsSimpleType(default!);
    }

    [Fact]
    public void MethodCall_10_Extensions_IsSimpleArrayType_1Params()
    {
        System.Extensions.IsSimpleArrayType(default!);
    }

    [Fact]
    public void MethodCall_11_Extensions_IsSimpleListType_1Params()
    {
        System.Extensions.IsSimpleListType(default!);
    }

    [Fact]
    public void MethodCall_12_Extensions_IsDefaultValue_1Params()
    {
        System.Extensions.IsDefaultValue(default!);
    }

    [Fact]
    public void MethodCall_13_Extensions_ToJsonString_2Params()
    {
        System.Extensions.ToJsonString(default!, default!);
    }

    [Fact]
    public void MethodCall_14_Extensions_Resize_2Params()
    {
        System.Extensions.Resize(default!, default!);
    }

    [Fact]
    public void MethodCall_15_EnumExtension_GetDescription_1Params()
    {
        Framework.System.EnumExtension.GetDescription(default!);
    }

    [Fact]
    public void MethodCall_16_EnumExtension_ToNameWithDescription_1Params()
    {
        Framework.System.EnumExtension.ToNameWithDescription(default!);
    }

    [Fact]
    public void MethodCall_17_EnumExtension_ToInt64_1Params()
    {
        Framework.System.EnumExtension.ToInt64(default!);
    }

    [Fact]
    public void MethodCall_18_Extensions_IsNull_1Params()
    {
        System.Extensions.IsNull(default!);
    }

    [Fact]
    public void MethodCall_19_Extensions_NotNull_1Params()
    {
        System.Extensions.NotNull(default!);
    }

    [Fact]
    public void MethodCall_20_Extensions_EqualsIgnoreCase_2Params()
    {
        System.Extensions.EqualsIgnoreCase(default!, default!);
    }

    [Fact]
    public void MethodCall_21_Extensions_FirstCharToLower_1Params()
    {
        System.Extensions.FirstCharToLower(default!);
    }

    [Fact]
    public void MethodCall_22_Extensions_FirstCharToUpper_1Params()
    {
        System.Extensions.FirstCharToUpper(default!);
    }

    [Fact]
    public void MethodCall_23_Extensions_ToBase64_1Params()
    {
        System.Extensions.ToBase64(default!);
    }

    [Fact]
    public void MethodCall_24_Extensions_ToBase64_2Params()
    {
        System.Extensions.ToBase64(default!, default!);
    }

    [Fact]
    public void MethodCall_25_Extensions_ToPath_1Params()
    {
        System.Extensions.ToPath(default!);
    }

    [Fact]
    public void MethodCall_26_Extensions_Limit_2Params()
    {
        System.Extensions.Limit(default!, default!);
    }

    [Fact]
    public void MethodCall_27_Extensions_LimitWithEllipsis_2Params()
    {
        System.Extensions.LimitWithEllipsis(default!, default!);
    }

    [Fact]
    public void MethodCall_28_Extension_ToMilliseconds_1Params()
    {
        System.Extension.ToMilliseconds(default!);
    }

    [Fact]
    public void MethodCall_29_Extension_ToSeconds_1Params()
    {
        System.Extension.ToSeconds(default!);
    }

    [Fact]
    public void MethodCall_30_Extension_ToMicroseconds_1Params()
    {
        System.Extension.ToMicroseconds(default!);
    }

    [Fact]
    public void MethodCall_31_Extension_In_4Params()
    {
        System.Extension.In(default!, default!, default!, default!);
    }

    [Fact]
    public void MethodCall_32_Extension_GetDayMinDate_1Params()
    {
        System.Extension.GetDayMinDate(default!);
    }

    [Fact]
    public void MethodCall_33_Extension_GetDayMaxDate_1Params()
    {
        System.Extension.GetDayMaxDate(default!);
    }

    [Fact]
    public void MethodCall_34_Extensions_GetExceptionFootprints_1Params()
    {
        System.Extensions.GetExceptionFootprints(default!);
    }

    [Fact]
    public void MethodCall_35_Helper_VerticalMergeImageByte_1Params()
    {
        Framework.Helpers.Helper.VerticalMergeImageByte(default!);
    }

    [Fact]
    public void MethodCall_36_Helper_VerticalMergeImageStream_1Params()
    {
        Framework.Helpers.Helper.VerticalMergeImageStream(default!);
    }

    [Fact]
    public void MethodCall_37_Helper_DESEncrypt_2Params()
    {
        Framework.Helper.DESEncrypt(default!, default!);
    }

    [Fact]
    public void MethodCall_38_Helper_DESDecrypt_2Params()
    {
        Framework.Helper.DESDecrypt(default!, default!);
    }

    [Fact]
    public void MethodCall_39_Helper_DESEncrypt4Hex_3Params()
    {
        Framework.Helper.DESEncrypt4Hex(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_40_Helper_DESDecrypt4Hex_2Params()
    {
        Framework.Helper.DESDecrypt4Hex(default!, default!);
    }

    [Fact]
    public void MethodCall_41_Helper_ToDateTime_1Params()
    {
        Framework.Helper.ToDateTime(default!);
    }

    [Fact]
    public void MethodCall_42_Helper_GetWeekAmount_1Params()
    {
        Framework.Helper.GetWeekAmount(default!);
    }

    [Fact]
    public void MethodCall_43_Helper_WeekOfYear_1Params()
    {
        Framework.Helper.WeekOfYear(default!);
    }

    [Fact]
    public void MethodCall_44_Helper_WeekOfYear_2Params()
    {
        Framework.Helper.WeekOfYear(default!, default!);
    }

    [Fact]
    public void MethodCall_45_Helper_GetWeekTime_2Params()
    {
        Framework.Helper.GetWeekTime(default!, default!);
    }

    [Fact]
    public void MethodCall_46_Helper_GetCurrentWeek_1Params()
    {
        Framework.Helper.GetCurrentWeek(default!);
    }

    [Fact]
    public void MethodCall_47_Helper_GetCurrentMonth_1Params()
    {
        Framework.Helper.GetCurrentMonth(default!);
    }

    [Fact]
    public void MethodCall_48_Helper_GetCurrentYear_1Params()
    {
        Framework.Helper.GetCurrentYear(default!);
    }

    [Fact]
    public void MethodCall_49_Helper_GetCurrentQuarter_1Params()
    {
        Framework.Helper.GetCurrentQuarter(default!);
    }

    [Fact]
    public void MethodCall_50_Helper_GetCurrentRange_2Params()
    {
        Framework.Helper.GetCurrentRange(default!, default!);
    }

    [Fact]
    public void MethodCall_51_Helper_GetDateTime_2Params()
    {
        Framework.Helper.GetDateTime(default!, default!);
    }

    [Fact]
    public void MethodCall_52_Helper_GetTotalSeconds_1Params()
    {
        Framework.Helper.GetTotalSeconds(default!);
    }

    [Fact]
    public void MethodCall_53_Helper_GetTotalMilliseconds_1Params()
    {
        Framework.Helper.GetTotalMilliseconds(default!);
    }

    [Fact]
    public void MethodCall_54_Helper_GetTotalMicroseconds_1Params()
    {
        Framework.Helper.GetTotalMicroseconds(default!);
    }

    [Fact]
    public void MethodCall_55_Helper_GetTotalNanoseconds_1Params()
    {
        Framework.Helper.GetTotalNanoseconds(default!);
    }

    [Fact]
    public void MethodCall_56_Helper_GetTotalMinutes_1Params()
    {
        Framework.Helper.GetTotalMinutes(default!);
    }

    [Fact]
    public void MethodCall_57_Helper_GetTotalHours_1Params()
    {
        Framework.Helper.GetTotalHours(default!);
    }

    [Fact]
    public void MethodCall_58_Helper_GetTotalDays_1Params()
    {
        Framework.Helper.GetTotalDays(default!);
    }

    [Fact]
    public void MethodCall_59_Helper_GetDaysOfYear_1Params()
    {
        Framework.Helper.GetDaysOfYear(default!);
    }

    [Fact]
    public void MethodCall_60_Helper_GetDaysOfMonth_1Params()
    {
        Framework.Helper.GetDaysOfMonth(default!);
    }

    [Fact]
    public void MethodCall_61_Helper_GetWeekNameOfDay_1Params()
    {
        Framework.Helper.GetWeekNameOfDay(default!);
    }

    [Fact]
    public void MethodCall_62_Helper_In_4Params()
    {
        Framework.Helper.In(default!, default!, default!, default!);
    }

    [Fact]
    public void MethodCall_63_Helper_GetMonthLastDate_1Params()
    {
        Framework.Helper.GetMonthLastDate(default!);
    }

    [Fact]
    public void MethodCall_64_Helper_GetTimeDelay_2Params()
    {
        Framework.Helper.GetTimeDelay(default!, default!);
    }

    [Fact]
    public void MethodCall_65_Helper_DateDiff_2Params()
    {
        Framework.Helper.DateDiff(default!, default!);
    }

    [Fact]
    public void MethodCall_66_Helper_GetDiffTime_2Params()
    {
        Framework.Helper.GetDiffTime(default!, default!);
    }

    [Fact]
    public void MethodCall_67_Helper_MD5Encrypt16_2Params()
    {
        Framework.Helper.MD5Encrypt16(default!, default!);
    }

    [Fact]
    public void MethodCall_68_Helper_MD5Encrypt32_2Params()
    {
        Framework.Helper.MD5Encrypt32(default!, default!);
    }

    [Fact]
    public void MethodCall_69_Helper_MD5Encrypt64_1Params()
    {
        Framework.Helper.MD5Encrypt64(default!);
    }

    [Fact]
    public void MethodCall_70_Helper_GetHash_1Params()
    {
        Framework.Helper.GetHash(default!);
    }

    [Fact]
    public void MethodCall_71_Helper_ZipStream_2Params()
    {
        Framework.Helper.ZipStream(default!, default!);
    }

    [Fact]
    public void MethodCall_72_Helper_ZipStream_3Params()
    {
        Framework.Helper.ZipStream(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_73_Helper_Zip_3Params()
    {
        Framework.Helper.Zip(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_74_Helper_Zip_5Params()
    {
        Framework.Helper.Zip(default!, default!, default!, default!, default!);
    }

    [Fact]
    public void MethodCall_75_Helper_Decompress_2Params()
    {
        Framework.Helper.Decompress(default!, default!);
    }

    [Fact]
    public void MethodCall_76_Helper_IsImplementInterface_2Params()
    {
        Framework.Helper.IsImplementInterface(default!, default!);
    }

    [Fact]
    public void MethodCall_77_Helper_SHA1Decrypt_3Params()
    {
        Framework.Helper.SHA1Decrypt(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_78_Helper_IsAnonymousType_1Params()
    {
        Framework.Helper.IsAnonymousType(default!);
    }

    [Fact]
    public void MethodCall_79_Helper_ToBase32String_1Params()
    {
        Framework.Helper.ToBase32String(default!);
    }

    [Fact]
    public void MethodCall_80_Helper_FromBase32String_1Params()
    {
        Framework.Helper.FromBase32String(default!);
    }

    [Fact]
    public void MethodCall_81_Helper_CreateTempFile_2Params()
    {
        Framework.Helper.CreateTempFile(default!, default!);
    }

    [Fact]
    public void MethodCall_82_Helper_ClearTempFiles_1Params()
    {
        Framework.Helper.ClearTempFiles(default!);
    }

    [Fact]
    public void MethodCall_83_Helper_JsonDeserialize_2Params()
    {
        Framework.Helper.JsonDeserialize(default!, default!);
    }

    [Fact]
    public void MethodCall_84_Helper_GenerateRandom_1Params()
    {
        Framework.Helper.GenerateRandom(default!);
    }

    [Fact]
    public void MethodCall_85_Helper_GenerateRandomNumber_1Params()
    {
        Framework.Helper.GenerateRandomNumber(default!);
    }

    [Fact]
    public void MethodCall_86_Helper_Format_2Params()
    {
        Framework.Helper.Format(default!, default!);
    }

    [Fact]
    public void MethodCall_87_Helper_SM4Encrypt_5Params()
    {
        Framework.Helper.SM4Encrypt(default!, default!, default!, default!, default!);
    }

    [Fact]
    public void MethodCall_88_Helper_SM4Decrypt_5Params()
    {
        Framework.Helper.SM4Decrypt(default!, default!, default!, default!, default!);
    }

    [Fact]
    public void MethodCall_89_Helper_ComputeSha256Hash_1Params()
    {
        Framework.Helper.ComputeSha256Hash(default!);
    }

    [Fact]
    public void MethodCall_90_Helper_ComputeSha384Hash_1Params()
    {
        Framework.Helper.ComputeSha384Hash(default!);
    }

    [Fact]
    public void MethodCall_91_Helper_ComputeSha512Hash_1Params()
    {
        Framework.Helper.ComputeSha512Hash(default!);
    }

    [Fact]
    public void MethodCall_92_Helper_ToHex_2Params()
    {
        Framework.Helper.ToHex(default!, default!);
    }

    [Fact]
    public void MethodCall_93_Helper_HexToBytes_1Params()
    {
        Framework.Helper.HexToBytes(default!);
    }

    [Fact]
    public void MethodCall_94_Helper_ToBase64_1Params()
    {
        Framework.Helper.ToBase64(default!);
    }

    [Fact]
    public void MethodCall_95_Helper_StringToUnicode_1Params()
    {
        Framework.Helper.StringToUnicode(default!);
    }

    [Fact]
    public void MethodCall_96_Helper_UnicodeToString_1Params()
    {
        Framework.Helper.UnicodeToString(default!);
    }

    [Fact]
    public void MethodCall_97_Helper_IsNumber_1Params()
    {
        Framework.Helper.IsNumber(default!);
    }

    [Fact]
    public void MethodCall_98_Helper_IsNumberic_1Params()
    {
        Framework.Helper.IsNumberic(default!);
    }

    [Fact]
    public void MethodCall_99_Helper_IsTel_1Params()
    {
        Framework.Helper.IsTel(default!);
    }

    [Fact]
    public void MethodCall_100_Helper_IsPhone_1Params()
    {
        Framework.Helper.IsPhone(default!);
    }

    [Fact]
    public void MethodCall_101_Helper_IsFax_1Params()
    {
        Framework.Helper.IsFax(default!);
    }

    [Fact]
    public void MethodCall_102_Helper_IsMobile_1Params()
    {
        Framework.Helper.IsMobile(default!);
    }

    [Fact]
    public void MethodCall_103_Helper_IsIDCard_1Params()
    {
        Framework.Helper.IsIDCard(default!);
    }

    [Fact]
    public void MethodCall_104_Helper_IsIDCard18_1Params()
    {
        Framework.Helper.IsIDCard18(default!);
    }

    [Fact]
    public void MethodCall_105_Helper_IsIDCard15_1Params()
    {
        Framework.Helper.IsIDCard15(default!);
    }

    [Fact]
    public void MethodCall_106_Helper_IsEmail_1Params()
    {
        Framework.Helper.IsEmail(default!);
    }

    [Fact]
    public void MethodCall_107_Helper_IsOnlyChinese_1Params()
    {
        Framework.Helper.IsOnlyChinese(default!);
    }

    [Fact]
    public void MethodCall_108_Helper_IsBadString_1Params()
    {
        Framework.Helper.IsBadString(default!);
    }

    [Fact]
    public void MethodCall_109_Helper_IsNzx_1Params()
    {
        Framework.Helper.IsNzx(default!);
    }

    [Fact]
    public void MethodCall_110_Helper_IsSzzmChinese_1Params()
    {
        Framework.Helper.IsSzzmChinese(default!);
    }

    [Fact]
    public void MethodCall_111_Helper_IsSzzm_1Params()
    {
        Framework.Helper.IsSzzm(default!);
    }

    [Fact]
    public void MethodCall_112_Helper_IsPostCode_1Params()
    {
        Framework.Helper.IsPostCode(default!);
    }

    [Fact]
    public void MethodCall_113_Helper_CheckLength_2Params()
    {
        Framework.Helper.CheckLength(default!, default!);
    }

    [Fact]
    public void MethodCall_114_Helper_IsDateTime_1Params()
    {
        Framework.Helper.IsDateTime(default!);
    }

    [Fact]
    public void MethodCall_115_Helper_GetAssemblyList_1Params()
    {
        Framework.Helper.GetAssemblyList(default!);
    }

    [Fact]
    public void MethodCall_116_Extensions_IsAsync_1Params()
    {
        System.Reflection.Extensions.IsAsync(default!);
    }

    [Fact]
    public void MethodCall_117_Extensions_InvokeMethod_3Params()
    {
        System.Reflection.Extensions.InvokeMethod(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_118_Extensions_GetField_2Params()
    {
        System.Reflection.Extensions.GetField(default!, default!);
    }

    [Fact]
    public void MethodCall_119_Extensions_GetFields_1Params()
    {
        System.Reflection.Extensions.GetFields(default!);
    }

    [Fact]
    public void MethodCall_120_Extensions_GetProperty_2Params()
    {
        System.Reflection.Extensions.GetProperty(default!, default!);
    }

    [Fact]
    public void MethodCall_121_Extensions_GetProperties_1Params()
    {
        System.Reflection.Extensions.GetProperties(default!);
    }

    [Fact]
    public void MethodCall_122_Extensions_GetDescription_1Params()
    {
        System.Reflection.Extensions.GetDescription(default!);
    }

    [Fact]
    public void MethodCall_123_Extensions_GetDescription_2Params()
    {
        System.Reflection.Extensions.GetDescription(default!, default!);
    }

    [Fact]
    public void MethodCall_124_Extensions_GetImageResource_2Params()
    {
        System.Reflection.Extensions.GetImageResource(default!, default!);
    }

    [Fact]
    public void MethodCall_125_Extensions_GetManifestString_3Params()
    {
        System.Reflection.Extensions.GetManifestString(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_126_Extensions_IsImplementsOf_2Params()
    {
        System.Reflection.Extensions.IsImplementsOf(default!, default!);
    }

    [Fact]
    public void MethodCall_127_Extensions_GetInstance_1Params()
    {
        System.Reflection.Extensions.GetInstance(default!);
    }

    [Fact]
    public void MethodCall_128_Extensions_CreateInstanceOf_4Params()
    {
        System.Reflection.Extensions.CreateInstanceOf(default!, default!, default!, default!);
    }

    [Fact]
    public void MethodCall_129_Extensions_StandardDeviation_1Params()
    {
        System.Collections.Generic.Extensions.StandardDeviation(default!);
    }

    [Fact]
    public void MethodCall_130_Extensions_CopyToFile_3Params()
    {
        System.IO.Extensions.CopyToFile(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_131_Extensions_CopyToFileAsync_3Params()
    {
        System.IO.Extensions.CopyToFileAsync(default!, default!, default!);
    }

    [Fact]
    public void MethodCall_132_Extensions_SaveFile_2Params()
    {
        System.IO.Extensions.SaveFile(default!, default!);
    }

    [Fact]
    public void MethodCall_133_Extensions_GetFileMD5_1Params()
    {
        System.IO.Extensions.GetFileMD5(default!);
    }

    [Fact]
    public void MethodCall_134_Extensions_GetFileSha1_1Params()
    {
        System.IO.Extensions.GetFileSha1(default!);
    }

    [Fact]
    public void MethodCall_135_Extensions_GetFileSha256_1Params()
    {
        System.IO.Extensions.GetFileSha256(default!);
    }

    [Fact]
    public void MethodCall_136_Extensions_GetFileSha512_1Params()
    {
        System.IO.Extensions.GetFileSha512(default!);
    }

    [Fact]
    public void MethodCall_137_Extensions_WriteCR_1Params()
    {
        System.Buffers.Extensions.WriteCR(default!);
    }

    [Fact]
    public void MethodCall_138_Extensions_WriteLF_1Params()
    {
        System.Buffers.Extensions.WriteLF(default!);
    }

    [Fact]
    public void MethodCall_139_Extensions_WriteCRLF_1Params()
    {
        System.Buffers.Extensions.WriteCRLF(default!);
    }

    [Fact]
    public void MethodCall_140_Extensions_Write_2Params()
    {
        System.Buffers.Extensions.Write(default!, default!);
    }

    [Fact]
    public void MethodCall_141_Extensions_WriteBigEndian_2Params()
    {
        System.Buffers.Extensions.WriteBigEndian(default!, default!);
    }

    [Fact]
    public void MethodCall_142_Extensions_WriteLittleEndian_2Params()
    {
        System.Buffers.Extensions.WriteLittleEndian(default!, default!);
    }

}