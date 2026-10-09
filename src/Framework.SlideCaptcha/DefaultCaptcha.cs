using Microsoft.Extensions.Options;
using Framework.SlideCaptcha.Exceptions;
using Framework.SlideCaptcha.Generator;
using Framework.SlideCaptcha.Storage;
using Framework.SlideCaptcha.Validator;
using System;

namespace Framework.SlideCaptcha
{
	/// <summary>
	/// 滑块验证码的默认实现：负责生成验证码数据（背景、滑块、校验信息）以及校验用户提交的轨迹。
	/// </summary>
	/// <remarks>
	/// 通过 <see cref="ICaptchaImageGenerator"/> 产出图像与缺口位置，
	/// 将 <see cref="CaptchaValidateData"/>（含位置百分比与容差）写入 <see cref="IStorage"/> 并设置过期时间。
	/// 验证时从存储取出数据，调用 <see cref="IValidator"/> 比对轨迹；无论成败都会在 finally 中消费掉 <paramref name="captchaId"/>，
	/// 保证验证码一次性使用，避免重放。
	/// </remarks>
	public class DefaultCaptcha : ICaptcha
	{
		private readonly CaptchaOptions _options;
		private readonly ICaptchaImageGenerator _captchaImageGenerator;
		private readonly IValidator _validator;
		private readonly IStorage _storage;

		/// <summary>
		/// 使用默认的图像生成器、校验器与分布式存储初始化 <see cref="DefaultCaptcha"/>。
		/// </summary>
		/// <param name="captchaImageGenerator">生成背景图、滑块图与缺口位置的图像生成器。</param>
		/// <param name="validator">对用户轨迹进行形状/位置校验的校验器。</param>
		/// <param name="storage">用于保存和读取 <see cref="CaptchaValidateData"/> 的存储实现。</param>
		/// <param name="options">滑块验证码配置快照（容差、过期秒数等）；每次创建从 <see cref="IOptionsSnapshot{T}"/> 取当前值。</param>
		public DefaultCaptcha(ICaptchaImageGenerator captchaImageGenerator, IValidator validator, IStorage storage, IOptionsSnapshot<CaptchaOptions> options)
		{
			_options = options.Value;
			_captchaImageGenerator = captchaImageGenerator;
			_storage = storage;
			_validator = validator;
		}

		/// <summary>
		/// 生成一张滑块验证码，返回供前端展示的数据以及用于提交验证的 <c>captchaId</c>。
		/// </summary>
		/// <param name="captchaId">业务侧指定的验证码标识；为 <c>null</c>、空或纯空白时由内部生成 <see cref="Guid.NewGuid"/>。</param>
		/// <returns>包含 <c>captchaId</c>、背景图与滑块图 Base64 的 <see cref="CaptchaData"/>。</returns>
		public CaptchaData Generate(string? captchaId = null)
		{
			captchaId = string.IsNullOrWhiteSpace(captchaId) ? Guid.NewGuid().ToString() : captchaId;
			var captchImageInfo = _captchaImageGenerator.Generate();
			captchImageInfo.Check();

			var captchaValidateData = new CaptchaValidateData(captchImageInfo.Percent, _options.Tolerant);

			// 原实现用 DateTime.Now.AddSeconds(...).ToUniversalTime() 得到一个 Kind=Utc 的 DateTime，
			// 再隐式转成 DateTimeOffset 时会把它当作 UTC 瞬间——虽然当前偏移为 0 时结果碰巧正确，
			// 但依赖隐式转换的时区语义十分脆弱。直接用 DateTimeOffset 表达绝对时刻。
			var expiresAt = DateTimeOffset.UtcNow.AddSeconds(_options.ExpirySeconds);

			_storage.Set(captchaId, captchaValidateData, expiresAt);

			return new CaptchaData(captchaId, captchImageInfo.BackgroundImageBase64, captchImageInfo.SliderImageBase64);
		}

		/// <summary>
		/// 校验用户提交的滑动轨迹：在存储中找到对应的 <see cref="CaptchaValidateData"/> 后委托 <see cref="IValidator"/> 比对。
		/// 校验完成（无论成功失败或异常）后都会从存储中移除该 <paramref name="captchaId"/>，确保一次性使用。
		/// </summary>
		/// <param name="captchaId">生成验证码时返回的标识；为空或纯空白视为非法，直接返回 <see cref="ValidateResult.Fail"/>。</param>
		/// <param name="slideTrack">用户前端提交的滑动轨迹数据。</param>
		/// <returns>一个 <see cref="ValidateResult"/>，分别表示成功、轨迹不合法或存储已过期（超时）。</returns>
		public ValidateResult Validate(string captchaId, SlideTrack slideTrack)
		{
			// 空 captchaId 会让缓存键退化为「前缀本身」，误删/误读其他条目
			if (string.IsNullOrWhiteSpace(captchaId))
			{
				return ValidateResult.Fail();
			}

			try
			{
				var captchaValidateData = _storage.Get<CaptchaValidateData>(captchaId);
				if (captchaValidateData == null) return ValidateResult.Timeout();

				var success = _validator.Validate(slideTrack, captchaValidateData);
				return success ? ValidateResult.Success() : ValidateResult.Fail();
			}
			catch (SlideCaptchaException)
			{
				// 轨迹数据非法属于「验证不通过」，不应向上冒泡成 500
				return ValidateResult.Fail();
			}
			finally
			{
				// 无论成败都消费掉该 captchaId，保证一次性使用
				_storage.Remove(captchaId);
			}
		}
	}
}
