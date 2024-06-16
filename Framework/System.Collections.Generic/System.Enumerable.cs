using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic;


/// <summary>
///
/// </summary>
public static partial class Extensions
{
	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TFirst"></typeparam>
	/// <typeparam name="TSecond"></typeparam>
	/// <param name="second"></param>
	/// <param name="condition"></param>
	/// <param name="first"></param>
	/// <returns></returns>
	public static IEnumerable<TFirst> IntersectBy<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> condition)
	{
		return first.Where(f => second.Any(s => condition(f, s)));
	}

	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="keySelector"></param>
	/// <returns></returns>
	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector)
	{
		return first.IntersectBy(second, keySelector, null);
	}

	/// <summary>
	/// 按字段属性判等取交集
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="keySelector"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	public static IEnumerable<TSource> IntersectBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		if (first == null)
			throw new ArgumentNullException(nameof(first));
		if (second == null)
			throw new ArgumentNullException(nameof(second));
		if (keySelector == null)
			throw new ArgumentNullException(nameof(keySelector));
		return IntersectByIterator(first, second, keySelector, comparer);
	}

	private static IEnumerable<TSource> IntersectByIterator<TSource, TKey>(IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		var set = new HashSet<TKey>(second.Select(keySelector), comparer);
		foreach (var item in first.Where(source => set.Remove(keySelector(source))))
		{
			yield return item;
		}
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <returns></returns>
	public static IEnumerable<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> source)
	{
		return source.Aggregate((current, item) => current.Intersect(item));
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector"></param>
	/// <returns></returns>
	public static IEnumerable<TSource> IntersectAll<TSource, TKey>(this IEnumerable<IEnumerable<TSource>> source, Func<TSource, TKey> keySelector)
	{
		return source.Aggregate((current, item) => current.IntersectBy(item, keySelector));
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="source"></param>
	/// <param name="keySelector"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	public static IEnumerable<TSource> IntersectAll<TSource, TKey>(this IEnumerable<IEnumerable<TSource>> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		return source.Aggregate((current, item) => current.IntersectBy(item, keySelector, comparer));
	}

	/// <summary>
	/// 多个集合取交集元素
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="comparer"></param>
	/// <returns></returns>
	public static IEnumerable<T> IntersectAll<T>(this IEnumerable<IEnumerable<T>> source, IEqualityComparer<T> comparer)
	{
		return source.Aggregate((current, item) => current.Intersect(item, comparer));
	}

	/// <summary>
	/// 按字段属性判等取差集
	/// </summary>
	/// <typeparam name="TFirst"></typeparam>
	/// <typeparam name="TSecond"></typeparam>
	/// <param name="second"></param>
	/// <param name="condition"></param>
	/// <param name="first"></param>
	/// <returns></returns>
	public static IEnumerable<TFirst> ExceptBy<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, bool> condition)
	{
		return first.Where(f => !second.Any(s => condition(f, s)));
	}



	/// <summary>
	/// 添加多个元素
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="values"></param>
	public static void AddRange<T>(this ConcurrentBag<T> @this, params T[] values)
	{
		foreach (var obj in values)
		{
			@this.Add(obj);
		}
	}

	/// <summary>
	/// 添加多个元素
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="values"></param>
	public static void AddRange<T>(this ConcurrentQueue<T> @this, params T[] values)
	{
		foreach (var obj in values)
		{
			@this.Enqueue(obj);
		}
	}

	/// <summary>
	/// 添加符合条件的多个元素
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="predicate"></param>
	/// <param name="values"></param>
	public static void AddRangeIf<T>(this ConcurrentBag<T> @this, Func<T, bool> predicate, params T[] values)
	{
		foreach (var obj in values.Where(predicate))
		{
			@this.Add(obj);
		}
	}

	/// <summary>
	/// 添加符合条件的多个元素
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="predicate"></param>
	/// <param name="values"></param>
	public static void AddRangeIf<T>(this ConcurrentQueue<T> @this, Func<T, bool> predicate, params T[] values)
	{
		foreach (var obj in values.Where(predicate))
		{
			@this.Enqueue(obj);
		}
	}


	/// <summary>
	/// 转HashSet
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="source"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static HashSet<TResult> ToHashSet<T, TResult>(this IEnumerable<T> source, Func<T, TResult> selector)
	{
		return new HashSet<TResult>(source.Select(selector));
	}

	/// <summary>
	/// 遍历IEnumerable
	/// </summary>
	/// <param name="objs"></param>
	/// <param name="action">回调方法</param>
	/// <typeparam name="T"></typeparam>
	public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
	{
		foreach (var o in @this)
		{
			action(o);
		}
	}

	/// <summary>
	/// 异步foreach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <param name="action"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static async Task ForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, int maxParallelCount, CancellationToken cancellationToken = default)
	{
		if (Debugger.IsAttached)
		{
			foreach (var item in source)
			{
				await action(item);
			}

			return;
		}

		var list = new List<Task>();
		foreach (var item in source)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			list.Add(action(item));
			if (list.Count(t => !t.IsCompleted) >= maxParallelCount)
			{
				await Task.WhenAny(list);
				list.RemoveAll(t => t.IsCompleted);
			}
		}

		await Task.WhenAll(list);
	}

	/// <summary>
	/// 异步foreach
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="action"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public static Task ForeachAsync<T>(this IEnumerable<T> source, Func<T, Task> action, CancellationToken cancellationToken = default)
	{
		if (source is ICollection<T> collection)
		{
			return ForeachAsync(collection, action, collection.Count, cancellationToken);
		}

		var list = source.ToList();
		return ForeachAsync(list, action, list.Count, cancellationToken);
	}

	/// <summary>
	/// 异步Select
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="source"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static Task<TResult[]> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, Task<TResult>> selector)
	{
		return Task.WhenAll(source.Select(selector));
	}

	/// <summary>
	/// 异步Select
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="source"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static Task<TResult[]> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, int, Task<TResult>> selector)
	{
		return Task.WhenAll(source.Select(selector));
	}

	/// <summary>
	/// 异步Select
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="source"></param>
	/// <param name="selector"></param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <returns></returns>
	public static async Task<List<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> source, Func<T, Task<TResult>> selector, int maxParallelCount)
	{
		var results = new List<TResult>();
		var tasks = new List<Task<TResult>>();
		foreach (var item in source)
		{
			var task = selector(item);
			tasks.Add(task);
			if (tasks.Count >= maxParallelCount)
			{
				await Task.WhenAny(tasks);
				var completedTasks = tasks.Where(t => t.IsCompleted).ToArray();
				results.AddRange(completedTasks.Select(t => t.Result));
				tasks.RemoveWhere(t => completedTasks.Contains(t));
			}
		}

		results.AddRange(await Task.WhenAll(tasks));
		return results;
	}

	/// <summary>
	/// 异步Select
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <returns></returns>
	public static async Task<List<TResult>> SelectAsync<T, TResult>(this IEnumerable<T> @this, Func<T, int, Task<TResult>> selector, int maxParallelCount)
	{
		var results = new List<TResult>();
		var tasks = new List<Task<TResult>>();
		int index = 0;
		foreach (var item in @this)
		{
			var task = selector(item, index);
			tasks.Add(task);
			Interlocked.Add(ref index, 1);
			if (tasks.Count >= maxParallelCount)
			{
				await Task.WhenAny(tasks);
				var completedTasks = tasks.Where(t => t.IsCompleted).ToArray();
				results.AddRange(completedTasks.Select(t => t.Result));
				tasks.RemoveWhere(t => completedTasks.Contains(t));
			}
		}

		results.AddRange(await Task.WhenAll(tasks));
		return results;
	}

	/// <summary>
	/// 异步For
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="selector"></param>
	/// <param name="maxParallelCount">最大并行数</param>
	/// <param name="cancellationToken">取消口令</param>
	/// <returns></returns>
	public static async Task ForAsync<T>(this IEnumerable<T> @this, Func<T, int, Task> selector, int maxParallelCount, CancellationToken cancellationToken = default)
	{
		int index = 0;
		if (Debugger.IsAttached)
		{
			foreach (var item in @this)
			{
				await selector(item, index);
				index++;
			}

			return;
		}

		var list = new List<Task>();
		foreach (var item in @this)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			list.Add(selector(item, index));
			Interlocked.Add(ref index, 1);
			if (list.Count >= maxParallelCount)
			{
				await Task.WhenAny(list);
				list.RemoveAll(t => t.IsCompleted);
			}
		}

		await Task.WhenAll(list);
	}

	/// <summary>
	/// 异步For
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <param name="selector"></param>
	/// <param name="cancellationToken">取消口令</param>
	/// <returns></returns>
	public static Task ForAsync<T>(this IEnumerable<T> @this, Func<T, int, Task> selector, CancellationToken cancellationToken = default)
	{
		if (@this is ICollection<T> collection)
		{
			return ForAsync(collection, selector, collection.Count, cancellationToken);
		}

		var list = @this.ToList();
		return ForAsync(list, selector, list.Count, cancellationToken);
	}

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IQueryable<TSource> @this, Expression<Func<TSource, TResult>> selector) => @this.Select(selector).DefaultIfEmpty().Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IQueryable<TSource> @this, Expression<Func<TSource, TResult>> selector, TResult defaultValue) => @this.Select(selector).DefaultIfEmpty(defaultValue).Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <returns></returns>
	public static TSource MaxOrDefault<TSource>(this IQueryable<TSource> @this) => @this.DefaultIfEmpty().Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TSource MaxOrDefault<TSource>(this IQueryable<TSource> @this, TSource defaultValue) => @this.DefaultIfEmpty(defaultValue).Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> @this, Func<TSource, TResult> selector, TResult defaultValue) => @this.Select(selector).DefaultIfEmpty(defaultValue).Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static TResult MaxOrDefault<TSource, TResult>(this IEnumerable<TSource> @this, Func<TSource, TResult> selector) => @this.Select(selector).DefaultIfEmpty().Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <returns></returns>
	public static TSource MaxOrDefault<TSource>(this IEnumerable<TSource> @this) => @this.DefaultIfEmpty().Max();

	/// <summary>
	/// 取最大值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TSource MaxOrDefault<TSource>(this IEnumerable<TSource> @this, TSource defaultValue) => @this.DefaultIfEmpty(defaultValue).Max();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static TResult MinOrDefault<TSource, TResult>(this IQueryable<TSource> @this, Expression<Func<TSource, TResult>> selector) => @this.Select(selector).DefaultIfEmpty().Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TResult MinOrDefault<TSource, TResult>(this IQueryable<TSource> @this, Expression<Func<TSource, TResult>> selector, TResult defaultValue) => @this.Select(selector).DefaultIfEmpty(defaultValue).Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <returns></returns>
	public static TSource MinOrDefault<TSource>(this IQueryable<TSource> @this) => @this.DefaultIfEmpty().Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TSource MinOrDefault<TSource>(this IQueryable<TSource> @this, TSource defaultValue) => @this.DefaultIfEmpty(defaultValue).Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> @this, Func<TSource, TResult> selector) => @this.Select(selector).DefaultIfEmpty().Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TResult MinOrDefault<TSource, TResult>(this IEnumerable<TSource> @this, Func<TSource, TResult> selector, TResult defaultValue) => @this.Select(selector).DefaultIfEmpty(defaultValue).Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <returns></returns>
	public static TSource MinOrDefault<TSource>(this IEnumerable<TSource> @this) => @this.DefaultIfEmpty().Min();

	/// <summary>
	/// 取最小值
	/// </summary>
	/// <typeparam name="TSource"></typeparam>
	/// <param name="this"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static TSource MinOrDefault<TSource>(this IEnumerable<TSource> @this, TSource defaultValue) => @this.DefaultIfEmpty(defaultValue).Min();

	/// <summary>
	/// 标准差
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	/// <param name="this"></param>
	/// <param name="selector"></param>
	/// <returns></returns>
	public static TResult StandardDeviation<T, TResult>(this IEnumerable<T> @this, Func<T, TResult> selector) where TResult : IConvertible
	{
		return StandardDeviation(@this.Select(t => selector(t).ConvertTo<double>())).ConvertTo<TResult>();
	}

	/// <summary>
	/// 标准差
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <returns></returns>
	public static T StandardDeviation<T>(this IEnumerable<T> @this) where T : IConvertible
	{
		return StandardDeviation(@this.Select(t => t.ConvertTo<double>())).ConvertTo<T>();
	}

	/// <summary>
	/// 标准差
	/// </summary>
	/// <param name="this"></param>
	/// <returns></returns>
	public static double StandardDeviation(this IEnumerable<double> @this)
	{
		double result = 0;
		var list = @this as ICollection<double> ?? @this.ToList();
		int count = list.Count;
		if (count > 1)
		{
			var avg = list.Average();
			var sum = list.Sum(d => (d - avg) * (d - avg));
			result = Math.Sqrt(sum / count);
		}

		return result;
	}

	/// <summary>
	/// 序列相等
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	public static bool SequenceEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T, T, bool> condition)
	{
		if (first is ICollection<T> source1 && second is ICollection<T> source2)
		{
			if (source1.Count != source2.Count)
			{
				return false;
			}

			if (source1 is IList<T> list1 && source2 is IList<T> list2)
			{
				int count = source1.Count;
				for (int index = 0; index < count; ++index)
				{
					if (!condition(list1[index], list2[index]))
					{
						return false;
					}
				}

				return true;
			}
		}

		using IEnumerator<T> enumerator1 = first.GetEnumerator();
		using IEnumerator<T> enumerator2 = second.GetEnumerator();
		while (enumerator1.MoveNext())
		{
			if (!enumerator2.MoveNext() || !condition(enumerator1.Current, enumerator2.Current))
			{
				return false;
			}
		}

		return !enumerator2.MoveNext();
	}

	/// <summary>
	/// 序列相等
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="condition"></param>
	/// <returns></returns>
	public static bool SequenceEqual<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> condition)
	{
		if (first is ICollection<T1> source1 && second is ICollection<T2> source2)
		{
			if (source1.Count != source2.Count)
			{
				return false;
			}

			if (source1 is IList<T1> list1 && source2 is IList<T2> list2)
			{
				int count = source1.Count;
				for (int index = 0; index < count; ++index)
				{
					if (!condition(list1[index], list2[index]))
					{
						return false;
					}
				}

				return true;
			}
		}

		using IEnumerator<T1> enumerator1 = first.GetEnumerator();
		using IEnumerator<T2> enumerator2 = second.GetEnumerator();
		while (enumerator1.MoveNext())
		{
			if (!enumerator2.MoveNext() || !condition(enumerator1.Current, enumerator2.Current))
			{
				return false;
			}
		}

		return !enumerator2.MoveNext();
	}

	/// <summary>
	/// 对比两个集合哪些是新增的、删除的、修改的
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="condition">对比因素条件</param>
	/// <returns></returns>
	public static (List<T1> adds, List<T2> remove, List<T1> updates) CompareChanges<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> condition)
	{
		first ??= new List<T1>();
		second ??= new List<T2>();
		var firstSource = first as ICollection<T1> ?? first.ToList();
		var secondSource = second as ICollection<T2> ?? second.ToList();
		var add = firstSource.ExceptBy(secondSource, condition).ToList();
		var remove = secondSource.ExceptBy(firstSource, (s, f) => condition(f, s)).ToList();
		var update = firstSource.IntersectBy(secondSource, condition).ToList();
		return (add, remove, update);
	}

	/// <summary>
	/// 对比两个集合哪些是新增的、删除的、修改的
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <param name="first"></param>
	/// <param name="second"></param>
	/// <param name="condition">对比因素条件</param>
	/// <returns></returns>
	public static (List<T1> adds, List<T2> remove, List<(T1 first, T2 second)> updates) CompareChangesPlus<T1, T2>(this IEnumerable<T1> first, IEnumerable<T2> second, Func<T1, T2, bool> condition)
	{
		first ??= new List<T1>();
		second ??= new List<T2>();
		var firstSource = first as ICollection<T1> ?? first.ToList();
		var secondSource = second as ICollection<T2> ?? second.ToList();
		var add = firstSource.ExceptBy(secondSource, condition).ToList();
		var remove = secondSource.ExceptBy(firstSource, (s, f) => condition(f, s)).ToList();
		var updates = firstSource.IntersectBy(secondSource, condition).Select(t1 => (t1, secondSource.FirstOrDefault(t2 => condition(t1, t2)))).ToList();
		return (add, remove, updates);
	}

	/// <summary>
	/// 将集合声明为非null集合
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list"></param>
	/// <returns></returns>
	public static List<T> AsNotNull<T>(this List<T> list)
	{
		return list ?? new List<T>();
	}

	/// <summary>
	/// 将集合声明为非null集合
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="list"></param>
	/// <returns></returns>
	public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T> list)
	{
		return list ?? new List<T>();
	}

	/// <summary>
	/// 满足条件时执行筛选条件
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="condition"></param>
	/// <param name="where"></param>
	/// <returns></returns>
	public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> @this, bool condition, Func<T, bool> where)
	{
		return condition ? @this.Where(where) : @this;
	}

	/// <summary>
	/// 满足条件时执行筛选条件
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="condition"></param>
	/// <param name="where"></param>
	/// <returns></returns>
	public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> @this, Func<bool> condition, Func<T, bool> where)
	{
		return condition() ? @this.Where(where) : @this;
	}

	/// <summary>
	/// 满足条件时执行筛选条件
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="condition"></param>
	/// <param name="where"></param>
	/// <returns></returns>
	public static IQueryable<T> WhereIf<T>(this IQueryable<T> @this, bool condition, Expression<Func<T, bool>> where)
	{
		return condition ? @this.Where(where) : @this;
	}

	/// <summary>
	/// 满足条件时执行筛选条件
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="condition"></param>
	/// <param name="where"></param>
	/// <returns></returns>
	public static IQueryable<T> WhereIf<T>(this IQueryable<T> @this, Func<bool> condition, Expression<Func<T, bool>> where)
	{
		return condition() ? @this.Where(where) : @this;
	}

}
