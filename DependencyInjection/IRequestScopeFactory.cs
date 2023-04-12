using Ninject.Activation;

namespace DependencyInjection;

public interface IRequestScopeAccessor {
    object? GetRequestScope(IContext context);
}
