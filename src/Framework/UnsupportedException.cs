using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System;

/// <summary>
/// 表示当前调用上下文不支持目标操作或功能时抛出的异常。
/// </summary>
/// <remarks>
/// 与 <see cref="System.NotSupportedException"/> 语义相近，但属于本框架自定义异常类型，便于在框架调用栈中识别"上层明确不支持"的语义（例如某些环境或配置下被禁用）。未携带额外消息或错误码，调用方可根据类型判定后自行包装上下文信息。
/// </remarks>
public class UnsupportedException : Exception
{
}
