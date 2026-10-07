using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Generator
{
	public interface ICaptchaImageGenerator
	{
		CaptchaImageData Generate();
	}
}
