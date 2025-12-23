using System;
using System.Threading.Tasks;
using HealthCare.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HealthCare.Hubs
{
    /// <summary>
    /// Hub trung tâm cho tất cả realtime (Dashboard, Queue, Notification...).
    /// Chạy kiểu strong-typed với IRealtimeClient.
    /// </summary>
    [Authorize]
    public class RealtimeHub : Hub<IRealtimeClient>
    {
        // ===== ROOMS (Queue) =====

        /// <summary>
        /// Client gọi khi mở màn Hàng đợi của 1 phòng.
        /// </summary>
        public Task JoinRoomAsync(string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("maPhong is required", nameof(maPhong));

            var groupName = GetRoomGroupName(maPhong);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveRoomAsync(string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("maPhong is required", nameof(maPhong));

            var groupName = GetRoomGroupName(maPhong);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // ===== USER (notification, patient/personal data) =====

        public Task JoinUserAsync(string loaiNguoiNhan, string maNguoiNhan)
        {
            if (string.IsNullOrWhiteSpace(loaiNguoiNhan))
                throw new ArgumentException("loaiNguoiNhan is required", nameof(loaiNguoiNhan));
            if (string.IsNullOrWhiteSpace(maNguoiNhan))
                throw new ArgumentException("maNguoiNhan is required", nameof(maNguoiNhan));

            var groupName = GetUserGroupName(loaiNguoiNhan, maNguoiNhan);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveUserAsync(string loaiNguoiNhan, string maNguoiNhan)
        {
            if (string.IsNullOrWhiteSpace(loaiNguoiNhan))
                throw new ArgumentException("loaiNguoiNhan is required", nameof(loaiNguoiNhan));
            if (string.IsNullOrWhiteSpace(maNguoiNhan))
                throw new ArgumentException("maNguoiNhan is required", nameof(maNguoiNhan));

            var groupName = GetUserGroupName(loaiNguoiNhan, maNguoiNhan);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// FE gọi ngay sau khi connect để join group role tương ứng.
        /// - Bác sĩ    → JoinRoleAsync("bac_si")
        /// - Y tá / thu ngân / phát thuốc / hành chính → JoinRoleAsync("y_ta")
        /// </summary>
        public Task JoinRoleAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("role is required", nameof(role));

            var groupName = GetRoleGroupName(role);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveRoleAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("role is required", nameof(role));

            var groupName = GetRoleGroupName(role);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // ✅ Task 15.2 & 15.3: Auto-join role group khi connect dựa trên JWT claims
        public override async Task OnConnectedAsync()
        {
            // ✅ Task 15.3: Auto-join role group dựa trên ChucVu claim từ JWT
            var chucVuClaim = Context.User?.FindFirst("ChucVu")?.Value;
            
            if (!string.IsNullOrWhiteSpace(chucVuClaim))
            {
                var chucVu = chucVuClaim.ToLowerInvariant();
                
                // Map ChucVu to role group
                string? roleGroup = chucVu switch
                {
                    "bac_si" => "bac_si",
                    "y_ta_hanh_chinh" or "y_ta_phong_kham" => "y_ta",
                    "ky_thuat_vien" => "y_ta", // Kỹ thuật viên cũng thuộc nhóm y_ta
                    "admin" => "bac_si", // Admin có thể xem như bác sĩ
                    _ => null
                };

                if (roleGroup != null)
                {
                    var groupName = GetRoleGroupName(roleGroup);
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                }
            }

            // ✅ Task 15.3: Auto-join user group dựa trên MaNhanVien claim
            var maNhanVienClaim = Context.User?.FindFirst("MaNhanVien")?.Value;
            var loaiNguoiDung = "nhan_vien_y_te"; // Default
            
            if (!string.IsNullOrWhiteSpace(maNhanVienClaim))
            {
                var userGroupName = GetUserGroupName(loaiNguoiDung, maNhanVienClaim);
                await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Groups are automatically cleaned up when connection is closed
            return base.OnDisconnectedAsync(exception);
        }

        // ===== Helper build group name =====

        public static string GetRoomGroupName(string maPhong)
            => $"room:{maPhong}";

        public static string GetUserGroupName(string loaiNguoiNhan, string maNguoiNhan)
            => $"user:{loaiNguoiNhan}:{maNguoiNhan}";

        public static string GetRoleGroupName(string role)
            => $"role:{role}";
    }
}
