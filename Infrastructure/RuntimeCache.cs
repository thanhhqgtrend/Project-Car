using System;
using System.Runtime.Caching;

namespace LuxuryCar.Infrastructure
{
    public interface IRuntimeCache
    {
        bool TryGetValue<T>(string key, out T value);
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan duration);
        void Set<T>(string key, T value, TimeSpan duration);
    }

    public class MemoryRuntimeCache : IRuntimeCache
    {
        private readonly ObjectCache _cache = MemoryCache.Default;

        public bool TryGetValue<T>(string key, out T value)
        {
            var cached = _cache.Get(key);
            if (cached is T typed)
            {
                value = typed;
                return true;
            }

            value = default(T);
            return false;
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan duration)
        {
            if (TryGetValue<T>(key, out var cached))
            {
                return cached;
            }

            var value = factory();
            Set(key, value, duration);
            return value;
        }

        public void Set<T>(string key, T value, TimeSpan duration)
        {
            _cache.Set(key, value, DateTimeOffset.UtcNow.Add(duration));
        }
    }
}
