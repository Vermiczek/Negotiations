using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Negotiations.Models.DTOs;
using Newtonsoft.Json;

namespace NegotiationsApi.IntegrationTests
{
    public class TestFixture : IAsyncLifetime
    {
        protected readonly HttpClient Client;
        protected string? AdminToken { get; private set; }
        protected string? SellerToken { get; private set; }
        protected IConfiguration Configuration { get; }
        
        public TestFixture()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
                
            string baseUrl = Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8080";
            
            Client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl)
            };
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        
        public async Task InitializeAsync()
        {
            string adminUsername = Configuration["ApiSettings:AdminUsername"] ?? "admin";
            string adminPassword = Configuration["ApiSettings:AdminPassword"] ?? "Admin123!";
            AdminToken = await GetAuthToken(new LoginRequest { Username = adminUsername, Password = adminPassword });
            
            string sellerUsername = Configuration["ApiSettings:SellerUsername"] ?? "seller"; 
            string sellerPassword = Configuration["ApiSettings:SellerPassword"] ?? "Seller123!";
            SellerToken = await GetAuthToken(new LoginRequest { Username = sellerUsername, Password = sellerPassword });
        }
        
        public Task DisposeAsync()
        {
            Client.Dispose();
            return Task.CompletedTask;
        }
        
        protected async Task<string?> GetAuthToken(LoginRequest loginRequest)
        {
            var content = new StringContent(JsonConvert.SerializeObject(loginRequest), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync("/api/auth/login", content);
            
            if (!response.IsSuccessStatusCode)
                return null;
                
            var result = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthResponse>(result);
            return authResponse?.Token;
        }
        
        protected void AuthenticateAsAdmin()
        {
            if (string.IsNullOrEmpty(AdminToken))
                throw new InvalidOperationException("Admin token is not available");
                
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken);
        }
        
        protected void AuthenticateAsSeller()
        {
            if (string.IsNullOrEmpty(SellerToken))
                throw new InvalidOperationException("Seller token is not available");
                
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SellerToken);
        }
        
        protected void RemoveAuthentication()
        {
            Client.DefaultRequestHeaders.Authorization = null;
        }
        
        protected void SetClientIdentifier(string clientId)
        {
            if (Client.DefaultRequestHeaders.Contains("Client-Identifier"))
                Client.DefaultRequestHeaders.Remove("Client-Identifier");
                
            Client.DefaultRequestHeaders.Add("Client-Identifier", clientId);
        }
        
        protected void RemoveClientIdentifier()
        {
            if (Client.DefaultRequestHeaders.Contains("Client-Identifier"))
                Client.DefaultRequestHeaders.Remove("Client-Identifier");
        }
    }
}
