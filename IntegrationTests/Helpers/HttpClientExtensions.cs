using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;

namespace NegotiationsApi.IntegrationTests.Helpers
{
    public static class HttpClientExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        
        public static async Task<(bool Success, TResponse? Response, string? Error)> GetAsync<TResponse>(
            this HttpClient client, string endpoint)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
                    return (true, content, null);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, default, $"Status: {response.StatusCode}, Error: {error}");
                }
            }
            catch (Exception ex)
            {
                return (false, default, $"Exception: {ex.Message}");
            }
        }
        
        public static async Task<(bool Success, TResponse? Response, string? Error, HttpResponseMessage? RawResponse)> PostAsync<TResponse>(
            this HttpClient client, string endpoint, object data)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
                    return (true, responseContent, null, response);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, default, $"Status: {response.StatusCode}, Error: {error}", response);
                }
            }
            catch (Exception ex)
            {
                return (false, default, $"Exception: {ex.Message}", null);
            }
        }
        
        public static async Task<(bool Success, HttpResponseMessage? RawResponse, string? Error)> PostAsyncRaw(
            this HttpClient client, string endpoint, object data)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, response, null);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, response, $"Status: {response.StatusCode}, Error: {error}");
                }
            }
            catch (Exception ex)
            {
                return (false, null, $"Exception: {ex.Message}");
            }
        }

        public static async Task<(bool Success, TResponse? Response, string? Error)> PutAsync<TResponse>(
            this HttpClient client, string endpoint, object data)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                var response = await client.PutAsync(endpoint, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
                    return (true, responseContent, null);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, default, $"Status: {response.StatusCode}, Error: {error}");
                }
            }
            catch (Exception ex)
            {
                return (false, default, $"Exception: {ex.Message}");
            }
        }
        
        public static async Task<bool> DeleteAsync(this HttpClient client, string endpoint)
        {
            try
            {
                var response = await client.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
