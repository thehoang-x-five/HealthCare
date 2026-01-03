# Cấu hình Tính năng Tự động Reset Hàng ngày

## Cấu hình mặc định

Service chạy với cấu hình mặc định:
- **23:59** - Hủy phiếu khám chưa hoàn thành
- **00:00** - Reset trạng thái hôm nay

## Tùy chỉnh thời gian (Tùy chọn)

Nếu muốn thay đổi thời gian chạy, bạn có thể:

### Option 1: Thêm cấu hình vào appsettings.json

```json
{
  "DailyReset": {
    "CancelTime": "23:59",
    "ResetTime": "00:00"
  }
}
```

Sau đó cập nhật `DailyResetService.cs`:

```csharp
private readonly IConfiguration _configuration;

public DailyResetService(
    IServiceProvider serviceProvider,
    ILogger<DailyResetService> logger,
    IConfiguration configuration)
{
    _serviceProvider = serviceProvider;
    _logger = logger;
    _configuration = configuration;
}

private DateTime CalculateNextRunTime(DateTime now)
{
    var cancelTime = _configuration["DailyReset:CancelTime"] ?? "23:59";
    var resetTime = _configuration["DailyReset:ResetTime"] ?? "00:00";
    
    // Parse và tính toán...
}
```

### Option 2: Sử dụng Cron Expression (Nâng cao)

Cài đặt package:
```bash
dotnet add package Cronos
```

Cập nhật service để sử dụng cron:
```csharp
using Cronos;

private readonly CronExpression _cancelCron = CronExpression.Parse("59 23 * * *");
private readonly CronExpression _resetCron = CronExpression.Parse("0 0 * * *");
```

## Tắt tính năng

Nếu muốn tắt tính năng tự động reset, comment dòng này trong `Program.cs`:

```csharp
// builder.Services.AddHostedService<DailyResetService>();
```

## Chạy thủ công (Testing)

Để test tính năng mà không cần đợi đến 23:59 hoặc 00:00:

### 1. Tạo API endpoint test

Thêm vào `Controllers/AdminController.cs`:

```csharp
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly DataContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(DataContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("test-cancel-unfinished")]
    public async Task<IActionResult> TestCancelUnfinished()
    {
        // Copy logic từ CancelUnfinishedExamsAsync()
        // ...
        return Ok("Đã hủy phiếu khám chưa hoàn thành");
    }

    [HttpPost("test-reset-daily-status")]
    public async Task<IActionResult> TestResetDailyStatus()
    {
        // Copy logic từ ResetDailyStatusAsync()
        // ...
        return Ok("Đã reset trạng thái hôm nay");
    }
}
```

### 2. Sử dụng Swagger hoặc Postman

```bash
POST https://localhost:7001/api/admin/test-cancel-unfinished
POST https://localhost:7001/api/admin/test-reset-daily-status
```

## Monitoring

### Xem log trong Development

```bash
# Windows
type logs\app.log | findstr "DailyResetService"

# Linux/Mac
tail -f logs/app.log | grep "DailyResetService"
```

### Xem log trong Production

Sử dụng logging provider như:
- **Serilog** → File, Elasticsearch, Seq
- **NLog** → File, Database
- **Application Insights** → Azure

Ví dụ với Serilog:

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .WriteTo.Console()
        .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
        .MinimumLevel.Information();
});
```

## Troubleshooting

### Service không chạy

**Kiểm tra:**
1. Service đã được đăng ký trong `Program.cs`?
2. Ứng dụng có đang chạy liên tục?
3. Xem log có lỗi gì không?

**Giải pháp:**
```csharp
// Thêm log chi tiết hơn
_logger.LogInformation("DailyResetService starting at {Time}", DateTime.Now);
```

### Service chạy nhưng không hủy phiếu

**Kiểm tra:**
1. Có bệnh nhân nào thỏa điều kiện không?
2. Database connection có ổn không?
3. Transaction có bị rollback không?

**Debug:**
```csharp
var unfinishedPatients = await db.BenhNhans
    .Where(b =>
        !string.IsNullOrEmpty(b.TrangThaiHomNay) &&
        b.TrangThaiHomNay != "da_hoan_tat" &&
        b.NgayTrangThai == today)
    .ToListAsync();

_logger.LogInformation("Found {Count} unfinished patients: {Patients}",
    unfinishedPatients.Count,
    string.Join(", ", unfinishedPatients.Select(p => p.MaBenhNhan)));
```

### Performance issues

**Nếu có quá nhiều bản ghi:**

1. **Batch processing:**
```csharp
var batchSize = 1000;
var skip = 0;

while (true)
{
    var batch = await db.PhieuKhamLamSangs
        .Where(/* conditions */)
        .Skip(skip)
        .Take(batchSize)
        .ToListAsync();

    if (!batch.Any()) break;

    // Process batch
    await db.SaveChangesAsync();
    skip += batchSize;
}
```

2. **Index database:**
```sql
CREATE INDEX IX_BenhNhan_TrangThaiHomNay_NgayTrangThai 
ON benh_nhan (TrangThaiHomNay, NgayTrangThai);

CREATE INDEX IX_PhieuKhamLamSang_NgayLap_TrangThai 
ON phieu_kham_lam_sang (NgayLap, TrangThai);
```

## Best Practices

### 1. Backup trước khi deploy

```bash
# MySQL
mysqldump -u root -p healthcare > backup_$(date +%Y%m%d).sql

# SQL Server
BACKUP DATABASE HealthCare TO DISK = 'C:\Backup\HealthCare.bak'
```

### 2. Test trên môi trường staging trước

```bash
# Chạy test trên staging
dotnet run --environment Staging
```

### 3. Monitor sau khi deploy

- Kiểm tra log hàng ngày
- Theo dõi số lượng phiếu bị hủy
- Đảm bảo không có lỗi

### 4. Thông báo cho team

Gửi email/notification khi:
- Service bắt đầu chạy
- Có lỗi xảy ra
- Số lượng phiếu bị hủy bất thường

```csharp
if (canceledLs > 100) // Ngưỡng cảnh báo
{
    await _emailService.SendAlertAsync(
        "admin@hospital.com",
        "Cảnh báo: Số phiếu khám bị hủy cao bất thường",
        $"Đã hủy {canceledLs} phiếu khám lâm sàng");
}
```

## FAQ

**Q: Service có chạy khi server restart không?**
A: Có, service tự động khởi động cùng ứng dụng.

**Q: Nếu server tắt lúc 23:59 thì sao?**
A: Khi server bật lại, service sẽ tính toán lần chạy tiếp theo. Dữ liệu của ngày hôm trước sẽ không được xử lý.

**Q: Có thể chạy service trên nhiều server không?**
A: Có thể, nhưng cần cơ chế distributed lock để tránh chạy trùng lặp.

**Q: Làm sao để khôi phục dữ liệu bị hủy nhầm?**
A: Restore từ backup. Nên có chiến lược backup định kỳ.

**Q: Service có ảnh hưởng đến performance không?**
A: Không, service chạy bulk operations và chỉ chạy 2 lần/ngày.

---

## Liên hệ

Nếu có vấn đề, liên hệ:
- **Email:** dev@hospital.com
- **Slack:** #healthcare-dev
- **Phone:** 0123-456-789
