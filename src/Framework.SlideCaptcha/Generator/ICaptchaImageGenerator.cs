using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

public interface ICaptchaImageGenerator
{
	CaptchaImageData Generate();
}
