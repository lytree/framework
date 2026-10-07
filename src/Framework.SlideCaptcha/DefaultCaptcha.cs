using Microsoft.Extensions.Options;
using Framework.SlideCaptcha.Exceptions;
using Framework.SlideCaptcha.Generator;
using Framework.SlideCaptcha.Storage;
using Framework.SlideCaptcha.Validator;
using System;

namespace Framework.SlideCaptcha
{
	public class DefaultCaptcha : ICaptcha
	{
		private readonly CaptchaOptions _options;
		private readonly ICaptchaImageGenerator _captchaImageGenerator;
		private readonly IValidator _validator;
		private readonly IStorage _storage;

		public DefaultCaptcha(ICaptchaImageGenerator captchaImageGenerator, IValidator validator, IStorage storage, IOptionsSnapshot<CaptchaOptions> options)
		{
			_options = options.Value;
			_captchaImageGenerator = captchaImageGenerator;
			_storage = storage;
			_validator = validator;
		}

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
