# YLFramework.Mvvm

基于 [`CommunityToolkit.Mvvm`](https://github.com/CommunityToolkit/dotnet) 的窗口状态消息与一些轻量帮助类，用于在 WPF / WinUI 等 MVVM 场景下解耦跨窗口通信。

## 功能概览

| 文件 | 命名空间 | 作用 |
|------|---------|------|
| `Constant/WindowsVariable.cs` | `Framework.Mvvm.Constant` | 集中存放窗口共享变量名常量，避免散落字符串字面量 |
| `Events/WindowsChange.cs` | `Framework.Mvvm.Events` | 窗口切换消息：发布 / 订阅时携带源窗口与目标窗口引用，常用于多窗口互斥或导航状态广播 |
| `Events/WindowsLoading.cs` | `Framework.Mvvm.Events` | 窗口加载完成消息：可用于在首个窗口渲染完成后再触发异步加载或解锁 UI |

## 消息发送

```csharp
WeakReferenceMessenger.Default.Send(new WindowsChangeMessage(fromWindow, toWindow));
```

## 消息接收

```csharp
WeakReferenceMessenger.Default.Register<WindowsChangeMessage>(this, (r, m) =>
{
    // 关闭窗口、广播导航事件等
});
```

## 适用场景

- 多窗口互斥（只允许一个"主操作窗口"可见）
- 启动窗口 → 主窗口 的状态广播
- 全局 Loading 状态聚合
- 跨 ViewModel 的轻量解耦（不依赖 `Prism` / `Caliburn.Micro`）

## 依赖

- `CommunityToolkit.Mvvm` 8.x（提供 `ObservableObject` / `WeakReferenceMessenger`）

## 版本

`1.0.0`，遵循 GitVersion。
