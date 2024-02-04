using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Helper;

/// <summary>
/// 接口帮助类
/// </summary>
public class InterfaceHelper
{
	public static string[] GetPropertyNames<T>() where T : class
	{
		Type interfaceType = typeof(T);

		if (!interfaceType.IsInterface)
		{
			throw new ArgumentException($"{interfaceType.FullName} is not an interface type.");
		}

		PropertyInfo[] properties = interfaceType.GetProperties();

		string[] propertyNames = properties.Select(p => p.Name).ToArray();

		return propertyNames;
	}
}
