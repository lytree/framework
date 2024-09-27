using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Logging;

internal static class LogProperties
{
    public static readonly string SourceContext = "SourceContext";

    public static readonly string InvocationContextClassName = "ClassName";

    public static readonly string InvocationContextMethodName = "MethodName";

    public static readonly string InvocationContextFilePath = "FilePath";

    public static readonly string InvocationContextLineNumber = "LineNumber";

}
