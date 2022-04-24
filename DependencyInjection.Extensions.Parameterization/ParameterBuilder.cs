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
            public Type ServiceType { get; set; }

            public Type ImplementationType { get; set; }

            public Action<ParameterBuilder> ParameterBuilder { get; set; }
        }

        private class FactoryParameter<ImplementationType> : FactoryParameter
        {
            public Func<IServiceProvider, ImplementationType> ImplementationFactory { get; set; }

            public override object Resolve(IServiceProvider serviceProvider) => ImplementationFactory(serviceProvider);
        }

        private abstract class FactoryParameter : IParameter
        {
            public abstract object Resolve(IServiceProvider serviceProvider);
        }

        private class OptionsParameter : IParameter
        {
            public Func<IConfiguration, object> OptionFactory { get; set; }
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
            Activate<TImplementation>(null);
            return this;
        }

        public ParameterBuilder Activate<TImplementation>(Action<ParameterBuilder> parameterBuilder = null)
        {
            parameters.Add(new TypeParameter()
            {
                ImplementationType = typeof(TImplementation),
                ParameterBuilder = parameterBuilder,
            });

            return this;
        }

        public ParameterBuilder Factory<TImplementation>(Func<IServiceProvider, TImplementation> implementationFactory)
        {
            parameters.Add(new FactoryParameter<TImplementation>()
            {
                ImplementationFactory = implementationFactory,
            });

            return this;
        }

        public ParameterBuilder Options<TImplementation>(string key)
            where TImplementation : class, new()
        {
            parameters.Add(new OptionsParameter()
            {
                OptionFactory = (configuration) =>
                {
                    var instance = new TImplementation();
                    configuration.Bind(key, instance);
                    return Microsoft.Extensions.Options.Options.Create(instance);
                }
            });
            return this;
        }

        public IEnumerable<object> Build(IServiceProvider serviceProvider)
        {
            var configuration = new Lazy<IConfiguration>(() => serviceProvider.GetRequiredService<IConfiguration>());
            var activatorFactory = new Lazy<IActivatorFactory>(() => serviceProvider.GetRequiredService<IActivatorFactory>());

            foreach (var item in parameters)
            {
                switch (item)
                {
                    case ValueParameter valueParameter:
                        yield return valueParameter.Value;
                        break;

                    case TypeParameter typeParameter:
                        if (typeParameter.ParameterBuilder != null)
                        {
                            var parameterBuilder = new ParameterBuilder();
                            typeParameter.ParameterBuilder(parameterBuilder);
                            var parameters = parameterBuilder.Build(serviceProvider).ToArray();
                            yield return activatorFactory.Value.CreateInstance(typeParameter.ImplementationType ?? typeParameter.ServiceType, parameters);
                        }
                        else
                        {
                            yield return serviceProvider.GetRequiredService(typeParameter.ImplementationType);
                        }
                        break;

                    case FactoryParameter factoryParameter:
                        yield return factoryParameter.Resolve(serviceProvider);
                        break;

                    case OptionsParameter optionsParameter:
                        yield return optionsParameter.OptionFactory(configuration.Value);
                        break;
                }
            }
        }
    }
}
