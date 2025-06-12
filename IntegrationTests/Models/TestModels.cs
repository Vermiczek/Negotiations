using Negotiations.Models.DTOs;

namespace NegotiationsApi.IntegrationTests.Models
{
    public static class TestModels
    {
        public static class Users
        {
            public static LoginRequest AdminUser => new LoginRequest 
            { 
                Username = "admin", 
                Password = "Admin123!" 
            };
            
            public static LoginRequest SellerUser => new LoginRequest 
            { 
                Username = "seller", 
                Password = "Seller123!" 
            };
        }
        
        public static class Products
        {
            public static object CreateProduct => new 
            { 
                Name = "Test Product", 
                Description = "This is a test product for integration tests", 
                Price = 199.99m 
            };
            
            public static object UpdateProduct => new 
            { 
                Name = "Updated Test Product", 
                Description = "Updated product description", 
                Price = 249.99m 
            };
        }
        
        public static class Negotiations
        {
            public static object CreateNegotiation(int productId, string email = "testclient@example.com") => new 
            { 
                ProductId = productId, 
                ProposedPrice = 150.00m, 
                ClientEmail = email, 
                ClientName = "Test Client" 
            };
            
            public static object ProposeNewPrice(decimal price = 175.00m) => new 
            { 
                ProposedPrice = price 
            };
            
            public static object AcceptNegotiation => new 
            { 
                IsAccepted = true, 
                Comment = "Your price is acceptable." 
            };
            
            public static object RejectNegotiation => new 
            { 
                IsAccepted = false, 
                Comment = "Please offer a higher price." 
            };
        }
    }
}
