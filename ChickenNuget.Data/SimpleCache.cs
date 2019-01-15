using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace ChickenNuget.Data
{
    public class SimpleCache<TKey, TObject> : ISimpleCache where TObject : class
    {
        private readonly MemoryCache Cache;

        public SimpleCache()
        {
            Cache = new MemoryCache(new MemoryCacheOptions()
            {
                Clock = new SystemClock(),
                ExpirationScanFrequency = TimeSpan.FromMinutes(3),
            });
        }

        public void Insert(TKey key, TObject obj)
        {
            var entry = Cache.CreateEntry(key);
            entry.SlidingExpiration = TimeSpan.FromMinutes(60);
            entry.Value = obj;
            entry.Dispose();
        }

        public TObject Get(TKey key)
        {
            if (Cache.TryGetValue(key, out TObject obj))
                return obj;
            return null;
        }

        private static object CachesLock = new object();
        private static Dictionary<string, ISimpleCache> Caches = new Dictionary<string, ISimpleCache>();

        public static SimpleCache<TKey, TObject> CreateCache(string name)
        {
            lock (CachesLock)
                if (!Caches.ContainsKey(name))
                    Caches.Add(name, new SimpleCache<TKey, TObject>());

            return (SimpleCache<TKey, TObject>) Caches[name];
        }
    }

    public interface ISimpleCache
    {
    }
}