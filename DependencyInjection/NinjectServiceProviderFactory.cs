using Ninject;
using Ninject.Parameters;
using Ninject.Web.Common;

namespace DependencyInjection;

public class NinjectServiceProviderFactory : IServiceProviderFactory<IServiceProvider> {
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly Microsoft.Extensions.Hosting.IHostEnvironment _hostEnvironment;
    private readonly Action<IServiceProvider>? _adjustment;

    public NinjectServiceProviderFactory(Microsoft.Extensions.Configuration.IConfiguration configuration, Microsoft.Extensions.Hosting.IHostEnvironment hostEnvironment, Action<IServiceProvider>? adjustment) {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _adjustment = adjustment;
    }

    IServiceProvider IServiceProviderFactory<IServiceProvider>.CreateBuilder(IServiceCollection services) {
        var kernel = new StandardKernel(new NinjectSettings { AllowNullInjection = true });
        // For Request Scope
        kernel.Components.Add<INinjectHttpApplicationPlugin, NinjectHttpApplicationPlugin>();
        kernel.Bind<IServiceScopeFactory>().ToMethod(i => new NinjectServiceScopeFactory(i));


        // Setup service provider and scopes
        var serviceProvider = new NinjectServiceProvider(kernel, new IParameter[0]);
        kernel.Bind<IServiceProvider>().ToMethod(i => i.Parameters.OfType<ScopeParameter>().LastOrDefault()?.ServiceProvider ?? serviceProvider);

        _adjustment?.Invoke(serviceProvider);

        services.Populate(kernel);

        return serviceProvider;
    }

    IServiceProvider IServiceProviderFactory<IServiceProvider>.CreateServiceProvider(IServiceProvider containerBuilder) {
        var disposeableServiceProvider = containerBuilder as IDisposable;
        if (disposeableServiceProvider != null) {
            var lifetime = containerBuilder.GetService<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
            lifetime!.ApplicationStopped.Register(() => {
                disposeableServiceProvider.Dispose();
            });
        }

        return containerBuilder;
    }

}
