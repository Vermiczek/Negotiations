using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace NegotiationsApi.IntegrationTests
{
    public class ApiHealthTests
    {
        private readonly HttpClient _client;

        public ApiHealthTests()
        {
            // Load configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
                
            // Get base URL from configuration or use default
            string baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8080";
            
            _client = new HttpClient();
            _client.BaseAddress = new Uri(baseUrl);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [Fact]
        public async Task Health_ShouldReturnHealthy()
        {
            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Fact]
        public async Task Swagger_ShouldBeAvailable()
        {
            // Act
            var response = await _client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
