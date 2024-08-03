using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Buffers.Internal;

/// <summary>Helper class for constants for inlining methods</summary>
public static class InlineMethod
{
    /// <summary>Value for lining method</summary>
    public const MethodImplOptions AggressiveInlining = MethodImplOptions.AggressiveInlining;

    /// <summary>Value for lining method</summary>
    public const MethodImplOptions AggressiveOptimization = MethodImplOptions.AggressiveInlining;

}