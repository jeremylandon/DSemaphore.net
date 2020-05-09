# DSemaphore.net

A distributed semaphore based on Redis.
> See [RedisLabs: Fair semaphores](https://redislabs.com/ebook/part-2-core-concepts/chapter-6-application-components-in-redis/6-3-counting-semaphores/6-3-2-fair-semaphores/)

## Usage

### Create a semaphore factory

##### DSemaphoreFactory.Create(connection)

- `connectionMultiplexer` - **required**, Inter-related group of connections to redis servers
- `redisDatabaseId` _optional_, The ID of the database _(default -1)_.

```csharp
var connection = ConnectionMultiplexer.Connect("127.0.0.1:6379");
var timeout = TimeSpan.FromSeconds(30);
using (var semaphoreFactory = DSemaphoreFactory.Create(connection))
{
  // ...
}
```

### Create a semaphore

##### semaphoreFactory.CreateSemaphore(string semaphoreName, int maxCount, TimeSpan? retryTime = null)

- `semaphoreName` - **required**, Name of the semaphore
- `maxCount` - **required**, The number of requests for the semaphore that can be granted concurrently
- `retryTime` _optional_, How long to wait between retries when trying to acquire a lock. _(default 10ms and it cannot be less than this value)_

```csharp
int maxCount = 5;
var timeout = TimeSpan.FromSeconds(30);
await using (var semaphore = semaphoreFactory.CreateSemaphore("foo", maxCount))
{
    foreach (var entity in collection)
    {
       // ...
    }
}
```

### Wait an acquisition

##### WaitAsync(TimeSpan timeout, string id = null, CancellationToken cancellationToken = default)

- `timeout` - **required**, observation time interval, after the delay has passed the method returns false. This value is important to prevent dead acquisition requests
- `id` - **_optional_**, Id of the semaphore context
- `cancellationToken` _optional_,

```csharp
if (await semaphore.WaitAsync(timeout))
{
    // an action ...
}
```

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
