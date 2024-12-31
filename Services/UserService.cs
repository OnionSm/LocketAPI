    using Microsoft.AspNetCore.Components;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Extensions.Options;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly int _jwtLifespan;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly JwtService _jwtService;
    

    public UserService(IMongoDatabase database, JwtService jwtService)
    {
        _usersCollection = database.GetCollection<User>("User");
        _jwtService = jwtService;
        _jwtIssuer = Environment.GetEnvironmentVariable("Issuer");
        _jwtAudience = Environment.GetEnvironmentVariable("Audience");
        _jwtLifespan = int.TryParse(Environment.GetEnvironmentVariable("TokenLifespan"), out var result) ? result : 30;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
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
    public async Task<User> GetUserByIdAsync(string public_user_id, IClientSessionHandle session)
    {
        return await _usersCollection.Find(session, user => user.PublicUserId == public_user_id).FirstOrDefaultAsync();
    }

    public async Task<User> GetUserDataByUserIdAsync(string user_id)
    {
        try
        {
            Console.WriteLine("user_id :", user_id);
            var user = await _usersCollection.Find(u => u.Id == user_id).FirstOrDefaultAsync();
            return user;
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
    public async Task<User> SetDeletedAccountAsync(string id, IClientSessionHandle session)
    {
        var user = await _usersCollection.Find(session, u => u.Id == id).FirstOrDefaultAsync();
        user.AccountDeleted = true;
        var res =  await DeleteUserAsync(id, session);
        if (!res)
        {
            return null;
        }
        return user;
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

    public string GenerateJwtToken(string user_id)
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
                new Claim("UserId", user_id.ToString()), 
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtLifespan),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
        return tokenHandler.WriteToken(accessToken);
    }
    public async Task<bool> ChangeUsernameAsync(string user_id, string first_name, string last_name, IClientSessionHandle session)
    {
        var user = await _usersCollection.Find(u => u.Id == user_id).FirstOrDefaultAsync();
        if (user == null)
        {
            return false;
        }   
        var update_user = Builders<User>.Update.Set(u => u.FirstName, first_name).Set(u => u.LastName, last_name);
        var update_result = await _usersCollection.UpdateOneAsync(u => u.Id == user_id, update_user);
        return update_result.IsAcknowledged && update_result.ModifiedCount > 0;
    }

    public async Task<bool> ChangeAvatarAsync(string user_id, byte[] binaryData, IClientSessionHandle session)
    {
        var user = _usersCollection.Find(session, u => u.Id == user_id).FirstOrDefaultAsync();
        if (user == null)
        {
            return false;
        }
        var data_update = Builders<User>.Update.Set(u => u.UserAvatarURL, binaryData);
        var update_result = await _usersCollection.UpdateOneAsync(u => u.Id == user_id, data_update);
        return update_result.IsAcknowledged && update_result.ModifiedCount > 0;
    }
}
