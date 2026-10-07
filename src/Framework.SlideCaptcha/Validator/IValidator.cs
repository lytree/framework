using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Validator
{
	public interface IValidator
	{
		bool Validate(SlideTrack slideTrack, CaptchaValidateData captchaValidateData);
	}
}
