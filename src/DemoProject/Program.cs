using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoProject
{
    using OpenTracing.Mock;
    using OpenTracing.Util;

    using TracerAttributes;

    class Program
    {
        static void Main(string[] args)
        {
            GlobalTracer.Register(new MockTracer());

            Abc(Environment.CurrentDirectory);
        }

        [TraceOn(Target = TraceTarget.Private, IncludeReturnValue = true)]
        public static string Abc(string a)
        {
            return "foo";
        }
    }
}
