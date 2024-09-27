using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging;


/// <summary>
/// Provides extension methods for logging.
/// </summary>
public static partial class Extensions
{
    private static ILoggerFactory _loggerFactory = default!;

    /// <summary>
    /// Sets the <see cref="ILoggerFactory"/>.
    /// </summary>
    public static ILoggerFactory LoggerFactory
    {
        internal get => _loggerFactory;
        set
        {
            _loggerFactory = value;
        }
    }

    /// <summary>
    /// Creates an instance of <see cref="ILogger"/> for the specified type.
    /// </summary>
    /// <param name="forType">The type to create the logger for.</param>
    /// <returns>An instance of <see cref="ILogger"/>.</returns>
    public static ILogger Logger(this Type forType)
        => LoggerFactory.CreateLogger(forType);

    /// <summary>
    /// Creates an instance of <see cref="ILogger"/> for the given object's type.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="instance">The object instance.</param>
    /// <returns>An instance of <see cref="ILogger"/>.</returns>
    public static ILogger Logger<T>(this T instance)
    {
        
        return instance.GetType().Logger();
    }
}
