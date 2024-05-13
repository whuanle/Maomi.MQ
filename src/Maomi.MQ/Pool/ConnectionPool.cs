using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Maomi.MQ.Pool
{
    /// <summary>
    /// 连接对象池.
    /// </summary>
    public class ConnectionPool : ObjectPool<ConnectionObject>
    {
        private readonly DefaultObjectPoolProvider _defaultObjectPoolProvider;
        private readonly ConnectionPooledObjectPolicy _connectionPooledObjectPolicy;
        private readonly ObjectPool<ConnectionObject> _objectPool;

        public ConnectionPool(ConnectionPooledObjectPolicy connectionPooledObjectPolicy)
        {
            _connectionPooledObjectPolicy = connectionPooledObjectPolicy;
            _defaultObjectPoolProvider = new DefaultObjectPoolProvider();

            _objectPool = _defaultObjectPoolProvider.Create<ConnectionObject>(_connectionPooledObjectPolicy);
        }

        public override ConnectionObject Get()
        {
            return _objectPool.Get();
        }

        public override void Return(ConnectionObject obj)
        {
            _objectPool.Return(obj);
        }
    }
}
