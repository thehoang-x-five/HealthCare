using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HealthCare.DTOs;
using Microsoft.AspNetCore.Http;

namespace HealthCare.Services.HttpClients
{
    /// <summary>
    /// HTTP Client để gọi User Interaction Service APIs
    /// Lấy thông tin user, gửi notifications
    /// </summary>
    public interface IUserInteractionServiceClient
    {
        Task<object?> GetUserAsync(string maNhanVien);
        Task<bool> CreateNotificationAsync(NotificationCreateRequest request);
        Task BroadcastRealtimeAsync(string eventName, object data);
    }

    public class UserInteractionServiceClient : IUserInteractionServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserInteractionServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<object?> GetUserAsync(string maNhanVien)
        {
            try
            {
                ForwardAuthToken();

                // Gọi MasterData API để lấy thông tin nhân viên
                // (User info thực tế nằm trong MasterData service)
                var response = await _httpClient.GetAsync($"/api/masterdata/staff/{maNhanVien}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<object>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling UserInteractionService.GetUser: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateNotificationAsync(NotificationCreateRequest request)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.PostAsJsonAsync("/api/notification", request);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling UserInteractionService.CreateNotification: {ex.Message}");
                return false;
            }
        }

        public async Task BroadcastRealtimeAsync(string eventName, object data)
        {
            try
            {
                ForwardAuthToken();

                // Gọi internal realtime broadcast endpoint (nếu có)
                // Hoặc có thể inject IRealtimeService trực tiếp vì cùng process
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling UserInteractionService.BroadcastRealtime: {ex.Message}");
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
