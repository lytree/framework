using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

public interface IValidator
{
	bool Validate(SlideTrack slideTrack, CaptchaValidateData captchaValidateData);
}
