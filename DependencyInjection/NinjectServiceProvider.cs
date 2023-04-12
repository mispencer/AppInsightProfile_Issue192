#undef NinjectServiceProvider_Logging
using System.Collections;
using System.Reflection;

using Ninject;
using Ninject.Parameters;
using Ninject.Syntax;

namespace DependencyInjection;

public class NinjectServiceProvider : IServiceProvider, IDisposable {
    private static readonly MethodInfo GETALL;

    private readonly IResolutionRoot _resolver;
    private readonly IParameter[] _inheritedParameters;
    private readonly object[] _getAllParameters;
#if NinjectServiceProvider_Logging
    private readonly global::Common.Logging.ILog? _log;
#endif

    static NinjectServiceProvider() {
        GETALL = typeof(ResolutionExtensions).GetMethod(
            "GetAll", new Type[] { typeof(IResolutionRoot), typeof(IParameter[]) })!;
    }

    public NinjectServiceProvider(IResolutionRoot resolver, IParameter[] inheritedParameters) {
        _resolver = resolver;
        _inheritedParameters = inheritedParameters;
        _getAllParameters = new object[] { resolver, inheritedParameters };
#if NinjectServiceProvider_Logging
        _log = _resolver.TryGet<global::Common.Logging.ILog>();
#endif
    }


    public object? GetService(Type type) {
        return GetSingleService(type) ??
            GetLast(GetAll(type)) ??
            GetMultiService(type);
    }

    private object GetSingleService(Type type) {
#if NinjectServiceProvider_Logging
        _log?.Trace($"GetSingle Type: {type.FullName}");
#endif
        return _resolver.TryGet(type, _inheritedParameters);
    }

    private IEnumerable? GetMultiService(Type collectionType) {
        var collectionTypeInfo = collectionType.GetTypeInfo();
        if (collectionTypeInfo.IsGenericType &&
            collectionTypeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
            var serviceType = collectionTypeInfo.GenericTypeArguments.Single();
            return GetAll(serviceType);
        }

        return null;
    }

    private IEnumerable GetAll(Type type) {
#if NinjectServiceProvider_Logging
        _log?.Trace($"GetAll Type: {type.FullName}");
#endif
        var getAll = GETALL.MakeGenericMethod(type);
        return (IEnumerable)getAll.Invoke(null, _getAllParameters)!;
    }

    private static object? GetLast(IEnumerable services) {
        object? result = null;
        foreach (var service in services) {
            result = service;
        }
        return result;
    }

    public void Dispose() {
        Dispose(true);
    }

    private bool _disposed = false;
    private void Dispose(bool disposing) {
        if (!_disposed) {
            (_resolver as IDisposable)?.Dispose();
            foreach (var param in _inheritedParameters) {
                (param as IDisposable)?.Dispose();
            }
        }
        _disposed = true;
    }
}
