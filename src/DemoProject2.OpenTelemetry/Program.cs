using System;

namespace DemoProject
{
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using TracerAttributes;

    class Program
    {
        static void Main(string[] args)
        {
            // We just AddSource and it connects, right? What do we do with the result?
            _ = Sdk.CreateTracerProviderBuilder()
                .AddSource("Tracer.OpenTelemetry.Fody")
                .AddConsoleExporter()
                .Build();

            Abc(Environment.CurrentDirectory);
        }

        [TraceOn(Target = TraceTarget.Private, IncludeReturnValue = true, IncludeArguments = true)]
        public static string Abc(string a)
        {
            return "foo";
        }
    }
}
