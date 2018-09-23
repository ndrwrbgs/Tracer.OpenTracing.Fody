namespace Tracer.OpenTracing
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using global::OpenTracing.Contrib.SemanticConventions;
    using global::OpenTracing.Util;
    using JetBrains.Annotations;
    using TracerAttributes;

    [PublicAPI]
    [TraceOn(TraceTarget.Public)]
    public class LoggerAdapter
    {
        /// <summary>
        /// From https://github.com/csnemes/tracer/blob/master/Tracer.Fody/Weavers/MethodWeaverBase.cs
        /// </summary>
        private static readonly string ExceptionMarker = "$exception";

        private readonly string name;

        public LoggerAdapter(Type containingType)
        {
            this.name = containingType != null ? containingType.Name + "." : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TraceEnter(
            string methodInfo,
            string[] paramNames,
            object[] paramValues)
        {
            if (!GlobalTracer.IsRegistered())
            {
                Trace.TraceError(Error.TracerNotRegisteredOnEnter);
                return;
            }

            GlobalTracer.Instance
                .BuildSpan($"{this.name}{methodInfo}")
                .StartActive();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TraceLeave(
            string methodInfo,
            long startTicks,
            long endTicks,
            string[] paramNames,
            object[] paramValues)
        {
            // TODO: This is dangerous, but only if folks misused tracing anyway. It will obscure their bugs, but not cause its own
            var activeScope = GlobalTracer.Instance
                .ScopeManager
                .Active;

            if (paramNames != null)
            {
                int i = 0;
                for (; i < paramNames.Length; i++)
                {
                    if (string.Equals(ExceptionMarker, paramNames[i]))
                    {
                        break;
                    }
                }

                if (i < paramNames.Length)
                {
                    // Found match
                    var exception = paramValues[i] as Exception;
                    activeScope.Span
                        .LogError(exception);
                }
            }

            activeScope.Dispose();
        }
    }

    internal static class Error
    {
        public static readonly string TracerNotRegisteredOnEnter =
            $"Attempted to trace a {nameof(LoggerAdapter.TraceEnter)} operation with {typeof(LoggerAdapter).FullName} before {nameof(GlobalTracer.Register)} was called.";
    }
}