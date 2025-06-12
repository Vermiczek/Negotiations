using FluentAssertions;
using Negotiations.Models.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json;

namespace NegotiationsApi.IntegrationTests.Tests
{
    public class AuthenticationTests : TestFixture
    {
        public AuthenticationTests()
        {
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "admin",
                Password = "Admin123!"
            };

            // Act
            var json = JsonConvert.SerializeObject(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
            authResponse.Should().NotBeNull();
            authResponse!.Token.Should().NotBeNullOrEmpty();
            authResponse.Roles.Should().Contain("admin");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "admin",
                Password = "WrongPassword"
            };

            // Act
            var json = JsonConvert.SerializeObject(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        
        [Fact]
        public async Task Login_WithSellerCredentials_ShouldHaveSellerRole()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "seller",
                Password = "Seller123!"
            };

            // Act
            var json = JsonConvert.SerializeObject(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/auth/login", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(responseContent);
            authResponse.Should().NotBeNull();
            authResponse!.Token.Should().NotBeNullOrEmpty();
            authResponse.Roles.Should().Contain("seller");
        }
    }
}
