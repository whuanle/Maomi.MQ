using Maomi.MQ.Barrier.DB;
using Microsoft.Extensions.DependencyInjection;

namespace Maomi.MQ.Barrier;

public static class BarrierExtensions
{
    public static IServiceCollection AddBarrier(this IServiceCollection services, Action<DtmOptions> action)
    {
        DtmOptions dtmOptions = new();
        action.Invoke(dtmOptions);

        if (dtmOptions.DBType == DBType.Mysql)
        {
            services.AddTransient<DBSpecial, MysqlDBSpecial>();
        }
        else if (dtmOptions.DBType == DBType.Postgres)
        {
            services.AddTransient<DBSpecial, PostgresDBSpecial>();
        }
        else
        {
            throw new Exception();
        }
        return services;
    }
}
