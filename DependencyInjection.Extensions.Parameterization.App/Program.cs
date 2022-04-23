using System;
using System.IO;
using System.Threading.Tasks;
using DependencyInjection.Extensions.Parameterization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DependencyInjection.Extensions.Parameterization.App
{
    public interface IService { }
    public class ServiceA : IService { }
    public class ServiceB : IService { }
    public class ConfigSetting
    {
        public string Name { get; set; }
    }

    public interface IDatabaseClient
    {
    }

    public class DatabaseClient : IDatabaseClient
    {
        public DatabaseClient(string clientName)
        {
            Console.WriteLine("DatabaseClient:" + clientName);
        }
    }

    public interface ICustomerRepo
    {
    }

    public class CustomerRepo : ICustomerRepo
    {
        public CustomerRepo(IDatabaseClient client, string parameter)
        {
            Console.WriteLine("Customerrepo: " + parameter);
        }
    }

    public interface IDataMigrationService { }
    public class DataMigrationService : IDataMigrationService {

        public DataMigrationService(ICustomerRepo source, ICustomerRepo destination)
        {

        }
    }

    public class App
    {
        private readonly IService serviceA;
        private readonly IService serviceB;
        private readonly string input;
        private readonly IOptions<ConfigSetting> options;
        private readonly IDataMigrationService dataMigrationService;

        public App(IService serviceA, IService serviceB, string input, IOptions<ConfigSetting> options, IDataMigrationService dataMigrationService)
        {
            this.serviceA = serviceA;
            this.serviceB = serviceB;
            this.input = input;
            this.options = options;
            this.dataMigrationService = dataMigrationService;
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
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            return new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration) // Optional registration, only used by `.Options<>`
                .AddSingleton<ServiceA>()
                .AddSingleton<ServiceB>()
                .AddSingleton<IDatabaseClient, DatabaseClient>()
                .AddSingleton<ICustomerRepo, CustomerRepo>()

                .AddSingleton<IDataMigrationService, DataMigrationService>(pb => pb
                    .Type<CustomerRepo>(pb => pb
                        .Type<IDatabaseClient, DatabaseClient>(pb2 => pb2.Value("connection1"))
                        .Value("inject parameter1"))

                    .Type<CustomerRepo>(pb => pb
                        .Type<IDatabaseClient, DatabaseClient>(pb2 => pb2.Value("connection2"))
                        .Value("inject parameter2"))
                )

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