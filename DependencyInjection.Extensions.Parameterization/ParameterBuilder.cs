using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

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
                OptionFactory = (configuration) => {
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
                        yield return optionsParameter.OptionFactory(configuration.Value);
                        break;
                }
            }
        }
    }
}
