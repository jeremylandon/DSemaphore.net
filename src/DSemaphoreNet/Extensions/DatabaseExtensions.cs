using System.Threading.Tasks;
using StackExchange.Redis;

namespace DSemaphoreNet.Extensions
{
    internal static class DatabaseExtensions
    {
        internal static async Task AddAcquisitionRequestAsync(this IDatabase db, DSemaphoreContext sempahoreContext, double currentTime, long counter)
        {
            // add the current time
            await db.SortedSetAddAsync(sempahoreContext.SemaphoreName, sempahoreContext.Id, currentTime);
            // add the current count
            await db.SortedSetAddAsync(sempahoreContext.CounterSetName, sempahoreContext.Id, counter);
        }

        internal static async Task RemoveObsoleteAcquisitionRequestsAsync(this IDatabase db, DSemaphoreContext sempahoreContext, double outdatedLimit)
        {
            await db.SortedSetRemoveRangeByScoreAsync(sempahoreContext.SemaphoreName, 0, outdatedLimit);
            await db.SortedSetCombineAndStoreAsync(
                SetOperation.Intersect,
                sempahoreContext.CounterSetName,
                new RedisKey[] { sempahoreContext.CounterSetName, sempahoreContext.SemaphoreName },
                new double[] { 1, 0 }
            );
        }

        internal static async Task RemoveSemaphoreAsync(this IDatabase db, DSemaphoreContext sempahoreContext)
        {
            await db.SortedSetRemoveAsync(sempahoreContext.SemaphoreName, sempahoreContext.Id);
            await db.SortedSetRemoveAsync(sempahoreContext.CounterSetName, sempahoreContext.Id);
        }
    }
}
