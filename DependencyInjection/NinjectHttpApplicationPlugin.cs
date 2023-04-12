using Ninject;
using Ninject.Activation;
using Ninject.Web.Common;

namespace DependencyInjection;

public class NinjectHttpApplicationPlugin : INinjectHttpApplicationPlugin {
    public INinjectSettings? Settings { get; set; }

    public void Dispose() {
    }

    public object? GetRequestScope(IContext context) {
        return context.Kernel.Get<IRequestScopeAccessor>().GetRequestScope(context);
    }

    public void Start() {
    }

    public void Stop() {
    }
}
