using Framework.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Serilog.Enrichers;

public class InvocationContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.ContainsKey(LogProperties.SourceContext))
        {
            var sourceContext = ((ScalarValue)logEvent.Properties[LogProperties.SourceContext]).Value?.ToString();
            var callerFrame = GetCallerStackFrame(sourceContext);

            if (callerFrame != null)
            {
                var methodName = callerFrame.GetMethod()?.Name;
                var lineNumber = callerFrame.GetFileLineNumber();
                var fileName = callerFrame?.GetFileName();

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.InvocationContextClassName, sourceContext));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.InvocationContextMethodName, methodName));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.InvocationContextFilePath, fileName));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(LogProperties.InvocationContextLineNumber, lineNumber));
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