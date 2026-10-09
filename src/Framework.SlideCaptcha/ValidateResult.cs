using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 滑块验证码校验的统一结果载体：包含结果枚举与中文消息，方便上层（Controller、UI）直接展示。
	/// </summary>
	public class ValidateResult
	{
		/// <summary>
		/// 校验结果状态。默认 <c>Success</c>，但通常应通过静态工厂方法（<see cref="Success"/>、<see cref="Fail"/>、<see cref="Timeout"/>）构造实例。
		/// </summary>
		public ValidateResultType Result { get; set; }

		/// <summary>
		/// 与 <see cref="Result"/> 对应的中文描述，默认值为空字符串。
		/// </summary>
		public string Message { get; set; } = string.Empty;

		/// <summary>
		/// 创建一个表示「校验通过」的结果实例（<see cref="ValidateResultType.Success"/>，消息「成功」）。
		/// </summary>
		/// <returns>代表成功的 <see cref="ValidateResult"/>。</returns>
		public static ValidateResult Success()
		{
			return new ValidateResult { Result = ValidateResultType.Success, Message = "成功" };
		}

		/// <summary>
		/// 创建一个表示「轨迹不合法」的结果实例（<see cref="ValidateResultType.ValidateFail"/>，消息「验证失败」）。
		/// </summary>
		/// <returns>代表验证失败（轨迹不合规）的 <see cref="ValidateResult"/>。</returns>
		public static ValidateResult Fail()
		{
			return new ValidateResult { Result = ValidateResultType.ValidateFail, Message = "验证失败" };
		}

		/// <summary>
		/// 创建一个表示「验证码已过期或缺失」的结果实例（<see cref="ValidateResultType.Timeout"/>，消息「验证超时」）。
		/// </summary>
		/// <returns>代表超时的 <see cref="ValidateResult"/>。</returns>
		public static ValidateResult Timeout()
		{
			return new ValidateResult { Result = ValidateResultType.Timeout, Message = "验证超时" };
		}

		/// <summary>
		/// 校验结果枚举。
		/// </summary>
		public enum ValidateResultType
		{
			/// <summary>校验通过。</summary>
			Success = 0,

			/// <summary>轨迹不合法（滑块位置、轨迹形状、时间等不通过校验）。</summary>
			ValidateFail = 1,

			/// <summary>验证码已过期或在存储中找不到对应记录。</summary>
			Timeout = 2
		}
	}
}