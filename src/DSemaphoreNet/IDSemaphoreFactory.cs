using System;

namespace DSemaphoreNet
{
	public interface IDSemaphoreFactory
    {
        /// <summary>
        /// Gets an <see cref="IDSemaphore"/> object using the factory.
        /// </summary>
        /// <param name="semaphoreName">Name of the semaphore.</param>
        /// <param name="maxCount">The number of requests for the semaphore that can be granted concurrently.</param>
        /// <param name="retryTime">How long to wait between retries when trying to acquire a lock. Without specification the default value is 10ms and it cannot be less than this value.</param>
        /// <returns>A object that implement <see cref="IDSemaphore"/>.</returns>
        /// <exception cref="T:System.ArgumentException">The semaphore name is required (<paramref name="semaphoreName"/>).</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">the number of requests that can be granted concurrently cannot be less than 1 (<paramref name="maxCount"/>).</exception>
        IDSemaphore CreateSemaphore(string semaphoreName, int maxCount, TimeSpan? retryTime = null);
    }
}
