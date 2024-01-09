using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP.Infrastructure.Helper
{
	public static class AttributeHelper
	{
		public static string GetDisplayName<T>(string propertyName)
		{
			return (TypeDescriptor.GetProperties(typeof(T))[propertyName].Attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute).DisplayName ?? "";
		}
	}
}
