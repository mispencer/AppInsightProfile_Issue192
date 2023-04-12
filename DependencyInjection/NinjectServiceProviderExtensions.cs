using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Ninject;

namespace DependencyInjection;

public static class NinjectServiceProviderExtensions {
    public static void AddRequestScopingMiddleware(this IServiceCollection services) {
        if (services == null) {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<RequestScopingStartupFilter>();
        services.AddSingleton<IRequestScopeAccessor>(i => i.GetService<RequestScopingStartupFilter>()!);
        services.AddSingleton<IStartupFilter>(i => i.GetService<RequestScopingStartupFilter>()!);
    }

    public static void Populate(this IServiceCollection services, IKernel kernel) {
        var serviceProvider = kernel.Get<IServiceProvider>();

        IRequestScopeAccessor? requestScopeAccessor = null;
        Func<IRequestScopeAccessor>? requestScopeAccessorFactory = null;
        ServiceDescriptor? requestScopeFactoryBinding = null;
        foreach (var service in services) { if (typeof(IRequestScopeAccessor).IsAssignableFrom(service.ServiceType)) { requestScopeFactoryBinding = service; break; } }
        if (requestScopeFactoryBinding != null) {
            requestScopeAccessorFactory = () => (IRequestScopeAccessor)(
#pragma warning disable IDE0029 // Use coalesce expression
                         requestScopeFactoryBinding.ImplementationInstance != null ? requestScopeFactoryBinding.ImplementationInstance
                    : requestScopeFactoryBinding.ImplementationFactory != null ? requestScopeFactoryBinding.ImplementationFactory(serviceProvider)
                    : requestScopeFactoryBinding.ImplementationType != null ? kernel.Get(requestScopeFactoryBinding.ImplementationType)
                    : throw new Exception("Service instancing impossible")
#pragma warning restore IDE0029 // Use coalesce expression
                    );
            if (requestScopeFactoryBinding.Lifetime == ServiceLifetime.Singleton) {
                //requestScopeAccessor = requestScopeAccessorFactory();
            } else if (requestScopeFactoryBinding.Lifetime == ServiceLifetime.Transient) {
                // Ok
            } else {
                throw new NotSupportedException("IRequestScopeFactory cannot have scoped lifetime");
            }
        }

        // TODO hack
        var loggers = services.Where(f => f.ServiceType.FullName == "Microsoft.ApplicationInsights.Profiler.Core.Logging.IAppInsightsLogger").ToList();
        foreach (var logger in loggers.Skip(1)) {
            services.Remove(logger);
        }

        foreach (var service in services) {
            if (service.ServiceType.Name == "IControllerPropertyActivator" && service.ImplementationType?.Name == "ViewDataDictionaryControllerPropertyActivator") continue;
            if (service.ServiceType.Name == "IControllerPropertyActivator" && service.ImplementationType?.Name == "ViewDataDictionaryControllerPropertyActivator") continue;
            var binding = kernel.Bind(service.ServiceType);
            var bindingLifetime
                = service.ImplementationInstance != null ? binding.ToConstant(service.ImplementationInstance)
                : service.ImplementationFactory != null ? binding.ToMethod(i => service.ImplementationFactory(serviceProvider))
                : service.ImplementationType != null ? binding.To(service.ImplementationType)
                : throw new Exception("Service instancing impossible");
            if (service.Lifetime == ServiceLifetime.Singleton) {
                bindingLifetime.InSingletonScope();
            } else if (service.Lifetime == ServiceLifetime.Transient) {
                bindingLifetime.InTransientScope();
            } else if (service.Lifetime == ServiceLifetime.Scoped) {
                if (requestScopeAccessor != null) {
                    bindingLifetime.InScope(i => requestScopeAccessor.GetRequestScope(i));
                } else if (requestScopeAccessorFactory != null) {
                    bindingLifetime.InScope(i => requestScopeAccessorFactory().GetRequestScope(i));
                } else {
                    throw new Exception($"Scoped without a scope factory: {service.ServiceType.FullName}");
                }
            } else {
                throw new Exception($"Service lifetime unknown: {service.Lifetime}");
            }
        }
    }
}
