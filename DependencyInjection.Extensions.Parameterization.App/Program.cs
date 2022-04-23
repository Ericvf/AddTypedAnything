using DependencyInjection.Extensions.Parameterization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

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
        public string Id { get; }
    }

    public class CustomerRepo : ICustomerRepo
    {
        public CustomerRepo(IDatabaseClient client, string parameter)
        {
            Console.WriteLine("Customerrepo: " + parameter);
            Id = parameter;
        }

        public string Id { get; }
    }

    public interface IDataMigrationService { }

    public class DataMigrationService : IDataMigrationService
    {

        public DataMigrationService(ICustomerRepo source, ICustomerRepo destination)
        {
            Console.WriteLine($"DataMigrationService: source {source.Id} - destination {destination.Id}");
        }
    }

    public interface IClient

    {
        public string ConnectionString { get; set; }
    }

    public class Client : IClient
    {
        public string ConnectionString { get; set; }
    }

    public interface IClientFactory<T>
        where T : IClient
    {
        T CreateClient(string connectionString);
    }

    public class ClientFactory<T> : IClientFactory<T>
        where T : IClient, new()
    {
        public T CreateClient(string connectionString)
        {
            var client = new T();
            client.ConnectionString = connectionString;
            return client;
        }
    }

    public interface IClientService
    {
    }

    public class ClientService : IClientService
    {
        public ClientService(IClient client)
        {
            Console.WriteLine($"ClientService: client connection {client.ConnectionString}");
        }
    }

    public class App
    {
        private readonly IService serviceA;
        private readonly IService serviceB;
        private readonly string input;
        private readonly IOptions<ConfigSetting> options;
        private readonly IDataMigrationService dataMigrationService;
        private readonly IClientService clientService;

        public App(IService serviceA, IService serviceB, string input, IOptions<ConfigSetting> options, IDataMigrationService dataMigrationService, IClientService clientService, IClientService clientService2)
        {
            this.serviceA = serviceA;
            this.serviceB = serviceB;
            this.input = input;
            this.options = options;
            this.dataMigrationService = dataMigrationService;
            this.clientService = clientService;
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
                .AddSingleton<IClientFactory<Client>, ClientFactory<Client>>()

               
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
                    .Value("inject parameter")
                    .Type<IClientService, ClientService>(pb => pb.AddServiceClient<Client>("ConnectionString1"))
                    .Type<IClientService, ClientService>(pb => pb.AddServiceClient<Client>("ConnectionString2")))
                .BuildServiceProvider();
        }

        static async Task Main(string[] args)
        {
            var serviceProvider = BuildServiceProvider();
            var application = serviceProvider.GetRequiredService<App>();
            await application.RunAsync(args);
        }
    }

    public static class ParameterBuilderExtensions
    {
        public static ParameterBuilder AddServiceClient<TClient>(this ParameterBuilder parameterBuilder, string connectionString) where TClient : IClient
            => parameterBuilder.Factory(serviceProvider => serviceProvider
                .GetRequiredService<IClientFactory<TClient>>()
                .CreateClient(connectionString));
    }
}