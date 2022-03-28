using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Caching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastCacheController : ControllerBase
    {
        private readonly ILogger<WeatherForecastCacheController> _logger;
        private readonly IReliableStateManager _stateManager;

        public WeatherForecastCacheController(ILogger<WeatherForecastCacheController> logger, IReliableStateManager stateManager)
        {
            _logger = logger;
            _stateManager = stateManager;
        }

        // PUT api/WeatherForecastCache/cacheKey
        [HttpPut(Name = "PutWeatherForecastCache")]
        public async Task<IActionResult> Put<T>(string cacheKey, [FromBody] T value)
        {
            IReliableDictionary<string, T> votesDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, T>>("mycache");

            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                if (await votesDictionary.ContainsKeyAsync(tx, cacheKey))
                {
                    _logger.LogInformation("Contains cache with key {cacheKey} hence removing", cacheKey);
                    await votesDictionary.TryRemoveAsync(tx, cacheKey);
                }

                await votesDictionary.AddAsync(tx, cacheKey, value);
                await tx.CommitAsync();
                _logger.LogInformation("Added cache with key {cacheKey} and value {value}", cacheKey, value);
            }

            return new OkResult();
        }

        // GET api/WeatherForecastCache/cacheKey
        [HttpGet(Name = "GetWeatherForecastCache")]
        public async Task<IActionResult> Get<T>(string cacheKey)
        {
            IReliableDictionary<string, T> votesDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, T>>("mycache");

            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                var result = await votesDictionary.TryGetValueAsync(tx, cacheKey);
                if (result.HasValue)
                {
                    return Ok(result.Value);
                }
            }

            return Ok();
        }
    }
}