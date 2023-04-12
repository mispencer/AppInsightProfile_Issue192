using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Ninject.Syntax;

namespace DependencyInjection;

public class NinjectServiceScopeFactory : IServiceScopeFactory {
    private readonly IResolutionRoot _resolver;
    private readonly IEnumerable<IParameter> _inheritedParameters;
    private readonly ILoggerFactory _loggerFactory;

    public NinjectServiceScopeFactory(IContext context) {
        _resolver = context.Kernel.Get<IResolutionRoot>();
        _inheritedParameters = context.Parameters.Where(p => p.ShouldInherit);
        _loggerFactory = context.Kernel.Get<ILoggerFactory>();
    }

    public IServiceScope CreateScope() {
        return new NinjectServiceScope(_loggerFactory, _resolver, _inheritedParameters);
    }

    private class NinjectServiceScope : IServiceScope {
        private readonly ScopeParameter _scope;
        private readonly IServiceProvider _serviceProvider;

        public NinjectServiceScope(ILoggerFactory loggerFactory, IResolutionRoot resolver, IEnumerable<IParameter> inheritedParameters) {
            _scope = new ScopeParameter(loggerFactory.CreateLogger<ScopeParameter>());
            inheritedParameters = AddOrReplaceScopeParameter(inheritedParameters, _scope);
            _serviceProvider = new NinjectServiceProvider(resolver, inheritedParameters.ToArray());
            _scope.ServiceProvider = _serviceProvider;
        }

        internal static IEnumerable<IParameter> AddOrReplaceScopeParameter(
                IEnumerable<IParameter> parameters,
                ScopeParameter scopeParameter) {
            return parameters
                .Where(p => !(p is ScopeParameter))
                .Concat(new[] { scopeParameter });
        }

        public IServiceProvider ServiceProvider {
            get { return _serviceProvider; }
        }

        public void Dispose() {
            _scope.Dispose();
        }
    }
}
