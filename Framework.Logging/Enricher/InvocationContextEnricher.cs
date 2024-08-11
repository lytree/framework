using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Logging.Enricher;

public class InvocationContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.ContainsKey(Constants.LogProperties.SourceContext))
        {
            var sourceContext = ((ScalarValue)logEvent.Properties[Constants.LogProperties.SourceContext]).Value?.ToString();
            var callerFrame = GetCallerStackFrame(sourceContext);

            if (callerFrame != null)
            {
                var methodName = callerFrame.GetMethod()?.Name;
                var lineNumber = callerFrame.GetFileLineNumber();
                var fileName = callerFrame?.GetFileName();

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(Constants.LogProperties.InvocationContextClassName, sourceContext));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(Constants.LogProperties.InvocationContextMethodName, methodName));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(Constants.LogProperties.InvocationContextFilePath, fileName));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(Constants.LogProperties.InvocationContextLineNumber, lineNumber));
            }
        }
    }

    private StackFrame? GetCallerStackFrame(string className)
    {
        var trace = new StackTrace(true);
        var frames = trace.GetFrames();

        var callerFrame = frames.FirstOrDefault(f => f.GetMethod()?.DeclaringType?.FullName == className);

        return callerFrame;
    }
}