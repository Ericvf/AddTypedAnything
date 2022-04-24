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

    public interface IDatabaseClient : IDisposable
    {
    }

    public class DatabaseClient : IDatabaseClient
    {
        public DatabaseClient(string clientName, IOptions<ConfigSetting> options)
        {
            Console.WriteLine("DatabaseClient:" + clientName);
            ClientName = clientName;
        }

        public string ClientName { get; }

        public void Dispose()
        {
            Console.WriteLine($"** Disposing DatabaseClient {ClientName}");
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

    public interface IClient : IDisposable
    {
        public string ConnectionString { get; set; }
    }

    public class Client : IClient
    {
        public string ConnectionString { get; set; }

        public void Dispose()
        {
            Console.WriteLine("Disposing client " + ConnectionString);
        }
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

    public interface IClientService : IDisposable
    {
    }

    public class ClientService : IClientService
    {
        private readonly IClient client;

        public ClientService(IClient client)
        {
            Console.WriteLine($"ClientService: client connection {client.ConnectionString}");
            this.client = client;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing clientservice");
            client.Dispose();
        }
    }

    public class App : IDisposable
    {
        private readonly IService serviceA;
        private readonly IService serviceB;
        private readonly string input;
        private readonly IOptions<ConfigSetting> options;
        private readonly IDataMigrationService dataMigrationService;
        private readonly IClientService clientService;
        private readonly IClientService clientService2;

        public App(IService serviceA, IService serviceB, string input, IOptions<ConfigSetting> options, IDataMigrationService dataMigrationService, IClientService clientService, IClientService clientService2)
        {
            this.serviceA = serviceA;
            this.serviceB = serviceB;
            this.input = input;
            this.options = options;
            this.dataMigrationService = dataMigrationService;
            this.clientService = clientService;
            this.clientService2 = clientService2;
            Console.WriteLine("Created App");
        }

        public void Dispose()
        {
            Console.WriteLine("#@ Disposing App");
            //this.clientService.Dispose();
            //this.clientService2.Dispose();
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
                .AddSingleton<IActivatorFactory, ActivatorFactory>() // Options registration, only used by `.Activate(pb => ...)`
                .AddSingleton<ServiceA>()
                .AddSingleton<ServiceB>()
                .AddSingleton<IDatabaseClient, DatabaseClient>()
                .AddSingleton<ICustomerRepo, CustomerRepo>()
                .AddSingleton<IClientFactory<Client>, ClientFactory<Client>>()

                .AddSingleton<IDataMigrationService, DataMigrationService>(pb => pb
                    .Activate<CustomerRepo>(pb => pb
                        .Activate<DatabaseClient>(pb2 => pb2
                            .Value("connection1")
                            .Options<ConfigSetting>("ConfigSetting"))
                        .Value("inject parameter1"))

                    .Activate<CustomerRepo>(pb => pb
                        .Activate<DatabaseClient>(pb2 => pb2
                            .Value("connection2")
                            .Options<ConfigSetting>("ConfigSetting2"))
                        .Value("inject parameter2"))
                )

                .AddTransient<App>(pb => pb
                    .Type<ServiceA>()
                    .Type<ServiceB>()
                    .Options<ConfigSetting>("ConfigSetting2")
                    .Value("inject parameter")
                    .Activate<ClientService>(pb => pb.AddServiceClient<Client>("ConnectionString1"))
                    .Activate<ClientService>(pb => pb.AddServiceClient<Client>("ConnectionString2")))
                .BuildServiceProvider();
        }

        static async Task Main(string[] args)
        {
            while (Console.ReadLine() != null)
            {
                using (var serviceProvider = BuildServiceProvider())
                {
                    Console.WriteLine("--------------------- FIRST");
                    var application1 = serviceProvider.GetRequiredService<App>();

                    Console.WriteLine("--------------------- SECOND");
                    var application2 = serviceProvider.GetRequiredService<App>();

                    Console.WriteLine("--------------------- THIRD");
                    var application3 = serviceProvider.GetRequiredService<App>();

                    await application1.RunAsync(args);
                    await application2.RunAsync(args);
                    await application3.RunAsync(args);
                }

                using (var serviceProvider = BuildServiceProvider())
                {
                    var application2 = serviceProvider.GetRequiredService<App>();
                    await application2.RunAsync(args);
                }
            }
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