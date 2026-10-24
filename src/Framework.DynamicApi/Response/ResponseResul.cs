namespace Framework.DynamicApi.Response;

/// <summary>
/// 动态 API 的统一响应包装类型。
/// </summary>
/// <typeparam name="T">业务负载类型。</typeparam>
public class ResponseResul<T>
{
	/// <summary>
	/// 创建一个空的响应包装，供需要先构造、后赋值的场景使用。
	/// </summary>
	public ResponseResul()
	{
	}

	/// <summary>
	/// 创建带有业务负载的响应包装。
	/// </summary>
	/// <param name="data">业务负载。</param>
	/// <param name="success">是否成功，默认 <c>true</c>。</param>
	/// <param name="code">业务编码，可为 <c>null</c>。</param>
	/// <param name="msg">业务消息，可为 <c>null</c>。</param>
	public ResponseResul(T data, bool success = true, string code = null, string msg = null)
	{
		Data = data;
		Success = success;
		Code = code;
		Msg = msg;
	}

	/// <summary>
	/// 是否成功标记
	/// </summary>
	public bool Success { get; private set; }

	/// <summary>
	/// 编码
	/// </summary>
	public string Code { get; set; }

	/// <summary>
	/// 消息
	/// </summary>
	public string Msg { get; private set; }

	/// <summary>
	/// 数据
	/// </summary>
	public T Data { get; private set; }
}
