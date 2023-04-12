using System.Collections.Concurrent;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using Ninject;
using Ninject.Activation;
using Ninject.Infrastructure.Disposal;

namespace DependencyInjection;

public class RequestScopingStartupFilter : IStartupFilter, IRequestScopeAccessor {
    private readonly IDictionary<object, Scope> _currentScopes = new ConcurrentDictionary<object, Scope>();
    private ILogger<RequestScopingStartupFilter>? _log = null;

    public RequestScopingStartupFilter(/*ILogger<RequestScopingStartupFilter> log*/) {
        //_log=log;
    }

    private sealed class Scope : DisposableObject {
        private readonly Guid _id = Guid.NewGuid();
        private readonly ILogger<RequestScopingStartupFilter>? _log;

        public override string ToString() => $"Request_{_id}";
        public Scope(ILogger<RequestScopingStartupFilter>? log) {
            _log = log;
            _log?.LogTrace($"Created scope {this}");
        }

        public override void Dispose(bool disposing) {
            base.Dispose(disposing);
            _log?.LogTrace($"Destroyed scope {this}");
        }
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> nextFilter) {
        return builder => {
            ConfigureRequestScoping(builder);
            nextFilter(builder);
        };
    }

    private void ConfigureRequestScoping(IApplicationBuilder builder) {
        builder.Use(async (context, next) => {
            using (var scope = new Scope(_log)) {
                _currentScopes[context] = scope;
                try {
                    await next();
                } finally {
                    _currentScopes.Remove(context);
                }
            }
        });
    }

    internal IRequestScopeAccessor GetRequestScopeFactory() {
        return this;
    }

    private static string GetTarget(IRequest request)
        => $"\nto {request}" + (request.ParentRequest == null ? "" : " " + GetTarget(request.ParentRequest));

    object? IRequestScopeAccessor.GetRequestScope(IContext context) {
        object? result = context.Parameters.OfType<ScopeParameter>().LastOrDefault();
        if (result == null) {
            var httpContext = context.Kernel.Get<Microsoft.AspNetCore.Http.HttpContextAccessor>().HttpContext;
            result = httpContext == null ? null : _currentScopes.TryGetValue(httpContext, out var result2) ? result2 : null;
        }
        return result;
    }
}
