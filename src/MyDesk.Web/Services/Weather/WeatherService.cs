using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyDesk.Web.Services.Weather
{
    public class WeatherOptions
    {
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "https://api.openweathermap.org/data/2.5";
        public string Units { get; set; } = "metric";
    }

    public class WeatherData
    {
        public string City { get; set; } = "";
        public double Temperature { get; set; }
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
    }

    public interface IWeatherService
    {
        Task<WeatherData?> GetWeatherAsync(string city);
        Task<WeatherData[]> GetForecastAsync(string city, int days);
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly WeatherOptions _options;
        private readonly ILogger<WeatherService> _logger;

        public WeatherService(HttpClient httpClient, IOptions<WeatherOptions> options, ILogger<WeatherService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<WeatherData?> GetWeatherAsync(string city)
        {
            try
            {
                if (string.IsNullOrEmpty(_options.ApiKey))
                {
                    // Return mock data if no API key is configured
                    return GetMockWeatherData(city);
                }

                var url = $"{_options.BaseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_options.ApiKey}&units={_options.Units}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Weather API request failed with status {StatusCode} for city {City}", 
                                       response.StatusCode, city);
                    return GetMockWeatherData(city);
                }

                var json = await response.Content.ReadAsStringAsync();
                var weatherResponse = JsonSerializer.Deserialize<WeatherApiResponse>(json);
                
                if (weatherResponse == null)
                    return GetMockWeatherData(city);

                return new WeatherData
                {
                    City = weatherResponse.Name,
                    Temperature = Math.Round(weatherResponse.Main.Temp, 1),
                    Description = weatherResponse.Weather[0].Description,
                    Icon = weatherResponse.Weather[0].Icon,
                    Humidity = weatherResponse.Main.Humidity,
                    WindSpeed = Math.Round(weatherResponse.Wind.Speed, 1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather for city {City}", city);
                return GetMockWeatherData(city);
            }
        }

        public async Task<WeatherData[]> GetForecastAsync(string city, int days)
        {
            // For simplicity, we'll return the same current weather for all days
            // In a real implementation, you'd call the forecast API
            var current = await GetWeatherAsync(city);
            if (current == null)
                return Array.Empty<WeatherData>();

            var forecast = new WeatherData[days];
            for (int i = 0; i < days; i++)
            {
                forecast[i] = new WeatherData
                {
                    City = current.City,
                    Temperature = current.Temperature + (new Random().Next(-3, 4)), // Small variation
                    Description = current.Description,
                    Icon = current.Icon,
                    Humidity = current.Humidity + new Random().Next(-10, 11),
                    WindSpeed = current.WindSpeed + (new Random().Next(-2, 3) * 0.5)
                };
            }
            return forecast;
        }

        private WeatherData GetMockWeatherData(string city)
        {
            // Mock data for demonstration purposes
            var random = new Random();
            var conditions = new[] { "Clear sky", "Few clouds", "Scattered clouds", "Broken clouds", "Shower rain", "Rain", "Thunderstorm", "Snow", "Mist" };
            var icons = new[] { "01d", "02d", "03d", "04d", "09d", "10d", "11d", "13d", "50d" };
            
            int index = random.Next(conditions.Length);
            
            return new WeatherData
            {
                City = city,
                Temperature = Math.Round(15 + random.NextDouble() * 15, 1), // 15-30°C
                Description = conditions[index],
                Icon = icons[index],
                Humidity = random.Next(30, 90),
                WindSpeed = Math.Round(random.NextDouble() * 5, 1) // 0-5 m/s
            };
        }

        private class WeatherApiResponse
        {
            public Coord Coord { get; set; } = default!;
            public Weather[] Weather { get; set; } = default!;
            public string Base { get; set; } = default!;
            public Main Main { get; set; } = default!;
            public int Visibility { get; set; }
            public Wind Wind { get; set; } = default!;
            public Clouds Clouds { get; set; } = default!;
            public int Dt { get; set; }
            public Sys Sys { get; set; } = default!;
            public int Timezone { get; set; }
            public int Id { get; set; }
            public string Name { get; set; } = default!;
            public int Cod { get; set; }
        }

        private class Coord
        {
            public double Lon { get; set; }
            public double Lat { get; set; }
        }

        private class Weather
        {
            public int Id { get; set; }
            public string Main { get; set; } = default!;
            public string Description { get; set; } = default!;
            public string Icon { get; set; } = default!;
        }

        private class Main
        {
            public double Temp { get; set; }
            public double FeelsLike { get; set; }
            public double TempMin { get; set; }
            public double TempMax { get; set; }
            public int Pressure { get; set; }
            public int Humidity { get; set; }
        }

        private class Wind
        {
            public double Speed { get; set; }
            public int Deg { get; set; }
            public double Gust { get; set; }
        }

        private class Clouds
        {
            public int All { get; set; }
        }

        private class Sys
        {
            public int Type { get; set; }
            public int Id { get; set; }
            public string Country { get; set; } = default!;
            public long Sunrise { get; set; }
            public long Sunset { get; set; }
        }
    }
}