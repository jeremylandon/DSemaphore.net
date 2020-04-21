using System;
using System.Threading;
using System.Threading.Tasks;

namespace DSemaphoreNet
{
    /// <summary>
    /// Distributed semaphore interface
    /// </summary>
    public interface IDSemaphore : IAsyncDisposable
    {
        /// <summary>
        /// Asynchronously waits to enter the <see cref="DSemaphore"/>
        /// using a <see cref="T:System.TimeSpan"/> to measure the time interval
        /// while observing a <see cref="T:System.Threading.CancellationToken"/>.
        /// </summary>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="timeout"/> cannot be less than 10 milliseconds
        /// </exception>
        Task<bool> WaitAsync(TimeSpan timeout, string id = null, CancellationToken cancellationToken = default);
    }
}
