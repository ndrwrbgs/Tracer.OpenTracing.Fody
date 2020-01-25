namespace Tracer.OpenTracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    using global::OpenTracing;
    using global::OpenTracing.Contrib.SemanticConventions;
    using global::OpenTracing.Util;
    using JetBrains.Annotations;

    using Tracer.OpenTracing.Util;

    using TracerAttributes;

    [PublicAPI]
    public class LoggerAdapter
    {
        /// <summary>
        /// From https://github.com/csnemes/tracer/blob/master/Tracer.Fody/Weavers/MethodWeaverBase.cs
        /// </summary>
        private static readonly string ExceptionMarker = "$exception";
        private static readonly string ReturnValueMarker = null;

        private readonly string name;

        private static bool loggedTraceEnterError;
        private static bool loggedTraceLeaveError;

        public LoggerAdapter(Type containingType)
        {
            this.name = containingType != null ? containingType.Name + "." : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TraceEnter(
            string methodInfo,
            Tuple<string, string>[] configParameters,
            string[] paramNames,
            object[] paramValues)
        {
            if (!GlobalTracer.IsRegistered())
            {
                // Not locking on this check, it's okay to log a few times
                if (!loggedTraceEnterError)
                {
                    loggedTraceEnterError = true;
                    Trace.TraceError(Error.TracerNotRegisteredOnEnter);
                }

                return;
            }

            ISpanBuilder spanBuilder = GlobalTracer.Instance
                .BuildSpan($"{this.name}{methodInfo}");

            // Add arguments (if configured to)
            {
                if (ShouldIncludeArguments(configParameters))
                {
                    for (int paramIndex = 0; paramIndex < paramNames.Length; paramIndex++)
                    {
                        string paramName = paramNames[paramIndex];
                        object paramValue = paramValues[paramIndex];
                        // TODO: Support other forms of serialization
                        string serializedParamValue = paramValue?.ToString();

                        spanBuilder = spanBuilder
                            .WithTag(paramName, serializedParamValue);
                    }
                }
            }

            spanBuilder
                .StartActive();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TraceLeave(
            string methodInfo,
            Tuple<string, string>[] configParameters,
            long startTicks,
            long endTicks,
            string[] paramNames,
            object[] paramValues)
        {
            // TODO: This is dangerous, but only if folks misused tracing anyway. It will obscure their bugs, but not cause its own
            var activeScope = GlobalTracer.Instance
                .ScopeManager
                .Active;

            // Since we allow silently passing TraceEnter if the GlobalTracer isn't registered, we would need to support Leaving those spans too
            // (e.g. after GlobalTracer registration from a nested method)
            if (activeScope?.Span == null)
            {
                // Not locking on this check, it's okay to log a few times
                if (!loggedTraceLeaveError)
                {
                    loggedTraceLeaveError = true;
                    Trace.TraceError(Error.NoActiveSpanOnLeave);
                }

                return;
            }

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
            
            // Add Return Value (if configured to)
            {
                if (ShouldIncludeReturnValue(configParameters))
                {
                    if (paramNames != null)
                    {
                        int i = 0;
                        for (; i < paramNames.Length; i++)
                        {
                            if (string.Equals(ReturnValueMarker, paramNames[i]))
                            {
                                break;
                            }
                        }

                        if (i < paramNames.Length)
                        {
                            // Found match
                            var returnValue = paramValues[i];
                            activeScope.Span
                                .Log(
                                    new KeyValuePair<string, object>[]
                                    {
                                        new KeyValuePair<string, object>("ReturnValue", returnValue)
                                    });
                        }
                    }
                }
            }

            activeScope.Dispose();
        }

        private static bool ShouldIncludeArguments(Tuple<string, string>[] configParameters)
        {
            var includeArgumentsParameter = configParameters
                ?.FirstOrDefault(tup => tup.Item1 == nameof(TraceOn.IncludeArguments))
                ?.Item2;
            var includeArguments = includeArgumentsParameter == null ? false : bool.Parse(includeArgumentsParameter);
            return includeArguments;
        }

        private static bool ShouldIncludeReturnValue(Tuple<string, string>[] configParameters)
        {
            var includeReturnValueParameter = configParameters
                ?.FirstOrDefault(tup => tup.Item1 == nameof(TraceOn.IncludeReturnValue))
                ?.Item2;
            var includeReturnValue = includeReturnValueParameter == null ? false : bool.Parse(includeReturnValueParameter);
            return includeReturnValue;
        }
    }

    internal static class Error
    {
        public static readonly string TracerNotRegisteredOnEnter =
            $"Attempted to trace a {nameof(LoggerAdapter.TraceEnter)} operation with {typeof(LoggerAdapter).FullName} before {nameof(GlobalTracer.Register)} was called.";

        public static readonly string NoActiveSpanOnLeave =
            $"Attempted to trace a {nameof(LoggerAdapter.TraceLeave)} operation with {typeof(LoggerAdapter).FullName} but {nameof(GlobalTracer)}.{nameof(GlobalTracer.Instance)}.{nameof(GlobalTracer.Instance.ScopeManager)}.{nameof(GlobalTracer.Instance.ScopeManager.Active)}.{nameof(GlobalTracer.Instance.ScopeManager.Active.Span)} was null.";
    }
}