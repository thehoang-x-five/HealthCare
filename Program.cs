using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Builder;
using HealthCare.Datas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthCare.Hubs;
using HealthCare.Realtime;
using HealthCare.DTOs;
// SOA Service Modules
using HealthCare.Services.UserInteraction;
using HealthCare.Services.MasterData;
using HealthCare.Services.PatientManagement;
using HealthCare.Services.OutpatientCare;
using HealthCare.Services.MedicationBilling;
using HealthCare.Services.Report;
using HealthCare.Services.HttpClients;





var builder = WebApplication.CreateBuilder(args);

// Nếu đang dùng SQL Server -> giữ UseSqlServer + "DefaultConnection" (đồng bộ appsettings)
//builder.Services.AddDbContext<DataContext>(opt =>
//    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));






builder.Services.AddDbContext<DataContext>(opt =>
    opt.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));



builder.Services.AddMemoryCache();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<OtpOptions>(builder.Configuration.GetSection("Otp"));

// ===== HTTP Clients for SOA Communication =====
builder.Services.AddHttpClient<IPatientServiceClient, PatientServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:BaseUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IMasterDataServiceClient, MasterDataServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:BaseUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IBillingServiceClient, BillingServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:BaseUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IUserInteractionServiceClient, UserInteractionServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:BaseUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IReportServiceClient, ReportServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:BaseUrl"] ?? "https://localhost:7001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ===== Services =====
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IRealtimeService, RealtimeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IMasterDataService, MasterDataService>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IClinicalService, ClinicalService>();
builder.Services.AddScoped<IClsService, ClsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IQueueService, QueueService>();


// ===== Controllers & JSON =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = null; // giữ nguyên casing DTO
});

// ===== CORS =====
const string CorsPolicy = "Frontend";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(CorsPolicy, p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>()!)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});



// ===== Auth: JWT Bearer (kèm hỗ trợ SignalR qua access_token query) =====
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;

                // Cho SignalR Hub: /hubs/realtime
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/realtime"))
                {
                    ctx.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
        
    });

builder.Services.AddAuthorization();

// ===== SignalR (nếu dùng) =====
builder.Services.AddSignalR();
// Scale-out (tuỳ chọn):
// builder.Services.AddSignalR().AddStackExchangeRedis("localhost:6379");

// ===== Swagger (Bearer) =====
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập: Bearer <token>",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
// ===== Seed dữ liệu demo vào DB (chỉ chạy khi DB đang trống) =====

 using (var scope = app.Services.CreateScope())
 {
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();
    db.Database.Migrate();
    await DataSeed.EnsureSeedAsync(db);
 }

// ===== Middleware pipeline =====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RealtimeHub>("/hubs/realtime");
app.Run();
