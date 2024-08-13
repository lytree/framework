using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Framework.SlideCaptcha;

public interface IResourceHandlerManager
{
	byte[] Handle(Resource resource);
}
