# DependencyInjection.Extensions.Parameterization

This package allows you to register parameters for Microsoft.Extensions.DependencyInjection.
It works like this:

```csharp
// Register
.AddSingleton<App>(pb => pb
    .Type<ServiceA>()
    .Type<ServiceB>()
    .Value("inject parameter"))
  
// Usage
public App(IService serviceA, IService serviceB, string input)
{
    serviceA // ServiceA
    serviceB // ServiceB
    input // inject parameter
}
```

It supports `Singleton`, `Scoped` and `Transient` lifetime scopes.


Here is a full example:
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DependencyInjection.Extensions.Parameterization;

namespace ConsoleApp1
{
    public interface IService { }
    public class ServiceA : IService { }
    public class ServiceB : IService { }

    public class App
    {
        private readonly IService serviceA;
        private readonly IService serviceB;
        private readonly string input;

        public App(IService serviceA, IService serviceB, string input)
        {
            this.serviceA = serviceA;
            this.serviceB = serviceB;
            this.input = input;
        }

        public Task RunAsync(string[] args)
        {
            Console.WriteLine($"ServiceA: {serviceA}");
            Console.WriteLine($"ServiceB: {serviceB}");
            Console.WriteLine($"input: {input}");
            return Task.CompletedTask;
        }
    }

    public class Program
    {
        private static ServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
                .AddSingleton<ServiceA>()
                .AddSingleton<ServiceB>()

            .AddSingleton<App>(pb => pb
                .Type<ServiceA>()
                .Type<ServiceB>()
                .Value("inject parameter"))

                .BuildServiceProvider();
        }

        static async Task Main(string[] args)
        {
            var serviceProvider = BuildServiceProvider();
            var application = serviceProvider.GetRequiredService<App>();
            await application.RunAsync(args);
        }
    }
}
```
