using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Mvvm.Events;


/// <summary>
/// 窗口高度变更消息。
/// 通过 <see cref="CommunityToolkit.Mvvm.Messaging.IMessenger"/> 发送，携带窗口高度的新值（以设备无关像素为单位）。
/// 接收方应在注册的 <c>Recipient</c> 中响应以更新 UI 布局或执行尺寸相关逻辑。
/// </summary>
public class HeightChange : ValueChangedMessage<double>
{
    /// <summary>
    /// 使用指定的窗口高度初始化 <see cref="HeightChange"/>。
    /// </summary>
    /// <param name="height">窗口高度（设备无关像素 DIP）。</param>
    public HeightChange(double height) : base(height)
    {
    }
}

/// <summary>
/// 窗口宽度变更消息。
/// 通过 <see cref="CommunityToolkit.Mvvm.Messaging.IMessenger"/> 发送，携带窗口宽度的新值（以设备无关像素为单位）。
/// 接收方应在注册的 <c>Recipient</c> 中响应以更新 UI 布局或执行尺寸相关逻辑。
/// </summary>
public class WidthChange : ValueChangedMessage<double>
{
    /// <summary>
    /// 使用指定的窗口宽度初始化 <see cref="WidthChange"/>。
    /// </summary>
    /// <param name="width">窗口宽度（设备无关像素 DIP）。</param>
    public WidthChange(double width) : base(width)
    {
    }
}
