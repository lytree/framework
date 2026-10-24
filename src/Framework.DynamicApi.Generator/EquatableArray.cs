using System;
using System.Collections;
using System.Collections.Generic;

namespace Framework.DynamicApi.Generator;

/// <summary>
/// 具备结构相等语义的数组包装，供增量生成器的模型缓存使用（数组自身按引用比较，会导致缓存失效）。
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

	private readonly T[]? _items;

	public EquatableArray(T[] items)
	{
		_items = items;
	}

	public int Count => _items?.Length ?? 0;

	public T this[int index] => _items![index];

	public bool Equals(EquatableArray<T> other)
	{
		var left = _items ?? Array.Empty<T>();
		var right = other._items ?? Array.Empty<T>();

		if (left.Length != right.Length)
			return false;

		for (var i = 0; i < left.Length; i++)
		{
			if (!left[i].Equals(right[i]))
				return false;
		}

		return true;
	}

	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		var items = _items ?? Array.Empty<T>();
		unchecked
		{
			var hash = 17;
			for (var i = 0; i < items.Length; i++)
			{
				hash = hash * 31 + items[i].GetHashCode();
			}

			return hash;
		}
	}

	public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
