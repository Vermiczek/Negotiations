// This file is just a placeholder after we removed the WebApplicationFactory approach
// We're using the TestFixture with direct HTTP client instead
// of the WebApplicationFactory approach, which caused issues with access to the Program class
// Tests now run directly against the running API

using System.Net.Http.Headers;

namespace NegotiationsApi.IntegrationTests.Fixtures
{
    // This class is intentionally left empty as we're not using the WebApplicationFactory approach
    public class HttpClientHelper
    {
        // Helper method that could be used to create clients with auth headers if needed
        public static HttpClient CreateClientWithJwt(HttpClient baseClient, string token)
        {
            var client = new HttpClient
            {
                BaseAddress = baseClient.BaseAddress
            };
            
            foreach (var header in baseClient.DefaultRequestHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}
