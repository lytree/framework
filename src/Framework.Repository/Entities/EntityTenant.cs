using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Framework.Repository.Entities;

/// <summary>
/// 实体租户
/// </summary>
public class EntityTenant<TKey> : EntityBase<TKey>, ITenant where TKey : struct
{
	/// <summary>
	/// 租户Id
	/// </summary>
	[Description("租户Id")]
	[Column(Position = 2, CanUpdate = false)]
	[JsonPropertyOrder(-20)]
	public virtual long? TenantId { get; set; }
}

/// <summary>
/// 实体租户
/// </summary>
public class EntityTenant : EntityTenant<long>
{
}