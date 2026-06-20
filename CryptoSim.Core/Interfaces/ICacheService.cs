using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSim.Core.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T> GetAsync<T>(string key);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}
