using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Repository.Entities;

/// <summary>
/// 租户接口
/// </summary>
public interface ITenant
{
	/// <summary>
	/// 租户Id
	/// </summary>
	long? TenantId { get; set; }
}