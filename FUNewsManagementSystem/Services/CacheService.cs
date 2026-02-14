using Microsoft.Extensions.Caching.Memory;

namespace FUNewsManagementSystem.Services
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        Task<T?> GetAsync<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expiration = null);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        void Remove(string key);
        Task RemoveAsync(string key);
    }

    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogInformation("Cache miss for key: {Key}", key);
            return default;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            return Task.FromResult(Get<T>(key));
        }

        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            _cache.Set(key, value, options);
            _logger.LogInformation("Cache set for key: {Key}", key);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            Set(key, value, expiration);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
            _logger.LogInformation("Cache removed for key: {Key}", key);
        }

        public Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}
