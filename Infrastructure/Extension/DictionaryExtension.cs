using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Extension;

public static class DictionaryExtension
{
	public static TValue GetOrAdd<TKey, TValue>(
	   this IDictionary<TKey, TValue> dictionary,
	   TKey key,
	   Func<TKey, TValue> factory)
	{
		if (dictionary.TryGetValue(key, out TValue obj))
		{
			return obj;
		}

		return dictionary[key] = factory(key);
	}

	public static TValue GetOrAdd<TKey, TValue>(
	   this IDictionary<TKey, TValue> dictionary,
	   TKey key,
	   Func<TValue> factory)
	{
		return dictionary.GetOrAdd(key, k => factory());
	}

	public static TValue AddOrUpdate<TKey, TValue>(
	   this IDictionary<TKey, TValue> dictionary,
	   TKey key,
	   Func<TKey, TValue> addFactory,
	   Func<TKey, TValue, TValue> updateFactory)
	{
		if (dictionary.TryGetValue(key, out TValue obj))
		{
			obj = updateFactory(key, obj);
		}
		else
		{
			obj = addFactory(key);
		}
		dictionary[key] = obj;
		return obj;
	}

	public static TValue AddOrUpdate<TKey, TValue>(
	   this IDictionary<TKey, TValue> dictionary,
	   TKey key,
	   Func<TValue> addFactory,
	   Func<TValue, TValue> updateFactory)
	{
		return dictionary.AddOrUpdate(key, k => addFactory(), (k, v) => updateFactory(v));
	}
}