using FluentAssertions;
using Negotiations.Models;
using NegotiationsApi.IntegrationTests.Models;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace NegotiationsApi.IntegrationTests.Tests
{
    public class EmailIdentificationTests : TestFixture
    {
        private const string TestEmail1 = "client1@example.com";
        private const string TestEmail2 = "client2@example.com";
        private const string TestClientIdentifier = "client-xyz-123";

        public EmailIdentificationTests()
        {
        }

        [Fact]
        public async Task CreateMultipleNegotiations_WithSameEmail_ShouldAssociateThem()
        {
            // Arrange
            AuthenticateAsAdmin();
            
            // Create products
            var product1Json = JsonConvert.SerializeObject(TestModels.Products.CreateProduct);
            var product1Content = new StringContent(product1Json, Encoding.UTF8, "application/json");
            var product1Response = await Client.PostAsync("/api/products", product1Content);
            product1Response.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create first product");
            var product1 = JsonConvert.DeserializeObject<Product>(await product1Response.Content.ReadAsStringAsync());
            
            var product2 = new { 
                Name = "Second Test Product", 
                Description = "Another test product", 
                Price = 299.99m 
            };
            var product2Json = JsonConvert.SerializeObject(product2);
            var product2Content = new StringContent(product2Json, Encoding.UTF8, "application/json");
            var product2Response = await Client.PostAsync("/api/products", product2Content);
            product2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create second product");
            var product2Result = JsonConvert.DeserializeObject<Product>(await product2Response.Content.ReadAsStringAsync());
            
            RemoveAuthentication();
            
            // Act - Create negotiations for both products with same email
            var negotiation1Data = TestModels.Negotiations.CreateNegotiation(product1!.Id, TestEmail1);
            var negotiation1Json = JsonConvert.SerializeObject(negotiation1Data);
            var negotiation1Content = new StringContent(negotiation1Json, Encoding.UTF8, "application/json");
            var negotiation1Response = await Client.PostAsync("/api/negotiations", negotiation1Content);
            negotiation1Response.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create first negotiation");
            var negotiation1 = JsonConvert.DeserializeObject<Negotiation>(await negotiation1Response.Content.ReadAsStringAsync());
            
            var negotiation2Data = TestModels.Negotiations.CreateNegotiation(product2Result!.Id, TestEmail1);
            var negotiation2Json = JsonConvert.SerializeObject(negotiation2Data);
            var negotiation2Content = new StringContent(negotiation2Json, Encoding.UTF8, "application/json");
            var negotiation2Response = await Client.PostAsync("/api/negotiations", negotiation2Content);
            negotiation2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create second negotiation");
            var negotiation2 = JsonConvert.DeserializeObject<Negotiation>(await negotiation2Response.Content.ReadAsStringAsync());
            
            // Get all negotiations for this client
            var response = await Client.GetAsync($"/api/negotiations/client?email={TestEmail1}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var negotiations = JsonConvert.DeserializeObject<List<Negotiation>>(responseContent);
            negotiations.Should().NotBeNull();
            // There might be more negotiations for this email from previous test runs
            negotiations!.Should().Contain(n => n.Id == negotiation1!.Id);
            negotiations.Should().Contain(n => n.Id == negotiation2!.Id);
        }
        
        [Fact]
        public async Task DualIdentification_BothMethodsWork()
        {
            // Arrange
            AuthenticateAsAdmin();
            var productJson = JsonConvert.SerializeObject(TestModels.Products.CreateProduct);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create product");
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            
            RemoveAuthentication();
            SetClientIdentifier(TestClientIdentifier);
            
            // Create negotiation with both identifiers
            var negotiationData = TestModels.Negotiations.CreateNegotiation(product!.Id, TestEmail1);
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create negotiation");
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(await negotiationResponse.Content.ReadAsStringAsync());
            
            // Get negotiation using email (removing client identifier header)
            RemoveClientIdentifier();
            var emailResponse = await Client.GetAsync($"/api/negotiations/{negotiation!.Id}?email={TestEmail1}");
            
            // Get same negotiation using client identifier
            SetClientIdentifier(TestClientIdentifier);
            var identifierResponse = await Client.GetAsync($"/api/negotiations/{negotiation!.Id}");
            
            // Assert
            emailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            identifierResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var negotiation1 = JsonConvert.DeserializeObject<Negotiation>(await emailResponse.Content.ReadAsStringAsync());
            var negotiation2 = JsonConvert.DeserializeObject<Negotiation>(await identifierResponse.Content.ReadAsStringAsync());
            
            negotiation1!.Id.Should().Be(negotiation2!.Id);
        }
        
        [Fact]
        public async Task MultipleUsers_CanHaveDistinctNegotiations()
        {
            // Arrange
            AuthenticateAsAdmin();
            var productJson = JsonConvert.SerializeObject(TestModels.Products.CreateProduct);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create product");
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            
            RemoveAuthentication();
            
            // Create negotiation for first client
            var negotiation1Data = TestModels.Negotiations.CreateNegotiation(product!.Id, TestEmail1);
            var negotiation1Json = JsonConvert.SerializeObject(negotiation1Data);
            var negotiation1Content = new StringContent(negotiation1Json, Encoding.UTF8, "application/json");
            var negotiation1Response = await Client.PostAsync("/api/negotiations", negotiation1Content);
            negotiation1Response.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create first negotiation");
            var negotiation1 = JsonConvert.DeserializeObject<Negotiation>(await negotiation1Response.Content.ReadAsStringAsync());
            
            // Create negotiation for second client
            var negotiation2Data = TestModels.Negotiations.CreateNegotiation(product!.Id, TestEmail2);
            var negotiation2Json = JsonConvert.SerializeObject(negotiation2Data);
            var negotiation2Content = new StringContent(negotiation2Json, Encoding.UTF8, "application/json");
            var negotiation2Response = await Client.PostAsync("/api/negotiations", negotiation2Content);
            negotiation2Response.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create second negotiation");
            var negotiation2 = JsonConvert.DeserializeObject<Negotiation>(await negotiation2Response.Content.ReadAsStringAsync());
            
            // Get negotiations for first client
            var response1 = await Client.GetAsync($"/api/negotiations/client?email={TestEmail1}");
            var negotiations1 = JsonConvert.DeserializeObject<List<Negotiation>>(await response1.Content.ReadAsStringAsync());
            
            // Get negotiations for second client
            var response2 = await Client.GetAsync($"/api/negotiations/client?email={TestEmail2}");
            var negotiations2 = JsonConvert.DeserializeObject<List<Negotiation>>(await response2.Content.ReadAsStringAsync());
            
            // Assert
            negotiations1.Should().NotBeNull();
            negotiations2.Should().NotBeNull();
            
            // There might be more negotiations for this email from previous test runs
            // So instead of checking counts, check that the specific negotiations we created are present
            negotiations1.Should().Contain(n => n.Id == negotiation1!.Id);
            negotiations2.Should().Contain(n => n.Id == negotiation2!.Id);
            
            // Verify our newly created negotiations have the correct email addresses
            var newNeg1 = negotiations1!.First(n => n.Id == negotiation1!.Id);
            var newNeg2 = negotiations2!.First(n => n.Id == negotiation2!.Id);
            
            newNeg1.ClientEmail.Should().Be(TestEmail1);
            newNeg2.ClientEmail.Should().Be(TestEmail2);
        }
        
        [Fact]
        public async Task GetNegotiation_WithBothIdentifiers_SucceedsEvenWithoutHeaderIfEmailProvided()
        {
            // Arrange
            AuthenticateAsAdmin();
            var productJson = JsonConvert.SerializeObject(TestModels.Products.CreateProduct);
            var productContent = new StringContent(productJson, Encoding.UTF8, "application/json");
            var productResponse = await Client.PostAsync("/api/products", productContent);
            productResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create product");
            var product = JsonConvert.DeserializeObject<Product>(await productResponse.Content.ReadAsStringAsync());
            
            RemoveAuthentication();
            SetClientIdentifier(TestClientIdentifier);
            
            // Create negotiation with both identifiers
            var negotiationData = TestModels.Negotiations.CreateNegotiation(product!.Id, TestEmail1);
            var negotiationJson = JsonConvert.SerializeObject(negotiationData);
            var negotiationContent = new StringContent(negotiationJson, Encoding.UTF8, "application/json");
            var negotiationResponse = await Client.PostAsync("/api/negotiations", negotiationContent);
            negotiationResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Failed to create negotiation");
            var negotiation = JsonConvert.DeserializeObject<Negotiation>(await negotiationResponse.Content.ReadAsStringAsync());
            
            // Remove Client-Identifier header
            RemoveClientIdentifier();
            
            // Act - Access with email only 
            var response = await Client.GetAsync($"/api/negotiations/{negotiation!.Id}?email={TestEmail1}");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseContent = await response.Content.ReadAsStringAsync();
            var retrievedNegotiation = JsonConvert.DeserializeObject<Negotiation>(responseContent);
            retrievedNegotiation.Should().NotBeNull();
            retrievedNegotiation!.Id.Should().Be(negotiation.Id);
        }
    }
}
