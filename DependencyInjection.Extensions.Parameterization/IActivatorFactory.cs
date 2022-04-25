using System;

namespace DependencyInjection.Extensions.Parameterization
{
    public interface IActivatorFactory : IDisposable
    {
        object CreateInstance(Type type, object[] parameters);

        object RegisterInstance(object instance);
    }
}
