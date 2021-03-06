using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System.Text.Json;

namespace Caching.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IReliableStateManager _stateManager;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IReliableStateManager stateManager)
        {
            _logger = logger;
            _stateManager = stateManager;
        }

        // PUT api/WeatherForecast/cacheKey
        [HttpPut(Name = "PutWeatherForecast")]
        public async Task<IActionResult> Put(string cacheKey, [FromBody] dynamic data)
        {
            //dynamic value = JsonSerializer.Deserialize<dynamic>(data.ToString());
            string value = JsonSerializer.Serialize(data);
            var votesDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("mycache");

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

        // GET api/WeatherForecast/cacheKey
        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IActionResult> Get(string cacheKey)
        {
            var votesDictionary = await _stateManager.GetOrAddAsync<IReliableDictionary<string, string>>("mycache");

            using (ITransaction tx = _stateManager.CreateTransaction())
            {
                var result = await votesDictionary.TryGetValueAsync(tx, cacheKey);
                if (result.HasValue)
                {
                    return Ok(JsonSerializer.Deserialize<dynamic>(result.Value));
                }
            }

            return Ok();
        }
    }
}