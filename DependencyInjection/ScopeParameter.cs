using Microsoft.Extensions.Logging;

using Ninject.Activation;
using Ninject.Infrastructure.Disposal;
using Ninject.Parameters;
using Ninject.Planning.Targets;

namespace DependencyInjection;

public class ScopeParameter : IParameter, IDisposable, IDisposableObject, INotifyWhenDisposed {
    private Guid _id = Guid.NewGuid();
    private readonly ILogger<ScopeParameter> _logger;

    public ScopeParameter(ILogger<ScopeParameter> logger) {
        _logger = logger;
        _logger.LogTrace($"Created scope {this}");
    }

    public override string ToString() => $"Scope_{_id}";

    public string Name {
        get { return typeof(ScopeParameter).FullName!; }
    }

    public bool ShouldInherit {
        get { return true; }
    }

    public object? GetValue(IContext context, ITarget target) {
        return null;
    }

    public bool Equals(IParameter? other) {
        return this == other;
    }

    public void Dispose() {
        Disposed?.Invoke(this, EventArgs.Empty);
        IsDisposed = true;
        _logger.LogTrace($"Disposed scope {this}");
    }

    public bool IsDisposed { get; private set; }

    public event EventHandler? Disposed;

    public IServiceProvider? ServiceProvider { get; internal set; }
}
