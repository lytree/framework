using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Mvvm.Events;

/// <summary>
/// 窗口加载状态变更消息。
/// 通过 <see cref="CommunityToolkit.Mvvm.Messaging.IMessenger"/> 发送，携带当前是否处于加载状态的布尔值。
/// 在执行异步加载操作（如分页/列表刷新）开始与结束时分别发送 <c>true</c>/<c>false</c>，由订阅方更新加载动画或遮罩层 UI。
/// </summary>
public class WindowsLoading : ValueChangedMessage<bool>
{
	/// <summary>
	/// 使用指定的加载状态初始化 <see cref="WindowsLoading"/>。
	/// </summary>
	/// <param name="loading">是否处于加载中。<c>true</c> 表示进入加载状态，<c>false</c> 表示加载结束。</param>
	public WindowsLoading(bool loading) : base(loading)
	{

	}
}
