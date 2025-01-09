using MongoExample.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Đăng ký MongoClient với ConnectionString từ MongoDBSettings
builder.Services.AddSingleton<IMongoClient>(s =>
{
    var connection_url = Environment.GetEnvironmentVariable("MongoDBConnectionURI");
    return new MongoClient(connection_url);
});


// Đăng ký IMongoDatabase
builder.Services.AddScoped(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    var database_name = Environment.GetEnvironmentVariable("DatabaseName");
    return client.GetDatabase(database_name);
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
builder.Services.AddScoped<DeletedUserService>();
builder.Services.AddScoped<FeedbackService>();
builder.Services.AddScoped<FriendService>();
builder.Services.AddScoped<StoryService>();

DotNetEnv.Env.Load(); // Đọc tệp .env

// Cấu hình JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Cấu hình yêu cầu metadata HTTPS (bỏ qua nếu phát triển)
        options.RequireHttpsMetadata = false; 

        // Lấy giá trị Issuer, Audience và SecretKey từ cấu hình
        var issuer = Environment.GetEnvironmentVariable("Issuer");
        var audience = Environment.GetEnvironmentVariable("Audience");
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        var tokenLifespanString = Environment.GetEnvironmentVariable("TokenLifespan");
        int tokenLifespan = int.TryParse(tokenLifespanString, out var result) ? result : 0; 

        // Kiểm tra nếu secretKey không có giá trị
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT secret key is missing.");
        }

        // Cấu hình các tham số xác thực token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, 
            ValidateLifetime = true, 
            ValidIssuer = issuer, 
            ValidAudience = audience, 
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), 
            ClockSkew = TimeSpan.Zero
        };

        
        options.SaveToken = true; 
    });


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
    policy =>
    {
        policy.AllowAnyOrigin()   
              .AllowAnyHeader()  
              .AllowAnyMethod(); 
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

app.MapGet("/hello", () => "Hello, World!");
app.Run();
