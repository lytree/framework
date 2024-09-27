using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

public class SlideCaptchaTimeoutException : Exception
{
	public SlideCaptchaTimeoutException() : base()
	{
	}

	public SlideCaptchaTimeoutException(string message) : base(message)
	{
	}

	public SlideCaptchaTimeoutException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
