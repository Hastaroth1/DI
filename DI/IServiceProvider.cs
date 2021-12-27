using System.Reflection;

namespace DI;

public interface IServiceProvider
{
    /// <summary>
    /// Register a dependency as Transient.
    /// Transient dependencies are different everytime they are requested.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TService"></typeparam>
    public void AddTransient<TInterface, TService>()
        where TService : class, TInterface;

    /// <summary>
    /// Register a dependency as Scoped.
    /// Scoped dependencies are the same in a <see cref="IServiceScope"/> but different between scopes.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TService"></typeparam>
    public void AddScoped<TInterface, TService>()
        where TService : class, TInterface;

    /// <summary>
    /// Register a dependency as Singleton.
    /// Singleton dependencies are identical between scopes within the same <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TService"></typeparam>
    public void AddSingleton<TInterface, TService>()
        where TService : class, TInterface;

    /// <summary>
    /// Create a scope with this <see cref="IServiceProvider"/>
    /// </summary>
    /// <returns><see cref="IServiceScope"/></returns>
    public IServiceScope CreateScope();

    public TSingleton GetSingleton<TSingleton>();

    public object GetSingleton(Type singletonType);
}

public enum ScopeLevel
{
    Singleton,
    Transient,
    Scoped
}

public class Serviceprovider : IServiceProvider
{
    internal readonly Dictionary<Type, (Type, ScopeLevel)> Dependencies = new();

    public void AddScoped<TInterface, TService>()
        where TService : class, TInterface
    {
        AddService<TInterface, TService>(ScopeLevel.Scoped);
    }

    public void AddSingleton<TInterface, TService>()
        where TService : class, TInterface
    {
        AddService<TInterface, TService>(ScopeLevel.Singleton);
    }

    public void AddTransient<TInterface, TService>()
        where TService : class, TInterface
    {
        AddService<TInterface, TService>(ScopeLevel.Transient);
    }

    private void AddService<TInterface, TService>(ScopeLevel scope)
        where TService : class, TInterface
    {
        Dependencies.Add(typeof(TInterface), (typeof(TService), scope));
    }

    public IServiceScope CreateScope()
    {
        return new ServiceScope(this);
    }

    internal readonly Dictionary<Type, object> Singletons = new();

    public TSingleton GetSingleton<TSingleton>()
    {
        return (TSingleton)GetSingleton(typeof(TSingleton));
    }

    public object GetSingleton(Type singletonType)
    {
        // Check if the singleton exists already and return it if it does.
        if (Singletons.ContainsKey(singletonType))
        {
            return Singletons[singletonType];
        }

        // Singleton is not already in the collection.
        // Create it.
        if (Dependencies.TryGetValue(singletonType, out var dependency))
        {
            var (serviceType, scope) = dependency;
            if (scope != ScopeLevel.Singleton)
            {
                throw new ArgumentException($"{singletonType.FullName} is registered but not as a singleton service.");
            }

            var instance = InitDependency(serviceType, GetSingleton);

            Singletons.Add(singletonType, instance);
            return instance;
        }

        throw new ArgumentException($"{singletonType.FullName} is not registered as a singleton.");
    }

    internal static object InitDependency(Type serviceImpl, Func<Type, object> activator)
    {
        var constructorInfo = FindConstructor(serviceImpl);

        if (constructorInfo is null)
        {
            throw new ArgumentException($"Could not find a suitable constructor to init {serviceImpl.FullName}." +
                $"\nMake sure your class has a public constructor. " +
                $"\nIf your class has more than one constructor, annotate the one you want used with {nameof(ConstructorInjectionAttribute)}.");
        }

        // Todo: handle circular dependencies gracefully.
        var parameters = constructorInfo.GetParameters()
                .Select(p => activator(p.ParameterType));

        return constructorInfo.Invoke(parameters.ToArray());
    }

    internal static ConstructorInfo? FindConstructor(Type type)
    {
        var constructors = type.GetConstructors();
        ConstructorInfo? constructorInfo = null;

        if (constructors.Length == 1)
        {
            // The type only has one constructor.
            // Use that one to instantiate the service.
            constructorInfo = constructors.First();
        }
        else
        {
            // Look for constructor attribute
            constructorInfo = constructors.FirstOrDefault(c => Attribute.GetCustomAttribute(c, typeof(ConstructorInjectionAttribute)) != null);
        }

        return constructorInfo;
    }
}

[AttributeUsage(AttributeTargets.Constructor)]
public class ConstructorInjectionAttribute : Attribute
{
}

public interface IServiceScope : IDisposable
{
    public TService GetService<TService>();

    public object GetService(Type serviceType);
}

public class ServiceScope : IServiceScope
{
    internal readonly Dictionary<Type, object> ScopedService = new();

    private readonly Serviceprovider serviceProvider;

    internal ServiceScope(Serviceprovider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public void Dispose()
    {
        ScopedService.Clear();
        GC.SuppressFinalize(this);
    }

    public TService GetService<TService>()
    {
        return (TService)GetService(typeof(TService));
    }

    public object GetService(Type serviceType)
    {
        if (serviceProvider.Dependencies.TryGetValue(serviceType, out var dependencyInfo))
        {
            var (implementingType, scopeLevel) = dependencyInfo;
            switch (scopeLevel)
            {
                case ScopeLevel.Singleton:
                    return serviceProvider.GetSingleton(serviceType);
                case ScopeLevel.Transient:
                    return Serviceprovider.InitDependency(implementingType, GetService);
                case ScopeLevel.Scoped:
                    object? service;
                    if (ScopedService.TryGetValue(serviceType, out service))
                    {
                        return service;
                    }

                    service = Serviceprovider.InitDependency(implementingType, GetService);
                    ScopedService.Add(serviceType, service);
                    return service;
            }
        }

        throw new ArgumentException($"{serviceType.FullName} is not registered with this ServiceProvider.");
    }
}