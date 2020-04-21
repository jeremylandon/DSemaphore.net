using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using StackExchange.Redis;
using Xunit;

namespace DSemaphoreNet.IntegrationTests
{
    public class DSemaphoreTest : IDisposable
    {
        private readonly ConnectionMultiplexer _connection = ConnectionMultiplexer.Connect("127.0.0.1:6379");
        private string SemaphoreName => GetType().FullName;

        [Theory]
        [MemberData(nameof(MultipleConcurrenceData))]
        public async Task WaitAsync_MultipleConcurrence_ManageExpiration(int semaphoreCount, int iterationCount, TimeSpan timeout, TimeSpan retryTime, TimeSpan actionDelay, int successCountExpected)
        {
            // arrange
            var db = _connection.GetDatabase();
            var semaphoreResults = new ConcurrentBag<(int id, bool wasAcquired)>();
            DSemaphore CreateSemaphore() => DSemaphore.Create(db, SemaphoreName, semaphoreCount, retryTime);

            // act
            var tasks = Enumerable.Range(0, iterationCount).Select(index => Task.Run(async () => semaphoreResults.Add(await RunWaitAsync(index, CreateSemaphore, actionDelay, timeout))));
            await Task.WhenAll(tasks);

            // assert
            using (new AssertionScope())
            {
                var success = semaphoreResults.Where(e => e.wasAcquired).ToList();
                var failed = semaphoreResults.Where(e => !e.wasAcquired).ToList();

                success.Count.Should().BeInRange(successCountExpected - semaphoreCount, successCountExpected);
                var failCount = iterationCount - successCountExpected;
                failed.Count.Should().BeInRange(failCount, failCount + semaphoreCount);
            }
        }

        async Task<(int Id, bool WasAcquired)> RunWaitAsync(int id, Func<DSemaphore> createFunc, TimeSpan actionDelay, TimeSpan timeout)
        {
            await using var semaphoreProvider = createFunc();

            var wasAcquired = await semaphoreProvider.WaitAsync(timeout);

            var actionResult = (id, wasAcquired);

            if (!actionResult.wasAcquired)
            {
                return actionResult;
            }

            // simulate an action
            await Task.Delay(actionDelay);
            return actionResult;
        }

        public static IEnumerable<object[]> MultipleConcurrenceData
        {
            get
            {
                yield return new object[] {1, 2, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10), TimeSpan.Zero, 2};
                yield return new object[] {2, 5, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(60), TimeSpan.Zero, 2};
                yield return new object[] { 1, 2, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10), TimeSpan.Zero, 2 };
                yield return new object[] { 2, 5, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(60), TimeSpan.Zero, 2 };
                yield return new object[] { 10, 100, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(30), TimeSpan.Zero, 20 };
                yield return new object[] { 2, 5, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10), 5 };
                yield return new object[] { 1, 5, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10), 4 };
                yield return new object[] { 2, 100, TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10), 8 };
                yield return new object[] { 30, 1000, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(10), TimeSpan.Zero, 1000 };
            }
        }

        public void Dispose()
        {
            foreach (var endPoint in _connection.GetEndPoints())
            {
                var server = _connection.GetServer(endPoint);

                if (server == null) continue;

                foreach (var key in server.Keys(pattern: $"{SemaphoreName}.*"))
                {
                    _connection.GetDatabase().KeyDelete(key);
                }
            }

            _connection.Dispose();
        }
    }
}