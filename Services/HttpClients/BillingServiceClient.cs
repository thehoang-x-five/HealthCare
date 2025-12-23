using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HealthCare.DTOs;
using Microsoft.AspNetCore.Http;

namespace HealthCare.Services.HttpClients
{
    /// <summary>
    /// HTTP Client để gọi Billing Service APIs
    /// Tạo hóa đơn thanh toán
    /// </summary>
    public interface IBillingServiceClient
    {
        Task<InvoiceDto?> CreateInvoiceAsync(InvoiceCreateRequest request);
        Task<InvoiceDto?> GetInvoiceAsync(string maHoaDon);
    }

    public class BillingServiceClient : IBillingServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BillingServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<InvoiceDto?> CreateInvoiceAsync(InvoiceCreateRequest request)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.PostAsJsonAsync("/api/billing", request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create invoice: {response.StatusCode} - {error}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<InvoiceDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling BillingService.CreateInvoice: {ex.Message}");
                return null;
            }
        }

        public async Task<InvoiceDto?> GetInvoiceAsync(string maHoaDon)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.GetAsync($"/api/billing/{maHoaDon}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<InvoiceDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling BillingService.GetInvoice: {ex.Message}");
                return null;
            }
        }

        private void ForwardAuthToken()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", token);
            }
        }
    }
}
