using MongoExample.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


DotNetEnv.Env.Load(); // Đọc tệp .env

// var builder = WebApplication.CreateBuilder(args);

// // Đăng ký MongoClient với ConnectionString từ MongoDBSettings
// builder.Services.AddSingleton<IMongoClient>(s =>
// {
//     var connection_url = Environment.GetEnvironmentVariable("MongoDBConnectionURI");
//     return new MongoClient(connection_url);
// });


// // Đăng ký IMongoDatabase
// builder.Services.AddScoped(s =>
// {
//     var client = s.GetRequiredService<IMongoClient>();
//     var database_name = Environment.GetEnvironmentVariable("DatabaseName");
//     return client.GetDatabase(database_name);
// });

var builder = WebApplication.CreateBuilder(args);

// Đăng ký MongoClient với ConnectionString từ MongoDBSettings
builder.Services.AddSingleton<IMongoClient>(s =>
{
    var MONGO_USERNAME = Environment.GetEnvironmentVariable("MONGO_USERNAME");
    var MONGO_PASSWORD = Environment.GetEnvironmentVariable("MONGO_PASSWORD");
    var AUTH_MECHANISM = Environment.GetEnvironmentVariable("AUTH_MECHANISM");
    //var connection_url = $"mongodb://{MONGO_USERNAME}:{MONGO_PASSWORD}@mongodb-0.mongodb-service:27017,mongodb-1.mongodb-service:27017,mongodb-2.mongodb-service:27017/?replicaSet=rs0&authSource=admin";
    
    var connection_url = $"mongodb://{MONGO_USERNAME}:{MONGO_PASSWORD}@k8s-loadbalancer.xn--hanh-0na.vn/database/?replicaSet=rs0&authMechanism=SCRAM-SHA-256&authSource=Locket";

   // var connection_url = $"mongodb://{MONGO_USERNAME}:{MONGO_PASSWORD}@mongodb-0.mongodb-headless:27017/?replicaSet=rs0&authMechanism=SCRAM-SHA-256&authSource=Locket";

 
    Console.WriteLine("Connection url " + connection_url);
    var client = new MongoClient(connection_url);
    Console.WriteLine("MongoClient initialized successfully.");
    return client;
});

// Đăng ký IMongoDatabase
builder.Services.AddScoped(s =>
{
    var client = s.GetRequiredService<IMongoClient>();
    var database_name = Environment.GetEnvironmentVariable("DatabaseName");
    var database = client.GetDatabase(database_name);
    Console.WriteLine($"Connected to database: {database_name}");
    return database;
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

// Endpoint /hello
app.MapGet("/hello", () =>
{
    Console.WriteLine("Endpoint /hello was called.");
    return "Hello, World!";
});

// Endpoint mặc định
app.MapGet("", () =>
{
    Console.WriteLine("Endpoint / was called.");
    return "Welcome to LOCKET API";
});
app.Run();
