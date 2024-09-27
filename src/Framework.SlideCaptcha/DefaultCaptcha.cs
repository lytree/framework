using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

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
		_storage.Set(captchaId, captchaValidateData, DateTime.Now.AddSeconds(_options.ExpirySeconds).ToUniversalTime());

		return new CaptchaData(captchaId, captchImageInfo.BackgroundImageBase64, captchImageInfo.SliderImageBase64);
	}

	public ValidateResult Validate(string captchaId, SlideTrack slideTrack)
	{
		try
		{
			var captchaValidateData = _storage.Get<CaptchaValidateData>(captchaId);
			if (captchaValidateData == null) return ValidateResult.Timeout();
			var success = _validator.Validate(slideTrack, captchaValidateData);
			return success ? ValidateResult.Success() : ValidateResult.Fail();
		}
		finally
		{
			_storage.Remove(captchaId);
		}
	}
}
