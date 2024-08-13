using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha;

public class SlideCaptchaException : Exception
{
	public SlideCaptchaException() : base()
	{
	}

	public SlideCaptchaException(string message) : base(message)
	{
	}

	public SlideCaptchaException(string message, Exception innerException) : base(message, innerException)
	{
	}
}
