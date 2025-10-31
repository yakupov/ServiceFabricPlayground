using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using StorageService;

namespace ApiService
{
    [ApiController]
    [Route("store")]
    public class GetPutController : ControllerBase
    {
        [HttpGet("{key}")]
        public async Task<IActionResult> Get(string key)
        {
            var value = await GetStorageService(key).GetAsync(key);
            return Ok(value);
        }

        [HttpPut("{key}")]
        [Consumes("text/plain")]
        public async Task<IActionResult> Put(string key, [FromBody] string value)
        {
            await GetStorageService(key).PutAsync(key, value);
            return NoContent();
        }

        private IStorageService GetStorageService(string key)
        {
            return ServiceProxy.Create<IStorageService>(
                    new Uri("fabric:/SampleKVStore/StorageService"),
                    new ServicePartitionKey(key.GetHashCode()));
        }
    }
}
