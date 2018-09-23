namespace Tracer.OpenTracing
{
    using System;
    using System.Diagnostics;
    using global::OpenTracing.Util;
    using JetBrains.Annotations;

    [PublicAPI]
    public class LoggerAdapter
    {
        private readonly string name;

        public LoggerAdapter(Type containingType)
        {
            this.name = containingType != null ? containingType.Name + "." : null;
        }

        public void TraceEnter(
            string methodInfo,
            string[] paramNames,
            object[] paramValues)
        {
            if (!GlobalTracer.IsRegistered())
            {
                Trace.TraceError($"Attempted to trace a {nameof(this.TraceEnter)} operation with {typeof(LoggerAdapter).FullName} before {nameof(GlobalTracer.Register)} was called.");
                return;
            }

            GlobalTracer.Instance
                .BuildSpan($"{this.name}{methodInfo}")
                .StartActive();
        }

        public void TraceLeave(
            string methodInfo,
            long startTicks,
            long endTicks,
            string[] paramNames,
            object[] paramValues)
        {
            // TODO: This is dangerous, but only if folks misused tracing anyway. It will obscure their bugs, but not cause its own
            GlobalTracer.Instance
                .ScopeManager
                .Active
                .Dispose();
        }
    }
}