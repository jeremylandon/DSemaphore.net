using System;
using StackExchange.Redis;

namespace DSemaphoreNet
{
    /// <summary>
    /// Factory to create <see cref="DSemaphore"/> objects
    /// </summary>
    public class DSemaphoreFactory : IDSemaphoreFactory, IDisposable
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly int _redisDatabaseId;

        private DSemaphoreFactory(IConnectionMultiplexer connectionMultiplexer, int redisDatabaseId)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _redisDatabaseId = redisDatabaseId;
        }

        /// <summary>
        /// Create a DSemaphoreFactory
        /// </summary>
        public static DSemaphoreFactory Create(IConnectionMultiplexer connectionMultiplexer, int redisDatabaseId = -1)
        {
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer), "parameter is missing.");
            }

            return new DSemaphoreFactory(connectionMultiplexer, redisDatabaseId);
        }

        public IDSemaphore CreateSemaphore(string semaphoreName, int maxCount, TimeSpan? retryTime = null)
        {
            return DSemaphore.Create(_connectionMultiplexer.GetDatabase(_redisDatabaseId), semaphoreName, maxCount, retryTime);
        }

        public void Dispose()
        {
            _connectionMultiplexer.Dispose();
        }
    }
}
