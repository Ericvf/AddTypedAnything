using System;
using System.IO;
using System.Threading.Tasks;
using DependencyInjection.Extensions.Parameterization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConsoleApp1
{
    public interface IService { }
    public class ServiceA : IService { }
    public class ServiceB : IService { }
    public class ConfigSetting {
        public string Name { get; set; }
    }

    public class App
    {
        private readonly IService serviceA;
        private readonly IService serviceB;
        private readonly string input;
        private readonly IOptions<ConfigSetting> options;
        private readonly IOptions<ConfigSetting> options2;

        public App(IService serviceA, IService serviceB, string input, IOptions<ConfigSetting> options)
        {
            this.serviceA = serviceA;
            this.serviceB = serviceB;
            this.input = input;
            this.options = options;
        }

        public Task RunAsync(string[] args)
        {
            Console.WriteLine($"ServiceA: {serviceA}");
            Console.WriteLine($"ServiceB: {serviceB}");
            Console.WriteLine($"input: {input}");
            if (options != null)
            {
                Console.WriteLine($"options: {options.Value.Name}");
            }
            return Task.CompletedTask;
        }
    }

    public class Program
    {
        private static ServiceProvider BuildServiceProvider()
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            return new ServiceCollection()
                .AddSingleton(configurationRoot) // Optional registration, only used by `.Options<>`
                .AddSingleton<ServiceA>()
                .AddSingleton<ServiceB>()

                .AddSingleton<App>(pb => pb
                    .Type<ServiceA>()
                    .Type<ServiceB>()
                    .Options<ConfigSetting>("ConfigSetting2")
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