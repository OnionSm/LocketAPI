using MongoExample.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Cấu hình MongoDBSettings từ appsettings.json
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

// Đăng ký MongoClient với ConnectionString từ MongoDBSettings
builder.Services.AddSingleton<IMongoClient>(s =>
{
    var settings = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionURI);
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));



// Đăng ký IMongoDatabase
builder.Services.AddScoped(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    var settings = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return client.GetDatabase(settings.DatabaseName);
});



// Đăng ký Service
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<UserConversationService>();
builder.Services.AddScoped<UserFriendRequestService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<JwtService>();


DotNetEnv.Env.Load(); // Đọc tệp .env

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Cấu hình yêu cầu metadata HTTPS (bỏ qua nếu phát triển)
        options.RequireHttpsMetadata = false; 

        // Lấy giá trị Issuer, Audience và SecretKey từ cấu hình
        var issuer = builder.Configuration["JwtSettings:Issuer"];
        var audience = builder.Configuration["JwtSettings:Audience"];
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? builder.Configuration["JwtSettings:SecretKey"];
        var tokenLifespan = builder.Configuration.GetValue<int>("JwtSettings:TokenLifespan");

        // Kiểm tra nếu secretKey không có giá trị
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT secret key is missing.");
        }

        // Cấu hình các tham số xác thực token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Kiểm tra Issuer trong token
            ValidateLifetime = true, // Kiểm tra thời gian sống của token (exp)
            ValidIssuer = issuer, // Thiết lập Issuer hợp lệ
            ValidAudience = audience, // Thiết lập Audience hợp lệ
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), // Thiết lập key để ký và xác thực token
            ClockSkew = TimeSpan.Zero // Tùy chọn, có thể điều chỉnh nếu muốn kiểm soát độ lệch thời gian giữa các máy chủ
        };

        // Cấu hình thời gian sống của token
        options.SaveToken = true; // Lưu token vào HttpContext
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
    policy =>
    {
        policy.AllowAnyOrigin()    // Cho phép tất cả các nguồn
              .AllowAnyHeader()    // Cho phép tất cả các header
              .AllowAnyMethod();   // Cho phép tất cả các phương thức HTTP (GET, POST, PUT, DELETE, v.v.)
    });
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddControllers();


var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");

// Ánh xạ các Controller
app.MapControllers();

// Ánh xạ các Hub
app.MapHub<ChatHub>("/chathub"); 


app.Run();
