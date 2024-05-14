using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ
{
    /// <summary>
    /// 类型过滤器.
    /// </summary>
    public interface ITypeFilter
    {
        void Filter(Type type, IServiceCollection services);

        void Build(IServiceCollection services);
    }
}
