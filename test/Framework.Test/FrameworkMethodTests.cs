using System.Reflection;

namespace Framework.Test;

public class FrameworkMethodTests
{
    [Fact]
    public void Method_1_Extensions_IsNumeric_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsNumeric" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_2_Extensions_TryConvertTo_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "TryConvertTo" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_3_Extensions_ConvertTo_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ConvertTo" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_4_Extensions_ChangeType_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ChangeType" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_5_Extensions_Resize_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Resize" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_6_Extensions_GetExceptionFootprints_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetExceptionFootprints" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_7_Extensions_IsPrimitive_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsPrimitive" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_8_Extensions_IsSimpleType_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsSimpleType" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_9_Extensions_IsSimpleArrayType_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsSimpleArrayType" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_10_Extensions_IsSimpleListType_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsSimpleListType" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_11_Extensions_IsDefaultValue_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsDefaultValue" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_12_Extensions_ToJsonString_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToJsonString" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_13_ArrayExtensions_ForEach_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.ArrayExtensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ForEach" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_14_ArrayTraverse_Step_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.ArrayTraverse");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Step" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_15_Extension_ToMilliseconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToMilliseconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_16_Extension_ToSeconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToSeconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_17_Extension_ToMicroseconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToMicroseconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_18_Extension_In_4Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "In" && m.GetParameters().Length == 4);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_19_Extension_GetDayMinDate_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDayMinDate" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_20_Extension_GetDayMaxDate_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDayMaxDate" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_21_Extensions_IsNull_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsNull" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_22_Extensions_NotNull_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "NotNull" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_23_Extensions_EqualsIgnoreCase_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "EqualsIgnoreCase" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_24_Extensions_FirstCharToLower_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "FirstCharToLower" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_25_Extensions_FirstCharToUpper_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "FirstCharToUpper" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_26_Extensions_ToBase64_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToBase64" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_27_Extensions_ToBase64_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToBase64" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_28_Extensions_ToPath_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToPath" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_29_Extensions_Limit_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Limit" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_30_Extensions_LimitWithEllipsis_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "LimitWithEllipsis" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_31_EnumExtension_GetDescription_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.System.EnumExtension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDescription" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_32_EnumExtension_ToNameWithDescription_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.System.EnumExtension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToNameWithDescription" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_33_EnumExtension_ToInt64_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.System.EnumExtension");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToInt64" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_34_UrlParser_Parse_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Proxy.UrlParser");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Parse" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_35_Extensions_InvokeMethod_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "InvokeMethod" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_36_Extensions_GetField_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetField" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_37_Extensions_GetFields_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetFields" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_38_Extensions_GetProperty_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetProperty" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_39_Extensions_GetProperties_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetProperties" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_40_Extensions_GetDescription_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDescription" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_41_Extensions_GetDescription_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDescription" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_42_Extensions_GetImageResource_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetImageResource" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_43_Extensions_GetManifestString_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetManifestString" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_44_Extensions_IsImplementsOf_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsImplementsOf" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_45_Extensions_GetInstance_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetInstance" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_46_InstanceCreationFactory_CreateInstanceOf_4Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.InstanceCreationFactory");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CreateInstanceOf" && m.GetParameters().Length == 4);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_47_InstanceCreationFactory_CacheInstanceCreationMethodIfRequired_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.InstanceCreationFactory");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CacheInstanceCreationMethodIfRequired" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_48_Extensions_IsAsync_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsAsync" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_49_Extensions_GetReturnType_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Reflection.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetReturnType" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_50_Helper_ComputeHash_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ComputeHash" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_51_Helper_ComputeSha256Hash_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ComputeSha256Hash" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_52_Helper_ComputeSha384Hash_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ComputeSha384Hash" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_53_Helper_ComputeSha512Hash_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ComputeSha512Hash" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_54_Helper_GenerateRandom_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GenerateRandom" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_55_Helper_GenerateRandomNumber_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GenerateRandomNumber" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_56_Helper_Format_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Format" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_57_Helper_JsonDeserialize_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "JsonDeserialize" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_58_Helper_GetAssemblyList_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetAssemblyList" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_59_Helper_MD5Encrypt16_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "MD5Encrypt16" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_60_Helper_MD5Encrypt32_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "MD5Encrypt32" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_61_Helper_MD5Encrypt64_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "MD5Encrypt64" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_62_Helper_GetHash_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetHash" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_63_Helper_SM4Encrypt_5Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "SM4Encrypt" && m.GetParameters().Length == 5);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_64_Helper_SM4Decrypt_5Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "SM4Decrypt" && m.GetParameters().Length == 5);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_65_Helper_ToHex_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToHex" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_66_Helper_HexToBytes_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "HexToBytes" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_67_Helper_ToBase64_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToBase64" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_68_Helper_StringToUnicode_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "StringToUnicode" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_69_Helper_UnicodeToString_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "UnicodeToString" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_70_Helper_IsAnonymousType_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsAnonymousType" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_71_Helper_VerticalMergeImageByte_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helpers.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "VerticalMergeImageByte" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_72_Helper_VerticalMergeImageStream_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helpers.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "VerticalMergeImageStream" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_73_Helper_SHA1Decrypt_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "SHA1Decrypt" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_74_Helper_ToDateTime_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToDateTime" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_75_Helper_GetWeekAmount_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetWeekAmount" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_76_Helper_WeekOfYear_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WeekOfYear" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_77_Helper_WeekOfYear_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WeekOfYear" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_78_Helper_GetWeekTime_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetWeekTime" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_79_Helper_GetCurrentWeek_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetCurrentWeek" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_80_Helper_GetCurrentMonth_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetCurrentMonth" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_81_Helper_GetCurrentYear_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetCurrentYear" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_82_Helper_GetCurrentQuarter_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetCurrentQuarter" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_83_Helper_GetCurrentRange_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetCurrentRange" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_84_Helper_GetDateTime_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDateTime" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_85_Helper_GetTotalSeconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalSeconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_86_Helper_GetTotalMilliseconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalMilliseconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_87_Helper_GetTotalMicroseconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalMicroseconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_88_Helper_GetTotalNanoseconds_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalNanoseconds" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_89_Helper_GetTotalMinutes_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalMinutes" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_90_Helper_GetTotalHours_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalHours" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_91_Helper_GetTotalDays_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTotalDays" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_92_Helper_GetDaysOfYear_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDaysOfYear" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_93_Helper_GetDaysOfMonth_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDaysOfMonth" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_94_Helper_GetWeekNameOfDay_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetWeekNameOfDay" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_95_Helper_In_4Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "In" && m.GetParameters().Length == 4);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_96_Helper_GetMonthLastDate_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetMonthLastDate" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_97_Helper_GetTimeDelay_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetTimeDelay" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_98_Helper_DateDiff_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DateDiff" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_99_Helper_GetDiffTime_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetDiffTime" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_100_DateTimeRange_HasIntersect_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "HasIntersect" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_101_DateTimeRange_HasIntersect_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "HasIntersect" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_102_DateTimeRange_Contains_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_103_DateTimeRange_Contains_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_104_DateTimeRange_In_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "In" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_105_DateTimeRange_In_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "In" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_106_DateTimeRange_Union_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Union" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_107_DateTimeRange_Union_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.DateTimeRange");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Union" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_108_Helper_CreateTempFile_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CreateTempFile" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_109_Helper_ClearTempFiles_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ClearTempFiles" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_110_Helper_DESEncrypt_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DESEncrypt" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_111_Helper_DESDecrypt_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DESDecrypt" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_112_Helper_DESEncrypt4Hex_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DESEncrypt4Hex" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_113_Helper_DESDecrypt4Hex_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DESDecrypt4Hex" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_114_Helper_DESEncrypt_4Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DESEncrypt" && m.GetParameters().Length == 4);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_115_Helper_DESDecrypt_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DESDecrypt" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_116_Helper_ZipStream_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ZipStream" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_117_Helper_ZipStream_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ZipStream" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_118_Helper_Zip_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Zip" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_119_Helper_Zip_5Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Zip" && m.GetParameters().Length == 5);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_120_Helper_Decompress_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Decompress" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_121_Helper_CreateZipArchive_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CreateZipArchive" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_122_Helper_ToBase32String_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToBase32String" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_123_Helper_FromBase32String_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "FromBase32String" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_124_Helper_DecodeBase32Char_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "DecodeBase32Char" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_125_Helper_IsNumber_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsNumber" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_126_Helper_IsNumberic_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsNumberic" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_127_Helper_IsTel_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsTel" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_128_Helper_IsPhone_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsPhone" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_129_Helper_IsFax_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsFax" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_130_Helper_IsMobile_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsMobile" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_131_Helper_IsIDCard_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsIDCard" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_132_Helper_IsIDCard18_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsIDCard18" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_133_Helper_IsIDCard15_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsIDCard15" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_134_Helper_IsEmail_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsEmail" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_135_Helper_IsOnlyChinese_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsOnlyChinese" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_136_Helper_IsBadString_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsBadString" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_137_Helper_IsNzx_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsNzx" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_138_Helper_IsSzzmChinese_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsSzzmChinese" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_139_Helper_IsSzzm_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsSzzm" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_140_Helper_IsPostCode_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsPostCode" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_141_Helper_CheckLength_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CheckLength" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_142_Helper_IsDateTime_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsDateTime" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_143_Helper_IsImplementInterface_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Helper");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "IsImplementInterface" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_144_Stopwatcher_Watch_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Stopwatcher");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Watch" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_145_Stopwatcher_Write_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Stopwatcher");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Write" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_146_Stopwatcher_WriteLog_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Stopwatcher");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteLog" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_147_Buffer_AsSpan_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "AsSpan" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_148_Buffer_AsReadableSpan_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "AsReadableSpan" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_149_Buffer_Clear_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Clear" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_150_Buffer_ExtractString_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ExtractString" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_151_Buffer_Remove_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Remove" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_152_Buffer_Reserve_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Reserve" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_153_Buffer_Resize_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Resize" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_154_Buffer_Shift_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Shift" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_155_Buffer_Unshift_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Unshift" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_156_Buffer_Append_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Append" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_157_Buffer_Append_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Append" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_158_Buffer_Advance_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Advance" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_159_Buffer_GetMemory_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetMemory" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_160_Buffer_GetSpan_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("Framework.Net.Buffer");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetSpan" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_161_DisposableDictionary_Dispose_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Collections.Generic.DisposableDictionary");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Dispose" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_162_DisposableDictionary_Dispose_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Collections.Generic.DisposableDictionary");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Dispose" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_163_Extensions_StandardDeviation_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Collections.Generic.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "StandardDeviation" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_164_TaskToApm_Begin_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Threading.TaskToApm");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Begin" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_165_TaskToApm_End_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Threading.TaskToApm");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "End" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_166_TaskToApm_InvokeCallback_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Threading.TaskToApm");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "InvokeCallback" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_167_Extensions_Forget_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Threading.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Forget" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_168_Extensions_CompleteOnCurrentThread_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Threading.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CompleteOnCurrentThread" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_169_Extensions_WriteCR_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteCR" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_170_Extensions_WriteLF_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteLF" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_171_Extensions_WriteCRLF_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteCRLF" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_172_Extensions_Write_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Write" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_173_Extensions_WriteBigEndian_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteBigEndian" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_174_Extensions_WriteLittleEndian_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteLittleEndian" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_175_PooledByteBuf_EnsureWritable_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "EnsureWritable" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_176_PooledByteBuf_CheckReadable_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CheckReadable" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_177_PooledByteBuf_WriteStringFixed_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteStringFixed" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_178_PooledByteBuf_ReadString_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadString" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_179_PooledByteBuf_WriteByte_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteByte" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_180_PooledByteBuf_WriteBytes_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteBytes" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_181_PooledByteBuf_WriteShort_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteShort" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_182_PooledByteBuf_WriteShortLE_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteShortLE" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_183_PooledByteBuf_WriteInt_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteInt" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_184_PooledByteBuf_WriteIntLE_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteIntLE" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_185_PooledByteBuf_WriteLong_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteLong" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_186_PooledByteBuf_WriteLongLE_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteLongLE" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_187_PooledByteBuf_WriteFloat_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteFloat" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_188_PooledByteBuf_WriteFloatLE_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteFloatLE" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_189_PooledByteBuf_WriteDouble_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteDouble" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_190_PooledByteBuf_WriteDoubleLE_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WriteDoubleLE" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_191_PooledByteBuf_ReadBytes_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadBytes" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_192_PooledByteBuf_ReadBytes_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadBytes" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_193_PooledByteBuf_ReadShort_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadShort" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_194_PooledByteBuf_ReadShortLE_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadShortLE" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_195_PooledByteBuf_ReadInt_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadInt" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_196_PooledByteBuf_ReadIntLE_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadIntLE" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_197_PooledByteBuf_ReadLong_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadLong" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_198_PooledByteBuf_ReadLongLE_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadLongLE" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_199_PooledByteBuf_ReadFloat_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadFloat" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_200_PooledByteBuf_ReadFloatLE_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadFloatLE" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_201_PooledByteBuf_ReadDouble_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadDouble" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_202_PooledByteBuf_ReadDoubleLE_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadDoubleLE" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_203_PooledByteBuf_GetWriteSpan_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetWriteSpan" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_204_PooledByteBuf_GetReadSpan_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetReadSpan" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_205_PooledByteBuf_ToArray_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToArray" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_206_PooledByteBuf_ToFullArray_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ToFullArray" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_207_PooledByteBuf_ReadSpan_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "ReadSpan" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_208_PooledByteBuf_WrittenSpan_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "WrittenSpan" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_209_PooledByteBuf_Dispose_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.Buffers.PooledByteBuf");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "Dispose" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_210_DuplexPipeStream_CancelPendingRead_0Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.DuplexPipeStream");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CancelPendingRead" && m.GetParameters().Length == 0);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_211_Extensions_CopyToFile_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CopyToFile" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_212_Extensions_CopyToFileAsync_3Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "CopyToFileAsync" && m.GetParameters().Length == 3);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_213_Extensions_SaveFile_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "SaveFile" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_214_Extensions_GetFileMD5_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetFileMD5" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_215_Extensions_GetFileSha1_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetFileSha1" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_216_Extensions_GetFileSha256_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetFileSha256" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_217_Extensions_GetFileSha512_1Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "GetFileSha512" && m.GetParameters().Length == 1);
        Assert.NotNull(method);
    }

    [Fact]
    public void Method_218_Extensions_HashFile_2Params_Exists()
    {
        var assembly = Assembly.Load("Framework");
        var type = assembly.GetType("System.IO.Extensions");
        Assert.NotNull(type);
        var method = type!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .FirstOrDefault(m => m.Name == "HashFile" && m.GetParameters().Length == 2);
        Assert.NotNull(method);
    }

}