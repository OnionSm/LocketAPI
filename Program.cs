using MongoExample.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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

// Đăng ký Controller
builder.Services.AddControllers();

var app = builder.Build();

// Ánh xạ các Controller
app.MapControllers();

app.MapGet("/", () => "Hello Onion");

app.Run();
