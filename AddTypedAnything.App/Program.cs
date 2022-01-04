using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AddTypedAnything
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = BuildServiceProvider();
            var application = serviceProvider.GetRequiredService<App>();
            await application.RunAsync(args);
        }

        private static ServiceProvider BuildServiceProvider()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("appsettings.json", optional: false);

            var configurationRoot = builder.Build();

            var configSetting2 = new ConfigSetting();
            configurationRoot.Bind("ConfigSetting2", configSetting2);

            return new ServiceCollection()
                .Configure<ConfigSetting>(configurationRoot.GetSection(key: nameof(ConfigSetting)))
                .AddSingleton<App>()
                .AddSingleton<TestServiceA>()
                .AddSingleton<TestServiceB>()
                .AddSingleton<TestServiceC>()
                .AddSingleton<ITest, TestServiceC>()
                .AddSingleton<SubstituteWithServiceA>(pb => pb.Type<TestServiceA>())
                .AddSingleton<SubstituteWithServiceB>(pb => pb.Type<TestServiceB>())
                .AddSingleton<IServiceBase, SubstituteWithServiceAAndOptions>(pb => pb
                    .Value("Injected string")
                    .Value(Options.Create(configSetting2))
                    .Type<TestServiceA>())
                .BuildServiceProvider();
        }
    }

    public class ConfigSetting
    {
        public string Name { get; set; }

    }

    public class TestServiceA : ITest
    {
        public TestServiceA(IOptions<ConfigSetting> bla)
        {
            Console.WriteLine("TestServiceA: " + GetHashCode());
        }
    }

    public class TestServiceB : ITest
    {
        public TestServiceB()
        {
            Console.WriteLine("TestServiceB: " + GetHashCode());
        }
    }

    public class TestServiceC : ITest
    {
        public TestServiceC()
        {
            Console.WriteLine("TestServiceC: " + GetHashCode());
        }
    }

    public interface ITest
    {
    }

    public class SubstituteWithServiceB
    {
        public SubstituteWithServiceB(ITest test)
        {
            Console.WriteLine("SubstituteWithServiceB:" + test);
        }
    }

    public class SubstituteWithServiceA
    {
        public SubstituteWithServiceA(ITest test)
        {
            Console.WriteLine("SubstituteWithServiceA:" + test);
        }
    }

    public class SubstituteWithServiceC : IServiceBase
    {
        public SubstituteWithServiceC(ITest test)
        {
            Console.WriteLine("SubstituteWithServiceC:" + test);
        }
    }
    public class SubstituteWithServiceAAndOptions : IServiceBase
    {
        public SubstituteWithServiceAAndOptions(ITest test, string input, IOptions<ConfigSetting> configSettings2)
        {
            Console.WriteLine("SubstituteWithServiceAAndOptions:" + test);
        }
    }
    public interface IServiceBase
    {
    }

    public class App
    {
        public App(ITest test, SubstituteWithServiceA substituteWithServiceA, SubstituteWithServiceB substituteWithServiceB, IServiceBase serviceBase, SubstituteWithServiceB substituteWithServiceB2)
        {
        }

        public async Task RunAsync(string[] args)
        {
            await Task.Delay(0);
        }
    }
}
