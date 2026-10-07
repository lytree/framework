using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Framework.DynamicApi.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.DynamicApi;

/// <summary>
/// Api分组约定
/// </summary>
public class ApiGroupConvention : IControllerModelConvention
{
	public void Apply(ControllerModel controller)
	{
		if (controller.Attributes?.Count > 0)
		{
			foreach (var attribute in controller.Attributes)
			{
				if (attribute is AreaAttribute area)
				{
					if (controller.ApiExplorer.GroupName == null)
					{
						controller.ApiExplorer.GroupName = area.RouteValue?.ToLower();
					}
					break;
				}
				else if (attribute is DynamicApiAttribute dynamicApi)
				{
					if (controller.ApiExplorer.GroupName == null)
					{
						controller.ApiExplorer.GroupName = dynamicApi.Area?.ToLower();
					}
					break;
				}
			}
		}
	}
}