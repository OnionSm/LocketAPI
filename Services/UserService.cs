using Microsoft.AspNetCore.Components;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly int _jwtLifespan;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public UserService(IMongoDatabase database, IConfiguration configuration)
    {
        _usersCollection = database.GetCollection<User>("User");
        _configuration = configuration;

        // Lấy giá trị từ cấu hình JWT
        var jwtSettings = _configuration.GetSection("JwtSettings");
        _jwtSecret = jwtSettings["SecretKey"];
        _jwtLifespan = int.Parse(jwtSettings["TokenLifespan"]);
        _jwtIssuer = jwtSettings["Issuer"];
        _jwtAudience = jwtSettings["Audience"];
    }


    // CREATE
    public async Task CreateUserAsync(User user, IClientSessionHandle session)
    {
        await _usersCollection.InsertOneAsync(session, user);
    }

    // READ - Retrieve all users
    public async Task<List<User>> GetAllUsersAsync(IClientSessionHandle session)
    {
        return await _usersCollection.Find(session, user => true).ToListAsync();
    }

    // READ - Retrieve user by ID
    public async Task<User> GetUserByIdAsync(string id, IClientSessionHandle session)
    {
        return await _usersCollection.Find(session, user => user.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User> GetUserDataByTokenAsync(string user_id, string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSecret);  

            // Thiết lập các tham số xác thực token
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtIssuer, // Issuer mong đợi

                ValidateAudience = true,
                ValidAudience = _jwtAudience, // Audience mong đợi

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Không cho phép thời gian chênh lệch
            };

            // Xác thực token và lấy ClaimsPrincipal
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            // Lấy thông tin từ Claims
            var userIdFromToken = principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            var userRole = principal.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;

            if (userIdFromToken == null)
            {
                throw new UnauthorizedAccessException("Token không chứa thông tin UserId.");
            }

            // Xử lý phân quyền dựa trên role
            if (userRole == "Admin")
            {
                var user = await _usersCollection.Find(u => u.Id == user_id).FirstOrDefaultAsync();
                return user;
            }
            else
            {
                var user = await _usersCollection.Find(u => u.Id == userIdFromToken && u.Id == user_id).FirstOrDefaultAsync();
                return user;
            }
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Không thể lấy thông tin người dùng.", ex);
        }
    }


    // UPDATE
    public async Task<bool> UpdateUserAsync(string id, User updatedUser, IClientSessionHandle session)
    {
        var result = await _usersCollection.ReplaceOneAsync(session, user => user.Id == id, updatedUser);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AddNewFriendAsync(string user_id_1, string user_id_2, IClientSessionHandle session)
    {
        var users = await _usersCollection
                            .Find(session, u => u.Id == user_id_1 || u.Id == user_id_2)
                            .ToListAsync();

        var user_1 = users.FirstOrDefault(u => u.Id == user_id_1);
        var user_2 = users.FirstOrDefault(u => u.Id == user_id_2);

        if (user_1 == null || user_2 == null)
        {
            return false; 
        }
        user_1.Friends.Add(user_id_2);
        user_2.Friends.Add(user_id_1);

     
        var update_user_1 = Builders<User>.Update.Set(u => u.Friends, user_1.Friends);
        var update_user_2 = Builders<User>.Update.Set(u => u.Friends, user_2.Friends);

        var update_result_1 = await _usersCollection.UpdateOneAsync(session, u => u.Id == user_id_1, update_user_1);
        var update_result_2 = await _usersCollection.UpdateOneAsync(session, u => u.Id == user_id_2, update_user_2);

        // Kiểm tra kết quả của cả hai thao tác
        return update_result_1.IsAcknowledged && update_result_1.ModifiedCount > 0
            && update_result_2.IsAcknowledged && update_result_2.ModifiedCount > 0;
    }

    public async Task<bool> DeleteUserAsync(string id, IClientSessionHandle session)
    {
        var result = await _usersCollection.DeleteOneAsync(session, user => user.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public async Task<User> GetUserByPhoneNumberAsync(string phone_number, IClientSessionHandle session)
    {
        var result = await _usersCollection.Find(session, user => user.PhoneNumber == phone_number).FirstOrDefaultAsync();
        return result;
    }

    public async Task<User> GetUserByEmailAsync(string email, IClientSessionHandle session)
    {
        return await _usersCollection.Find(session, user => user.Email == email).FirstOrDefaultAsync();
    }

    public string GenerateJwtToken(User user)
    {
        if (_jwtLifespan <= 0 || _jwtSecret == null)
        {
            return ""; 
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);

        // Tạo Access Token
        var accessTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[] 
            {
                new Claim("UserId", user.Id.ToString()), 
                new Claim("FirstName", user.FirstName), 
                new Claim("LastName", user.LastName),
                new Claim("Role", "User")
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtLifespan),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
        return tokenHandler.WriteToken(accessToken);
    }
}
