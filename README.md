[![NuGet][nuget-img]][nuget]

# Tracer.OpenTracing.Fody
[Fody](https://github.com/Fody/Fody) plugin based off of [tracer](https://github.com/csnemes/tracer) for instrumenting code with [OpenTracing](http://opentracing.io)

## Functionality
After setup, you can configure application wide tracing with a simple FodyWeavers.xml file, or you can individually select methods and
classes to trace with the included [TraceOn] and [NoTrace] attributes.

It can reduce your code as follows
```diff
+[TraceOn(TraceTarget.Public)]
 class Class
 {
   public void Method1()
   {
-    using (GlobalTracer.Instance
-      .BuildSpan(nameof(Class) + "." + nameof(Method1))
-      .StartActive())
-    {
       // Do stuff
       this.MethodImpl();
-    }
   }

   public void Method2()
   {
-    using (GlobalTracer.Instance
-      .BuildSpan(nameof(Class) + "." + nameof(Method2))
-      .StartActive())
-    {
       // Do stuff
       this.MethodImpl();
-    }
   }
   
   private void MethodImpl()
   {
     // Do stuff
   }
 }
```

## Setup
1. Make sure your startup logic is configuring `GlobalTracer.Instance` via [`GlobalTracer.Register`](https://github.com/opentracing/opentracing-csharp/blob/d00349731545c04c989ba138f12e402cbe902208/src/OpenTracing/Util/GlobalTracer.cs#L74). 
If you don't have one ready presently, you can use [`EventHookTracer`](https://www.nuget.org/packages/OpenTracing.Contrib.EventHookTracer/)
as follows.

      ### EventHookTracer example
      ```C#
      var eventHookTracer = new EventHookTracer(new MockTracer());
      eventHookTracer.SpanActivated += (sender, eventArgs) => Console.WriteLine("+" + eventArgs.OperationName);
      eventHookTracer.SpanFinished += (sender, eventArgs) => Console.WriteLine("-" + eventArgs.OperationName);
      GlobalTracer.Register(eventHookTracer);
      ```
1. Import [Tracer.OpenTracing.Fody](https://www.nuget.org/packages/Tracer.OpenTracing.Fody/) from nuget

   _This will import Tracer.Fody and Tracer, which are needed to manipulate the assembly with tracing instrumentation._
1. Configure Fody to use this tracer
  Add a FodyWeavers.xml file with the following contents. (_This file does not need CopyToOutputDirectory_)
    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <Weavers>   
      <Tracer adapterAssembly="Tracer.OpenTracing" 
              logManager="Tracer.OpenTracing.LogManagerAdapter" 
              logger="Tracer.OpenTracing.LoggerAdapter" >
      </Tracer>
    </Weavers>
    ```
1. Configure code to be traced
   Use a combination of the FodyWeavers.xml configuration and Attributes on Classes, Assembly, or individual Methods to signal what elements
   should be traced. See [tracer/basics wiki](https://github.com/csnemes/tracer/wiki/Basics) for details.
   
      ### Example 1 - FodyWeavers.xml
      ```xml
      <?xml version="1.0" encoding="utf-8" ?>
      <Weavers>   
        <Tracer adapterAssembly="Tracer.OpenTracing" 
                logManager="Tracer.OpenTracing.LogManagerAdapter" 
                logger="Tracer.OpenTracing.LoggerAdapter" >
          <!-- Trace all methods on public classes -->
          <!-- Avoid tracing the startup method - e.g. don't trace before you've configured the GlobalTracer.Instance
               so in this case, make sure Main and SetupTracer are marked with [NoTrace] -->
          <TraceOn class="public" method="all" />
        </Tracer>
      </Weavers>
      ```
      ### Example 2 - MyClass.cs
      ```C#
      [TraceOn]
      class Foo
      {
          [NoTrace]
          public static void A()
          {
              B();
              B();
          }

          public static void B()
          { // BuildSpan().StartActive() is injected on this line
          } // ActiveSpan.Finish() is injected on this line
      }
      ```

  [nuget-img]: https://img.shields.io/nuget/v/Tracer.OpenTracing.Fody.svg
  [nuget]: https://www.nuget.org/packages/Tracer.OpenTracing.Fody
