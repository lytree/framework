using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic;

public static partial class Extensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="this"></param>
	/// <param name="key"></param>
	/// <param name="factory"></param>
	/// <returns></returns>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> factory)
	{
		if (@this.TryGetValue(key, out TValue obj))
		{
			return obj;
		}

		return @this[key] = factory(key);
	}
	/// <summary>
	/// Gets the value at the given key, or a default value.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	/// <param name="dictionary">The dictionary to retrieve the value from.</param>
	/// <param name="key">The key to retrieve the value for.</param>
	/// <returns>The value associated with the specified key, or the default value if the key is not found.</returns>
	public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.TryGetValue(key, out TValue? value) ? value : default;
	}
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="this"></param>
	/// <param name="key"></param>
	/// <param name="factory"></param>
	/// <returns></returns>
	public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TValue> factory)
	{
		return @this.GetOrAdd(key, k => factory());
	}
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="this"></param>
	/// <param name="key"></param>
	/// <param name="addFactory"></param>
	/// <param name="updateFactory"></param>
	/// <returns></returns>
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> addFactory, Func<TKey, TValue, TValue> updateFactory)
	{
		if (@this.TryGetValue(key, out TValue obj))
		{
			obj = updateFactory(key, obj);
		}
		else
		{
			obj = addFactory(key);
		}
		@this[key] = obj;
		return obj;
	}
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TKey">Type of the key.</typeparam>
	/// <typeparam name="TValue">Type of the value.</typeparam>
	/// <param name="this"></param>
	/// <param name="key">The key.</param>
	/// <param name="addFactory"></param>
	/// <param name="updateFactory"></param>
	/// <returns></returns>
	public static TValue AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key, Func<TValue> addFactory, Func<TValue, TValue> updateFactory)
	{
		return @this.AddOrUpdate(key, k => addFactory(), (k, v) => updateFactory(v));
	}
	/// <summary>
	///     An IDictionary&lt;TKey,TValue&gt; extension method that removes if contains key.
	/// </summary>
	/// <typeparam name="TKey">Type of the key.</typeparam>
	/// <typeparam name="TValue">Type of the value.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="key">The key.</param>
	public static void RemoveIfContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> @this, TKey key)
	{
		if (@this.ContainsKey(key))
		{
			@this.Remove(key);
		}
	}
	/// <summary>
	/// 遍历IEnumerable
	/// </summary>
	/// <param name="dic"></param>
	/// <param name="action">回调方法</param>
	public static void ForEach<TKey, TValue>(this IDictionary<TKey, TValue> dic, Action<TKey, TValue> action)
	{
		foreach (var item in dic)
		{
			action(item.Key, item.Value);
		}
	}
}