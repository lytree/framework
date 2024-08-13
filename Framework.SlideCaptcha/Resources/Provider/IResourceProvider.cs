using System;
using System.Collections.Generic;
using System.Text;

namespace Framework.SlideCaptcha.Resources.Provider
{
	public interface IResourceProvider
	{
		List<Resource> Backgrounds();
		List<TemplatePair> Templates();
	}
}
