using System;

namespace DSemaphoreNet.Internals
{
    internal class DSemaphoreContext
    {
        private DSemaphoreContext() { }

        public static DSemaphoreContext CreateNewContext(string id, string semaphoreName)
        {
            return new DSemaphoreContext
            {
                Id = id,
                CreationDate = DateTime.UtcNow,
                SemaphoreName = semaphoreName
            };
        }

        public string Id { get; private set; }
        public DateTime CreationDate { get; private set; }
        public string SemaphoreName { get; private set; }
        public string CounterSetName => SemaphoreName + ":owner";
        public string CounterName => SemaphoreName + ":counter";
    }
}