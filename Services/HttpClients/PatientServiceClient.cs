using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HealthCare.DTOs;

namespace HealthCare.Services.HttpClients
{
    /// <summary>
    /// HTTP Client để gọi Patient Management Service APIs
    /// Thay thế database joins với HTTP calls theo nguyên tắc SOA
    /// </summary>
    public interface IPatientServiceClient
    {
        Task<PatientDetailDto?> GetPatientAsync(string maBenhNhan);
        Task<PatientDto?> GetPatientBasicAsync(string maBenhNhan);
    }

    public class PatientServiceClient : IPatientServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PatientServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PatientDetailDto?> GetPatientAsync(string maBenhNhan)
        {
            try
            {
                // Forward JWT token từ current request
                ForwardAuthToken();

                var response = await _httpClient.GetAsync($"/api/patients/{maBenhNhan}");
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return null;
                    
                    throw new HttpRequestException($"Failed to get patient: {response.StatusCode}");
                }

                return await response.Content.ReadFromJsonAsync<PatientDetailDto>();
            }
            catch (Exception ex)
            {
                // Log error (có thể inject ILogger sau)
                Console.WriteLine($"Error calling PatientService.GetPatient: {ex.Message}");
                throw;
            }
        }

        public async Task<PatientDto?> GetPatientBasicAsync(string maBenhNhan)
        {
            try
            {
                ForwardAuthToken();

                // Gọi endpoint search với filter maBenhNhan để lấy thông tin cơ bản
                var response = await _httpClient.GetAsync($"/api/patients/search?maBenhNhan={maBenhNhan}&page=1&pageSize=1");
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        return null;
                    
                    throw new HttpRequestException($"Failed to get patient basic info: {response.StatusCode}");
                }

                var result = await response.Content.ReadFromJsonAsync<PagedResult<PatientDto>>();
                if (result?.Items == null || result.Items.Count() == 0) return null;
                return result.Items.ToList()[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling PatientService.GetPatientBasic: {ex.Message}");
                throw;
            }
        }

        private void ForwardAuthToken()
        {
            // Forward JWT token từ current HTTP request sang internal HTTP call
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                _httpClient.DefaultRequestHeaders.Add("Authorization", token);
            }
        }
    }
}
