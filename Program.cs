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

// Đăng ký IMongoDatabase
builder.Services.AddScoped(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    var settings = s.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return client.GetDatabase(settings.DatabaseName);
});

// Đăng ký UserService
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<UserConversationService>();
builder.Services.AddScoped<UserFriendRequestService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<UserSessionService>();


// Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;  // Nếu đang phát triển, có thể bỏ qua HTTPS
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,  // Kiểm tra Issuer
            ValidateLifetime = true,  // Kiểm tra tuổi thọ của token
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],  // Lấy Issuer từ cấu hình
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))  // Lấy Key từ cấu hình
        };
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
// Đăng ký Controller
builder.Services.AddControllers();


var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");

// Ánh xạ các Controller
app.MapControllers();
app.MapHub<ChatHub>("/chathub"); 

app.MapGet("/", () => "Hello Onion");

app.Run();
