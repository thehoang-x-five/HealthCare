using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using HealthCare.DTOs;
using Microsoft.AspNetCore.Http;

namespace HealthCare.Services.HttpClients
{
    /// <summary>
    /// HTTP Client để gọi Master Data Service APIs
    /// Lấy thông tin khoa, phòng, dịch vụ, nhân sự
    /// </summary>
    public interface IMasterDataServiceClient
    {
        Task<DepartmentDto?> GetDepartmentAsync(string maKhoa);
        Task<RoomDto?> GetRoomAsync(string maPhong);
        Task<ServiceDetailInfoDto?> GetServiceAsync(string maDichVu);
        Task<IReadOnlyList<ServiceDto>> GetServicesByTypeAsync(string? loaiDichVu = null);
        Task<StaffDetailDto?> GetStaffAsync(string maNhanVien);
    }

    public class MasterDataServiceClient : IMasterDataServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MasterDataServiceClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DepartmentDto?> GetDepartmentAsync(string maKhoa)
        {
            try
            {
                ForwardAuthToken();

                // Gọi search API với filter
                var response = await _httpClient.GetAsync($"/api/masterdata/departments/search?maKhoa={maKhoa}&page=1&pageSize=1");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<PagedResult<DepartmentDto>>();
                if (result?.Items == null || result.Items.Count() == 0) return null;
                return result.Items.ToList()[0];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling MasterDataService.GetDepartment: {ex.Message}");
                return null;
            }
        }

        public async Task<RoomDto?> GetRoomAsync(string maPhong)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.GetAsync($"/api/masterdata/rooms/{maPhong}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<RoomDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling MasterDataService.GetRoom: {ex.Message}");
                return null;
            }
        }

        public async Task<ServiceDetailInfoDto?> GetServiceAsync(string maDichVu)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.GetAsync($"/api/masterdata/services/{maDichVu}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                // Return ServiceDetailInfoDto directly (it contains all ServiceDto fields)
                return await response.Content.ReadFromJsonAsync<ServiceDetailInfoDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling MasterDataService.GetService: {ex.Message}");
                return null;
            }
        }

        public async Task<IReadOnlyList<ServiceDto>> GetServicesByTypeAsync(string? loaiDichVu = null)
        {
            try
            {
                ForwardAuthToken();

                var url = "/api/masterdata/services";
                if (!string.IsNullOrEmpty(loaiDichVu))
                    url += $"?loaiDichVu={loaiDichVu}";

                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    return Array.Empty<ServiceDto>();

                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ServiceDto>>();
                return result ?? Array.Empty<ServiceDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling MasterDataService.GetServicesByType: {ex.Message}");
                return Array.Empty<ServiceDto>();
            }
        }

        public async Task<StaffDetailDto?> GetStaffAsync(string maNhanVien)
        {
            try
            {
                ForwardAuthToken();

                var response = await _httpClient.GetAsync($"/api/masterdata/staff/{maNhanVien}");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                // Return StaffDetailDto directly (it contains all StaffDto fields)
                return await response.Content.ReadFromJsonAsync<StaffDetailDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calling MasterDataService.GetStaff: {ex.Message}");
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
