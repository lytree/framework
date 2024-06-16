using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic;

/// <summary>
/// 
/// </summary>
public static partial class Extensions
{
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that adds only if the value satisfies the predicate.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="predicate">The predicate.</param>
	/// <param name="value">The value.</param>
	/// <returns>true if it succeeds, false if it fails.</returns>
	public static bool AddIf<T>(this ICollection<T> @this, Func<T, bool> predicate, T value)
	{
		if (predicate(value))
		{
			@this.Add(value);
			return true;
		}

		return false;
	}
	/// <summary>
	///     Adds <paramref name="value" /> to it lacks this value.
	/// </summary>
	/// <typeparam name="T">Type of generic <see cref="ICollection{T}" /></typeparam>
	/// <param name="source">
	///     The <see cref="ICollection{T}" /> where should be inserted a distinct <paramref name="value" />
	/// </param>
	/// <param name="value">Value to be inserted in <paramref name="source" /> if it is a distinct one.</param>
	public static void AddIfNotContains<T>(this ICollection<T> @this, T value)
	{
		if (!@this.Contains(value)) @this.Add(value);
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that adds a range to 'values'.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	public static void AddRange<T>(this ICollection<T> @this, params T[] values)
	{
		foreach (T value in values)
		{
			@this.Add(value);
		}
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that adds a collection of objects to the end of this collection only
	///     for value who satisfies the predicate.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="predicate">The predicate.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	public static void AddRangeIf<T>(this ICollection<T> @this, Func<T, bool> predicate, params T[] values)
	{
		foreach (T value in values)
		{
			if (predicate(value))
			{
				@this.Add(value);
			}
		}
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that adds a range of values that's not already in the ICollection.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	public static void AddRangeIfNotContains<T>(this ICollection<T> @this, params T[] values)
	{
		foreach (T value in values)
		{
			if (!@this.Contains(value))
			{
				@this.Add(value);
			}
		}
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that query if '@this' contains all values.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	/// <returns>true if it succeeds, false if it fails.</returns>
	public static bool ContainsAll<T>(this ICollection<T> @this, params T[] values)
	{
		foreach (T value in values)
		{
			if (!@this.Contains(value))
			{
				return false;
			}
		}

		return true;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that query if '@this' contains any value.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	/// <returns>true if it succeeds, false if it fails.</returns>
	public static bool ContainsAny<T>(this ICollection<T> @this, params T[] values)
	{
		foreach (T value in values)
		{
			if (@this.Contains(value))
			{
				return true;
			}
		}

		return false;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that query if the collection is empty.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <returns>true if empty&lt; t&gt;, false if not.</returns>
	public static bool IsEmpty<T>(this ICollection<T> @this)
	{
		return @this.Count == 0;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that query if the collection is not empty.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <returns>true if not empty&lt; t&gt;, false if not.</returns>
	public static bool IsNotEmpty<T>(this ICollection<T> @this)
	{
		return @this.Count != 0;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that queries if the collection is not (null or is empty).
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <returns>true if the collection is not (null or empty), false if not.</returns>
	public static bool IsNotNullOrEmpty<T>(this ICollection<T> @this)
	{
		return @this != null && @this.Count != 0;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that queries if the collection is null or is empty.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <returns>true if null or empty&lt; t&gt;, false if not.</returns>
	public static bool IsNullOrEmpty<T>(this ICollection<T> @this)
	{
		return @this == null || @this.Count == 0;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that removes if.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="value">The value.</param>
	/// <param name="predicate">The predicate.</param>
	public static void RemoveIf<T>(this ICollection<T> @this, T value, Func<T, bool> predicate)
	{
		if (predicate(value))
		{
			@this.Remove(value);
		}
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that removes if contains.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="value">The value.</param>
	public static void RemoveIfContains<T>(this ICollection<T> @this, T value)
	{
		if (@this.Contains(value))
		{
			@this.Remove(value);
		}
	}
	/// <summary>
	///     Removes the elements of the specified collection from the end of the <paramref name="source"/>.
	/// </summary>
	/// <typeparam name="T">Type of generic <see cref="ICollection{T}" />.</typeparam>
	/// <param name="this">
	///     The <see cref="ICollection{T}" /> where should be inserted items of <paramref name="collection" />
	///     parameter.
	/// </param>
	/// <param name="collection">
	///     The collection whose elements should be removed from the <paramref name="source" />.
	///     The collection itself cannot be <see langword="null" />, but it
	///     can contain elements that are <see langword="null" />, if type <typeparamref name="T" /> is a reference type.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="collection" /> is <see langword="null" />.</exception>
	/// <returns>Reference to the <paramref name="source"/> object.</returns>
	public static ICollection<T> RemoveRange<T>(this ICollection<T> @this, IEnumerable<T> collection)
	{
		if (collection == null) throw new ArgumentNullException(nameof(collection));

		foreach (T item in collection) @this.Remove(item);

		return @this;
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that removes range item that satisfy the predicate.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="predicate">The predicate.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	public static void RemoveRangeIf<T>(this ICollection<T> @this, Func<T, bool> predicate, params T[] values)
	{
		foreach (T value in values)
		{
			if (predicate(value))
			{
				@this.Remove(value);
			}
		}
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that removes the range if contains.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="values">A variable-length parameters list containing values.</param>
	public static void RemoveRangeIfContains<T>(this ICollection<T> @this, params T[] values)
	{
		foreach (T value in values)
		{
			if (@this.Contains(value))
			{
				@this.Remove(value);
			}
		}
	}
	/// <summary>
	///     An ICollection&lt;T&gt; extension method that removes value that satisfy the predicate.
	/// </summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <param name="this">The @this to act on.</param>
	/// <param name="predicate">The predicate.</param>
	public static void RemoveWhere<T>(this ICollection<T> @this, Func<T, bool> predicate)
	{
		List<T> list = @this.Where(predicate).ToList();
		foreach (T item in list)
		{
			@this.Remove(item);
		}
	}
}