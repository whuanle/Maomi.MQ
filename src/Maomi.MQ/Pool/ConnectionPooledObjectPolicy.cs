using Microsoft.Extensions.ObjectPool;

namespace Maomi.MQ.Pool
{
    /// <summary>
    /// 对象池策略.
    /// </summary>
    public class ConnectionPooledObjectPolicy : PooledObjectPolicy<ConnectionObject>
    {
        private readonly DefaultConnectionOptions _connectionOptions;

        public ConnectionPooledObjectPolicy(DefaultConnectionOptions connectionOptions)
        {
            _connectionOptions = connectionOptions;
        }

        public override ConnectionObject Create()
        {
            return new ConnectionObject(_connectionOptions);
        }

        public override bool Return(ConnectionObject obj)
        {
            return true;
        }
    }
}
