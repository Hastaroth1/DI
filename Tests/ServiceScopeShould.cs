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
    private class SingletonService: ISingletonService { };
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
    public void HandlesScopedDependency()
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
    public void HandlesTransientDependency()
    {
        var provider = new Serviceprovider();
        provider.AddTransient<ITransientService, TransientService>();

        using var scope = provider.CreateScope();

        var service = scope.GetService<ITransientService>();
        var service2 = scope.GetService<ITransientService>();

        Assert.NotSame(service, service2);
    }
}
