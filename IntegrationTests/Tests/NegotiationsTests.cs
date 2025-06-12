using FluentAssertions;
using Negotiations.Models;
using Newtonsoft.Json;
using NegotiationsApi.IntegrationTests.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace NegotiationsApi.IntegrationTests.Tests
{
    public class NegotiationsTests : TestFixture
    {
        private const string TestClientEmail = "testclient@example.com";
        private const string TestClientIdentifier = "client-12345";

        public NegotiationsTests()
        {
        }

        [Fact]
        public async Task CreateNegotiation_WithEmailOnly_ShouldCreateNegotiation()
        {
            // Arrange
            RemoveAuthentication();
            RemoveClientIdentifier();
            
            // Create a product first
            AuthenticateAsAdmin();
            
            // Create a new product using direct HTTP call
            var productData = new
            {
                Name = "Test Product",
                Description = "This is a test product for integration tests",
                Price = 199.99m
            };
            
            var productJson = JsonConvert.SerializeObject(productData);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            // Verify product was created
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var productResponseContent = await productResponse.Content.ReadAsStringAsync();
            var product = JsonConvert.DeserializeObject<Product>(productResponseContent);
            product.Should().NotBeNull();
            
            // Remove auth for client operations
            RemoveAuthentication();
            
            // Act - Create negotiation with email only
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 150.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            // Assert
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var negotiationResponseContent = await negotiationResponse.Content.ReadAsStringAsync();
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(negotiationResponseContent);
            
            negotiation.Should().NotBeNull();
            negotiation!.ClientEmail.Should().Be(TestClientEmail);
            negotiation.ClientIdentifier.Should().BeEmpty(); // No client identifier was provided, but API returns empty string instead of null
        }

        [Fact]
        public async Task CreateNegotiation_WithEmailAndIdentifier_ShouldCreateNegotiation()
        {
            // Arrange
            RemoveAuthentication();
            SetClientIdentifier(TestClientIdentifier);
            
            // Create a product first
            AuthenticateAsAdmin();
            
            // Create a new product using direct HTTP call
            var productJson = JsonConvert.SerializeObject(TestModels.Products.CreateProduct);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            // Verify product was created
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            product.Should().NotBeNull();
            
            // Remove auth for client operations
            RemoveAuthentication();
            
            // Act - Create negotiation with both email and client identifier
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 150.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            // Assert
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(await negotiationResponse.Content.ReadAsStringAsync());
            
            negotiation.Should().NotBeNull();
            negotiation!.ClientEmail.Should().Be(TestClientEmail);
            negotiation.ClientIdentifier.Should().Be(TestClientIdentifier);
        }

        [Fact]
        public async Task GetClientNegotiations_WithEmail_ShouldReturnNegotiations()
        {
            // Arrange
            RemoveAuthentication();
            RemoveClientIdentifier();
            
            // Create a product first
            AuthenticateAsAdmin();
            
            // Create a new product using direct HTTP call
            var productData = new
            {
                Name = "Client Negotiation Test Product",
                Description = "Product for testing client negotiations",
                Price = 299.99m
            };
            
            var productJson = JsonConvert.SerializeObject(productData);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            
            // Remove auth for client operations
            RemoveAuthentication();
            
            // Create a negotiation
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 250.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Email Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            // Act - Get client negotiations using email
            var response = await Client.GetAsync($"/api/negotiations/client?email={TestClientEmail}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var negotiations = JsonConvert.DeserializeObject<List<Negotiation>>(responseContent);
            
            negotiations.Should().NotBeNull();
            negotiations.Should().Contain(n => n.ClientEmail == TestClientEmail);
        }

        [Fact]
        public async Task GetClientNegotiations_WithIdentifier_ShouldReturnNegotiations()
        {
            // Arrange
            RemoveAuthentication();
            SetClientIdentifier(TestClientIdentifier);
            
            // Create a product first
            AuthenticateAsAdmin();
            
            // Create a new product using direct HTTP call
            var productData = new
            {
                Name = "Identifier Test Product",
                Description = "Product for testing client identifier",
                Price = 399.99m
            };
            
            var productJson = JsonConvert.SerializeObject(productData);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            product.Should().NotBeNull();
            
            // Remove auth for client operations
            RemoveAuthentication();
            
            // Create a negotiation
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 350.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Identifier Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            // Act - Get client negotiations using client identifier
            var response = await Client.GetAsync("/api/negotiations/client");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var negotiations = JsonConvert.DeserializeObject<List<Negotiation>>(responseContent);
            
            negotiations.Should().NotBeNull();
            negotiations.Should().Contain(n => n.ClientIdentifier == TestClientIdentifier);
        }

        [Fact]
        public async Task GetNegotiation_WithEmail_ShouldReturnNegotiation()
        {
            // Arrange
            RemoveAuthentication();
            RemoveClientIdentifier();
            
            // Create a product first
            AuthenticateAsAdmin();
            
            // Create a new product using direct HTTP call
            var productData = new
            {
                Name = "Email Test Product",
                Description = "Product for testing email authentication",
                Price = 299.99m
            };
            
            var productJson = JsonConvert.SerializeObject(productData);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            // Verify product was created
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            product.Should().NotBeNull();
            
            // Remove auth for client operations
            RemoveAuthentication();
            
            // Create a negotiation
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 250.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Email Auth Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(await negotiationResponse.Content.ReadAsStringAsync());
            negotiation.Should().NotBeNull();
            
            var response = await Client.GetAsync($"/api/negotiations/{negotiation!.Id}?email={TestClientEmail}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var receivedNegotiation = JsonConvert.DeserializeObject<Negotiation>(await response.Content.ReadAsStringAsync());
            receivedNegotiation.Should().NotBeNull();
            receivedNegotiation!.Id.Should().Be(negotiation.Id);
        }

        [Fact]
        public async Task ProposeNewPrice_WithEmailAuth_ShouldUpdatePrice()
        {
            AuthenticateAsAdmin();
            
            // Create a product
            var productData = new
            {
                Name = "Price Update Test Product",
                Description = "Product for testing price updates",
                Price = 349.99m
            };
            
            var productJson = JsonConvert.SerializeObject(productData);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            product.Should().NotBeNull();
            
            // Create a negotiation as client
            RemoveAuthentication();
            RemoveClientIdentifier();
            
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 300.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Price Update Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(await negotiationResponse.Content.ReadAsStringAsync());
            negotiation.Should().NotBeNull();
            
            AuthenticateAsAdmin();
            
            var rejectData = new
            {
                IsAccepted = false,
                Comment = "Please offer a higher price."
            };
            
            var rejectJson = JsonConvert.SerializeObject(rejectData);
            var rejectContent = new StringContent(rejectJson, Encoding.UTF8, "application/json");
            var rejectResponse = await Client.PostAsync($"/api/negotiations/{negotiation!.Id}/respond", rejectContent);
            
            rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            RemoveAuthentication();
            RemoveClientIdentifier();
            decimal newProposedPrice = 320.00m;
            
            // Act
            var newPriceData = new
            {
                ProposedPrice = newProposedPrice
            };
            
            var newPriceJson = JsonConvert.SerializeObject(newPriceData);
            var newPriceContent = new StringContent(newPriceJson, Encoding.UTF8, "application/json");
            var newPriceResponse = await Client.PostAsync($"/api/negotiations/{negotiation.Id}/propose-new-price?email={TestClientEmail}", newPriceContent);
            
            newPriceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var getResponse = await Client.GetAsync($"/api/negotiations/{negotiation.Id}?email={TestClientEmail}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedNegotiationJson = await getResponse.Content.ReadAsStringAsync();
            var updatedNegotiation = JsonConvert.DeserializeObject<Negotiation>(updatedNegotiationJson);
            
            updatedNegotiation!.ProposedPrice.Should().Be(newProposedPrice);
            updatedNegotiation.Status.Should().Be(NegotiationStatus.Pending);
        }

        [Fact]
        public async Task GetNegotiation_WithWrongEmailAuth_ShouldReturnForbidden()
        {
            RemoveAuthentication();
            
            AuthenticateAsAdmin();
            
            var productData = new
            {
                Name = "Forbidden Test Product",
                Description = "Product for testing forbidden access",
                Price = 199.99m
            };
            
            var productJson = JsonConvert.SerializeObject(productData);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            product.Should().NotBeNull();
            
            RemoveAuthentication();
            var negotiationData = new
            {
                ProductId = product!.Id,
                ProposedPrice = 150.00m,
                ClientEmail = TestClientEmail,
                ClientName = "Forbidden Test Client"
            };
            
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(await negotiationResponse.Content.ReadAsStringAsync());
            negotiation.Should().NotBeNull();
            
            var response = await Client.GetAsync($"/api/negotiations/{negotiation!.Id}?email=wrong@example.com");

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
