using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>?> Get()
        {
            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Get,
                "http://localhost:19081/SFCaching/Caching/WeatherForecast?cacheKey=testcache");

            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var result = await httpResponseMessage.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<WeatherForecast>>(result, options);
            }

            return Enumerable.Empty<WeatherForecast>();
        }

        [HttpPut(Name = "PutWeatherForecast")]
        public async Task<IActionResult> Put()
        {
            var rng = new Random();
            var weatherForecastList = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray();

            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Put,
                "http://localhost:19081/SFCaching/Caching/WeatherForecast?cacheKey=testcache")
            {
                Content = new StringContent(JsonSerializer.Serialize(weatherForecastList, options), Encoding.UTF8, "application/json"),
            };

            var httpClient = _httpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                return NoContent();
            }

            return StatusCode((int)httpResponseMessage.StatusCode);
        }
    }
}