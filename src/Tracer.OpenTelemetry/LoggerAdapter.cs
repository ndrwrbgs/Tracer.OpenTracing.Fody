namespace Tracer.OpenTelemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using global::OpenTelemetry.Trace;
    using JetBrains.Annotations;
    using TracerAttributes;
    using Util;

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

        private static readonly ActivitySource ActivityTracer = new ActivitySource("Tracer.OpenTelemetry.Fody", "1.0.0");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TraceEnter(
            string methodInfo,
            Tuple<string, string>[] configParameters,
            string[] paramNames,
            object[] paramValues)
        {
            if (ActivityTracer.HasListeners())
            {
                var activity = ActivityTracer.StartActivity($"{this.name}{methodInfo}");

                if (ShouldIncludeArguments(configParameters))
                {
                    for (int paramIndex = 0; paramIndex < paramNames.Length; paramIndex++)
                    {
                        string paramName = paramNames[paramIndex];
                        object paramValue = paramValues[paramIndex];
                        // TODO: Support other forms of serialization
                        string serializedParamValue = paramValue?.ToString();

                        // TODO: This supports 'object' directly, could remove serialization at this level, depends how well documented adding serialization at any other level is
                        activity.AddTag(paramName, serializedParamValue);
                    }
                }
            }
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
            var activeScope = Activity.Current;

            // Since we allow silently passing TraceEnter if the GlobalTracer isn't registered, we would need to support Leaving those spans too
            // (e.g. after GlobalTracer registration from a nested method)
            if (activeScope == null)
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
                    
                    activeScope.RecordException(exception);
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

                            activeScope.AddEvent(
                                new ActivityEvent(
                                    "ReturnValue",
                                    tags: new ActivityTagsCollection(
                                        new[]
                                        {
                                            new KeyValuePair<string, object>("ReturnValue", returnValue)
                                        })));
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
        public static readonly string NoActiveSpanOnLeave =
            $"Attempted to trace a {nameof(LoggerAdapter.TraceLeave)} operation with {typeof(LoggerAdapter).FullName} but {nameof(Activity)}.{nameof(Activity.Current)} was null.";
    }
}