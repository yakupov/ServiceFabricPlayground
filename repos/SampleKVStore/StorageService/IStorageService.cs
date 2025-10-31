using Microsoft.ServiceFabric.Services.Remoting;

namespace StorageService
{
    public interface IStorageService : IService
    {
        Task<string> GetAsync(string key);  
        Task PutAsync(string key, string value);
    }
}
