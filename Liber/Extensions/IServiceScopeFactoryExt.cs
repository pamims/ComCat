using Microsoft.Extensions.DependencyInjection;

namespace ComCat.Extensions
{
    public static class IServiceScopeFactoryExt
    {
        public static IServiceScope GetRequiredScopedService<T>(
            this IServiceScopeFactory scopeFactory,
            out T service)
        {
            var scope = scopeFactory.CreateScope();
            service = scope.ServiceProvider.GetRequiredService<T>();
            return scope;
        }
    }
}
