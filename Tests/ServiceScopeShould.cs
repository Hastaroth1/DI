using System;
using Xunit;

namespace DI.Tests;

public class ServiceScopeShould
{
    private interface IFakeInterfaceService { };

    [Fact]
    public void ThrowWhenServiceNotRegistered()
    {
        using var scope = new Serviceprovider().CreateScope();

        Assert.Throws<ArgumentException>(() => scope.GetService<IFakeInterfaceService>());
    }

    private interface ISingletonService { };

    private class SingletonService : ISingletonService { };

    [Fact]
    public void HandleSingletonServices()
    {
        var provider = new Serviceprovider();
        provider.AddSingleton<ISingletonService, SingletonService>();

        using var scope = provider.CreateScope();
        var service = scope.GetService<ISingletonService>();

        Assert.NotNull(service);
        Assert.IsType<SingletonService>(service);
    }

    private interface IScopedService { };

    private class ScopedService : IScopedService { };

    [Fact]
    public void HandleScopedDependency()
    {
        var provider = new Serviceprovider();
        provider.AddScoped<IScopedService, ScopedService>();

        using var scope = provider.CreateScope();

        var service = scope.GetService<IScopedService>();
        var service2 = scope.GetService<IScopedService>();

        Assert.Same(service, service2);
    }

    private interface ITransientService { };

    private class TransientService : ITransientService { };

    [Fact]
    public void HandleTransientDependency()
    {
        var provider = new Serviceprovider();
        provider.AddTransient<ITransientService, TransientService>();

        using var scope = provider.CreateScope();

        var service = scope.GetService<ITransientService>();
        var service2 = scope.GetService<ITransientService>();

        Assert.NotSame(service, service2);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    class A
    {
        [ConstructorInjection]
        public A(B b, C c)
        {
        }

        public A(D d) => throw new InvalidOperationException();
    }

    class B
    {
        public B(C c)
        {

        }
    }
#pragma warning restore IDE0060 // Remove unused parameter

    class C { }

    class D { }

    [Fact]
    public void SelectMarkedConstructors()
    {
        var serviceProvider = new Serviceprovider();
        serviceProvider.AddSingleton<A, A>();
        serviceProvider.AddSingleton<B, B>();
        serviceProvider.AddSingleton<C, C>();

        using var scope = serviceProvider.CreateScope();

        var a = scope.GetService<A>();
    }

#pragma warning disable IDE0060 // Remove unused parameter
    class E
    {
        public E()
        {

        }

        public E(D d)
        {

        }
    }
#pragma warning restore IDE0060 // Remove unused parameter

    [Fact]
    public void ThrowIfAmbiguousConstructors()
    {
        var serviceProvider = new Serviceprovider();
        serviceProvider.AddSingleton<E, E>();
        serviceProvider.AddSingleton<D, D>();

        using var scope = serviceProvider.CreateScope();

        Assert.Throws<ArgumentException>(() => _ = scope.GetService<A>());
    }
}
