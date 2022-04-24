using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace DependencyInjection.Extensions.Parameterization
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSingleton<TTargetImplementation>(this IServiceCollection serviceCollection, Action<ParameterBuilder> builder)
          where TTargetImplementation : class
          => serviceCollection.AddSingleton(serviceProvider => Resolve<TTargetImplementation, TTargetImplementation>(serviceProvider, builder));

        public static IServiceCollection AddScoped<TTargetImplementation>(this IServiceCollection serviceCollection, Action<ParameterBuilder> builder)
          where TTargetImplementation : class
          => serviceCollection.AddScoped(serviceProvider => Resolve<TTargetImplementation, TTargetImplementation>(serviceProvider, builder));

        public static IServiceCollection AddTransient<TTargetImplementation>(this IServiceCollection serviceCollection, Action<ParameterBuilder> builder)
          where TTargetImplementation : class
          => serviceCollection.AddTransient(serviceProvider => Resolve<TTargetImplementation, TTargetImplementation>(serviceProvider, builder));

        public static IServiceCollection AddSingleton<TTargetInterface, TTargetImplementation>(this IServiceCollection serviceCollection, Action<ParameterBuilder> builder)
          where TTargetInterface : class
          where TTargetImplementation : class, TTargetInterface
          => serviceCollection.AddSingleton(serviceProvider => Resolve<TTargetInterface, TTargetImplementation>(serviceProvider, builder));

        public static IServiceCollection AddScoped<TTargetInterface, TTargetImplementation>(this IServiceCollection serviceCollection, Action<ParameterBuilder> builder)
          where TTargetInterface : class
          where TTargetImplementation : class, TTargetInterface
          => serviceCollection.AddScoped(serviceProvider => Resolve<TTargetInterface, TTargetImplementation>(serviceProvider, builder));

        public static IServiceCollection AddTransient<TTargetInterface, TTargetImplementation>(this IServiceCollection serviceCollection, Action<ParameterBuilder> builder)
          where TTargetInterface : class
          where TTargetImplementation : class, TTargetInterface
          => serviceCollection.AddTransient(serviceProvider => Resolve<TTargetInterface, TTargetImplementation>(serviceProvider, builder));

        private static TTargetInterface Resolve<TTargetInterface, TTargetImplementation>(IServiceProvider serviceProvider, Action<ParameterBuilder> builder)
            where TTargetInterface : class
            where TTargetImplementation : class, TTargetInterface
        {
            var parameterBuilder = new ParameterBuilder();
            builder(parameterBuilder);
            var parameters = parameterBuilder.Build(serviceProvider).ToArray();
            return ActivatorUtilities.CreateInstance<TTargetImplementation>(serviceProvider, parameters);
        }
    }
}
