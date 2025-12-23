using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HealthCare.DTOs;
using Microsoft.AspNetCore.Http;

namespace HealthCare.Services.HttpClients
{
    /// <summary>
    /// HTTP Client để gọi Report Service APIs
    /// Lấy dữ liệu dashboard và báo cáo
    /// </summary>
    public interface IReportServiceClient
    {
        Task<DashboardTodayDto?> GetTodayDashboardAsync();
        Task<ReportOverviewDto?> GetReportOverviewAsync(ReportFilterRequest filter);
    }

    public class ReportServiceClient : IReportServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DashboardTodayDto?> GetTodayDashboardAsync()
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.GetAsync("/api/dashboard/today");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<DashboardTodayDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling ReportService.GetTodayDashboard: {ex.Message}");
                return null;
            }
        }

        public async Task<ReportOverviewDto?> GetReportOverviewAsync(ReportFilterRequest filter)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.PostAsJsonAsync("/api/reports/overview", filter);
                
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<ReportOverviewDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling ReportService.GetReportOverview: {ex.Message}");
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
