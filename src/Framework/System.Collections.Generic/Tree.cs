using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic;

public class Tree<T>
{
	public Tree(T value)
	{
		Value = value;
	}

	public T Value { get; set; }

	/// <summary>
	/// 父节点
	/// </summary>
	public virtual T Parent { get; set; }

	/// <summary>
	/// 子级
	/// </summary>
	public virtual ICollection<Tree<T>> Children { get; set; }
}
