using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Middleware.Modbus
{
	internal static class DictionaryExtensions
	{
		/// <summary>
		/// Gets the specified value in the dictionary. If not found, returns default for TValue.
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="dictionary"></param>
		/// <param name="key"></param>
		/// <returns></returns>
		internal static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			TValue value;

			if (dictionary.TryGetValue(key, out value))
				return value;

			return default(TValue);
		}
	}
}
