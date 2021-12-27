using System;
using Xunit;

namespace DI.Tests;

public class ServiceProviderShould
{
    private interface ISomeService { }

    private class SomeService : ISomeService { }

    [Fact]
    public void ThrowWhenSingletonNotRegistered()
    {
        var serviceProvider = new Serviceprovider();
        Assert.Throws<ArgumentException>(() => _ = serviceProvider.GetSingleton<ISomeService>());
    }

    [Fact]
    public void ReturnSingletonWithNoParameter()
    {
        var serviceProvider = new Serviceprovider();
        serviceProvider.AddSingleton<ISomeService, SomeService>();

        var service = serviceProvider.GetSingleton<ISomeService>();

        Assert.NotNull(service);
        Assert.IsType<SomeService>(service);
    }

    private interface IServiceWithOneDependency
    {
        public ISomeService AService { get; set; }
    }

    private class ServiceWithOneDependency : IServiceWithOneDependency
    {
        public ISomeService AService { get; set; }

        public ServiceWithOneDependency(ISomeService service)
        {
            AService = service;
        }
    }

    [Fact]
    public void ReturnSingletonWithSingletonParameter()
    {
        var serviceProvider = new Serviceprovider();
        serviceProvider.AddSingleton<ISomeService, SomeService>();
        serviceProvider.AddSingleton<IServiceWithOneDependency, ServiceWithOneDependency>();

        var service = serviceProvider.GetSingleton<IServiceWithOneDependency>();

        Assert.NotNull(service);
        Assert.IsType<ServiceWithOneDependency>(service);
        Assert.NotNull(service.AService);
        Assert.IsType<SomeService>(service.AService);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    class A
    {
        public A(B b, C c)
        {

        }
    }

    class B
    {
        public B(C c)
        {

        }
    }

    class C { }

#pragma warning restore IDE0060 // Remove unused parameter

    [Fact]
    public void ReturnsSingletonWithMultipleParams()
    {
        var serviceProvider = new Serviceprovider();
        serviceProvider.AddSingleton<A, A>();
        serviceProvider.AddSingleton<B, B>();
        serviceProvider.AddSingleton<C, C>();
        _ = serviceProvider.GetSingleton<A>();
    }
}
