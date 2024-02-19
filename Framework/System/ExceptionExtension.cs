using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.System;

public static class ExceptionExtension
{
	/// <summary>
	///     Gets the entire stack trace consisting of exception's footprints (File, Method, LineNumber)
	/// </summary>
	/// <param name="exception">Source <see cref="Exception" /></param>
	/// <returns>
	///     <see cref="string" /> that represents the entire stack trace consisting of exception's footprints (File,
	///     Method, LineNumber)
	/// </returns>
	public static string GetExceptionFootprints(this Exception exception)
	{
		StackTrace stackTrace = new(exception, true);
		StackFrame[] frames = stackTrace.GetFrames();

		if (frames is null)
		{
			return string.Empty;
		}

		var traceStringBuilder = new StringBuilder();

		for (var i = 0; i < frames.Length; i++)
		{
			StackFrame frame = frames[i];

			if (frame.GetFileLineNumber() < 1)
				continue;

			traceStringBuilder.AppendLine($"File: {frame.GetFileName()}");
			traceStringBuilder.AppendLine($"Method: {frame.GetMethod()?.Name}");
			traceStringBuilder.AppendLine($"LineNumber: {frame.GetFileLineNumber()}");

			if (i == frames.Length - 1)
				break;

			traceStringBuilder.AppendLine(" ---> ");
		}

		string stackTraceFootprints = traceStringBuilder.ToString();

		if (string.IsNullOrWhiteSpace(stackTraceFootprints))
			return "NO DETECTED FOOTPRINTS";

		return stackTraceFootprints;
	}

}
