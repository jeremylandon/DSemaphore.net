using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DSemaphoreNet.Extensions;
using DSemaphoreNet.Internals;
using StackExchange.Redis;

namespace DSemaphoreNet
{
    /// <summary>
    /// Limits the number of threads that can access a resource or pool of resources concurrently with Redis
    /// </summary>
    public class DSemaphore : IDSemaphore
    {
        private readonly TimeSpan _minimumTimeoutTime = TimeSpan.FromMilliseconds(10);
        private readonly TimeSpan _minimumRetryTime = TimeSpan.FromMilliseconds(10);

        private readonly ConcurrentDictionary<string, Lazy<DSemaphoreContext>> _contexts;
        private readonly TimeSpan _retryTime;
        private readonly string _semaphoreName;
        private readonly int _maxCount;
        private readonly IDatabase _db;
        private readonly CancellationToken _cancellationToken;
        private readonly SemaphoreSlim _locker;

        private bool _isDisposed;

        private DSemaphore(IDatabase db, string semaphoreName, int maxCount, TimeSpan? retryTime = null, CancellationToken cancellationToken = default)
        {
            // ReSharper disable once JoinNullCheckWithUsage
            if (db == null)
            {
                throw new ArgumentNullException($"{nameof(db)} is required");
            }

            if (string.IsNullOrWhiteSpace(semaphoreName))
            {
                throw new ArgumentException($"{nameof(semaphoreName)} cannot be empty");
            }

            if (maxCount < 1)
            {
                throw new ArgumentOutOfRangeException($"{nameof(maxCount)} cannot be less than 1");
            }

            _db = db;
            _semaphoreName = semaphoreName;
            _maxCount = maxCount;
            _retryTime = !retryTime.HasValue || retryTime.Value < _minimumRetryTime ? _minimumRetryTime : retryTime.Value;
            _cancellationToken = cancellationToken;
            _contexts = new ConcurrentDictionary<string, Lazy<DSemaphoreContext>>();
            _locker = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Create a new instance of the <see cref="DSemaphore"/> class, specifying the database and the semaphore name the lock is for.
        /// </summary>
        /// <exception cref="T:System.ArgumentNullException">The database is required.</exception>
        /// <exception cref="T:System.ArgumentException">The semaphore name is required.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">the number of requests that can be granted concurrently cannot be less than 1.</exception>
        internal static DSemaphore Create(IDatabase db, string semaphoreName, int maxCount, TimeSpan? retryTime = null, CancellationToken cancellationToken = default)
        {
            return new DSemaphore(db, semaphoreName, maxCount, retryTime, cancellationToken);
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, string id = null,
            CancellationToken cancellationToken = default)
        {
            CheckDispose();

            if (timeout < _minimumTimeoutTime)
            {
                throw new ArgumentOutOfRangeException($"{nameof(timeout)} cannot be less than {_minimumTimeoutTime}");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<bool>(cancellationToken);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationToken, default);

            id ??= Guid.NewGuid().ToString();

            var semaphoreContext = _contexts.GetOrAdd(id, s => new Lazy<DSemaphoreContext>(() => DSemaphoreContext.CreateNewContext(id, _semaphoreName))).Value;
            var isAcquired = false;

            if (semaphoreContext.CreationDate < DateTime.Now)
            {
                var currentTime = semaphoreContext.CreationDate.ToOADate();
                var obsoleteSemaphoreLimit = (semaphoreContext.CreationDate - timeout).ToOADate();

                await _db.RemoveObsoleteAcquisitionRequestsAsync(semaphoreContext, obsoleteSemaphoreLimit);

                var currentCounter = await _db.StringIncrementAsync(semaphoreContext.CounterName);

                await _db.AddAcquisitionRequestAsync(semaphoreContext, currentTime, currentCounter);

                var stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed <= timeout)
                {
                    isAcquired = await CanBeAcquiredAsync(semaphoreContext, cts.Token);
                    if (isAcquired) break;

                    await Task.Delay(_retryTime, cancellationToken);
                }
            }

            await ReleaseAsync(semaphoreContext, cts.Token);

            cts.Cancel();

            return isAcquired;
        }

        private async Task<bool> CanBeAcquiredAsync(DSemaphoreContext sempahoreContext, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentPosition = await _db.SortedSetRankAsync(sempahoreContext.CounterSetName, sempahoreContext.Id);

            return currentPosition.HasValue && currentPosition.Value < _maxCount;
        }

        /// <summary>
        /// Exits the <see cref="DSemaphoreContext"/>.
        /// </summary>
        private async Task ReleaseAsync(DSemaphoreContext context, CancellationToken cancellationToken)
        {
            await _locker.WaitAsync(cancellationToken);

            try
            {
                if (!_contexts.ContainsKey(context.Id))
                {
                    return;
                }

                await _db.RemoveSemaphoreAsync(context);

                _contexts.TryRemove(context.Id, out _);
            }
            finally
            {
                _locker.Release();
            }
        }

        /// <summary>
        /// Checks the dispose status by checking the lock object, if it is null means that object
        /// has been disposed and throw ObjectDisposedException
        /// </summary>
        private void CheckDispose()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DSemaphore));
            }
        }

        /// <summary>
        /// Releases all <see cref="DSemaphoreContext"/> used by the current instance of <see cref="DSemaphore"/>.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            foreach (var context in _contexts)
            {
                await ReleaseAsync(context.Value.Value, _cancellationToken);
            }

            _isDisposed = true;
        }
    }
}
