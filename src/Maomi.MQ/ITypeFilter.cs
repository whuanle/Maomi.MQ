using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ
{
    public interface ITypeFilter
    {
        void Filter(Type type, IServiceCollection services);

        void Build(IServiceCollection services);
    }
}
