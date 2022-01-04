using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DependencyInjection.Extensions.Parameterization
{
    public class ParameterBuilder
    {
        private readonly IList<IParameter> parameters = new List<IParameter>();

        private interface IParameter
        {
        }

        private class ValueParameter : IParameter
        {
            public object Value { get; set; }
        }

        private class TypeParameter : IParameter
        {
            public Type ImplementationType { get; set; }
        }

        private class OptionsParameter : IParameter
        {
            public Func<IConfigurationRoot, object> OptionFactory { get; set; }
        }

        public ParameterBuilder Value(object obj)
        {
            parameters.Add(new ValueParameter()
            {
                Value = obj
            });
            return this;
        }

        public ParameterBuilder Type<TImplementation>()
        {
            parameters.Add(new TypeParameter()
            {
                ImplementationType = typeof(TImplementation)
            });
            return this;
        }

        public ParameterBuilder Options<TImplementation>(string key)
            where TImplementation : class, new()
        {
            parameters.Add(new OptionsParameter()
            {
                OptionFactory = (configurationRoot) => {
                    var instance = new TImplementation();
                    configurationRoot.Bind(key, instance);
                    return Microsoft.Extensions.Options.Options.Create(instance);
                }

            });
            return this;
        }

        public IEnumerable<object> Build(IServiceProvider serviceProvider)
        {
            var configurationRoot = new Lazy<IConfigurationRoot>(() => serviceProvider.GetRequiredService<IConfigurationRoot>());

            foreach (var item in parameters)
            {
                switch (item)
                {
                    case ValueParameter valueParameter:
                        yield return valueParameter.Value;
                        break;

                    case TypeParameter typeParameter:
                        yield return serviceProvider.GetRequiredService(typeParameter.ImplementationType);
                        break;

                    case OptionsParameter optionsParameter:
                        yield return optionsParameter.OptionFactory(configurationRoot.Value);
                        break;
                }
            }
        }
    }

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
            var parameters = parameterBuilder.Build(serviceProvider);
            return ActivatorUtilities.CreateInstance<TTargetImplementation>(serviceProvider, parameters.ToArray());
        }
    }
}
