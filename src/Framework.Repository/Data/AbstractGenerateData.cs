using Framework;
using Framework.Repository;
using Framework.Repository.Attributes;
using Framework.Repository.Entities;
using Framework.System;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace Framework.Repository.Data;


/// <summary>
/// 数据库初始数据导出基类。
/// 提供将实体集合序列化为 JSON 文件的通用流程：依据 <see cref="TableAttribute"/> 推断输出文件名、按租户维度生成不同变体，并通过 <see cref="NotGenAttribute"/> 跳过不需要导出的字段。
/// 子类在生成初始化数据时通常无需重写 <see cref="IgnorePropName"/>，仅需调用 <see cref="SaveDataToJsonFile{T}"/>。
/// </summary>
public abstract class AbstractGenerateData
{
    // 缓存实体中租户列名（首条匹配的接口属性名，小写形式），用于在 JSON 序列化时单独判断是否输出
    private readonly string? _tenantName = Helper.GetInterfacePropertyNames<ITenant>().FirstOrDefault()?.ToLower();

    /// <summary>
    /// 为 System.Text.Json 注册属性过滤回调。
    /// 当属性名匹配租户列且类型实现 <see cref="ITenant"/> 时，使用 <paramref name="isTenant"/> 控制是否序列化；否则依据 <see cref="NotGenAttribute"/> 决定是否跳过。
    /// </summary>
    /// <param name="ti">当前正在解析的 JSON 类型元数据 <see cref="JsonTypeInfo"/>。</param>
    /// <param name="isTenant">是否处于租户数据导出场景。</param>
    protected virtual void IgnorePropName(JsonTypeInfo ti, bool isTenant)
    {
        foreach (var jsonPropertyInfo in ti.Properties)
        {
            jsonPropertyInfo.ShouldSerialize = (obj, _) =>
            {
                if (jsonPropertyInfo.Name.ToLower() == _tenantName && Helper.IsImplementInterface(ti.Type, typeof(ITenant)))
                {
                    return isTenant;
                }

                return !jsonPropertyInfo.AttributeProvider.IsDefined(typeof(NotGenAttribute), false);
            };
        }
    }


    /// <summary>
    /// 将给定的数据对象集合序列化为 JSON 文件并写入磁盘。
    /// 文件名取自泛型类型 <typeparamref name="T"/> 上 <see cref="TableAttribute"/> 的配置（若未配置则使用类型名），租户场景会在扩展名前追加 <c>.tenant</c>。
    /// </summary>
    /// <typeparam name="T">导出对应的实体类型，需具备无参构造函数并标注 <see cref="TableAttribute"/>。</typeparam>
    /// <param name="data">要序列化的数据对象（通常是实体集合或单个实体）。</param>
    /// <param name="isTenant">是否为租户数据；为 <c>true</c> 时写入租户变体文件并序列化租户列。</param>
    /// <param name="path">输出目录（相对当前工作目录），默认为 <c>InitData/Admin</c>。</param>
    protected virtual void SaveDataToJsonFile<T>(object data, bool isTenant = false, string path = "InitData/Admin") where T : class, new()
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.Create(new TextEncoderSettings(UnicodeRanges.All)),
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { (JsonTypeInfo ti) => IgnorePropName(ti, isTenant) }
            }
        };

        var table = typeof(T).GetCustomAttributes(typeof(TableAttribute), false).FirstOrDefault() as TableAttribute;
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{path}/{table?.Name}{(isTenant ? ".tenant" : "")}.json").ToPath();

        var jsonData = JsonSerializer.Serialize(data, jsonSerializerOptions);

        FileHelper.WriteFile(filePath, jsonData);
    }
}