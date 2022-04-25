using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace DependencyInjection.Extensions.Parameterization
{
    public class ActivatorFactory : IActivatorFactory
    {
        private readonly ConcurrentStack<IDisposable> activatedInstances = new ConcurrentStack<IDisposable>();
        private readonly IServiceProvider serviceProvider;

        public ActivatorFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public object CreateInstance(Type type, object[] parameters)
        {
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, type, parameters);

            if (instance is IDisposable disposable)
                activatedInstances.Push(disposable);

            return instance;
        }

        public void Dispose()
        {
            while (activatedInstances.TryPop(out var disposable))
                disposable.Dispose();
        }

        public object RegisterInstance(object instance)
        {
            if (instance is IDisposable disposable)
                activatedInstances.Push(disposable);

            return instance;
        }
    }
}
