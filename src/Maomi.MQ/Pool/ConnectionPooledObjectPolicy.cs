using Microsoft.Extensions.ObjectPool;

namespace Maomi.MQ.Pool
{
    /// <summary>
    /// 对象池策略.
    /// </summary>
    public class ConnectionPooledObjectPolicy : PooledObjectPolicy<ConnectionObject>
    {
        private readonly DefaultMqOptions _connectionOptions;

        public ConnectionPooledObjectPolicy(DefaultMqOptions connectionOptions)
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
