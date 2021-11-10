// ReSharper disable once CheckNamespace - Required by Tracer.Fody
namespace TracerAttributes
{
    using System;

    /// <summary>
    /// Namespace and type are required for projects of Tracer.Fody.
    /// TODO: Suggest an .Interfaces style import for Tracer.Fody similar to ArgValidation.Fody
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class TraceOn : Attribute
    {
        public TraceTarget Target { get; set; }

        public bool IncludeArguments { get; set; }

        public bool IncludeReturnValue { get; set; }

        public TraceOn()
        {
        }

        public TraceOn(TraceTarget traceTarget)
        {
            Target = traceTarget;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
    public class NoTrace : Attribute
    {
    }

    public enum TraceTarget
    {
        Public,
        Internal,
        Protected,
        Private
    }
}