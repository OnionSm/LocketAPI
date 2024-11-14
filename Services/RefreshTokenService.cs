using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

public class RefreshTokenService
{
    private readonly IMongoClient _mongo_client;
    private readonly IMongoCollection<UserRefreshTokens> _user_refresh_token_collection;
    
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecret;
    private readonly int _jwtLifespan;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    public RefreshTokenService(IMongoDatabase database, IMongoClient client, IConfiguration configuration, IOptions<JwtSettings> jwt_setting)
    {
        _user_refresh_token_collection = database.GetCollection<UserRefreshTokens>("UserRefreshTokens");
        _mongo_client = client;
        _configuration = configuration;
        _jwtAudience = jwt_setting.Value.Audience;
        _jwtIssuer = jwt_setting.Value.Issuer;
        _jwtLifespan = jwt_setting.Value.Lifespan.HasValue ? jwt_setting.Value.Lifespan.Value : 30;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    }

    public async Task<string> CreateNewRefreshTokenAsync(string user_id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {

            session.StartTransaction();
            try
            {
                var check = await _user_refresh_token_collection.Find(session, urt => urt.UserId == user_id).FirstOrDefaultAsync();
                // create new user refresh tokens
                if(check == null)
                {
                    UserRefreshTokens new_urft = new UserRefreshTokens();
                    new_urft.UserId = user_id;
                    await _user_refresh_token_collection.InsertOneAsync(session, new_urft);
                }
                var urft = await GetUserRefreshTokenByUserId(user_id, session);
                var refreshToken = Guid.NewGuid().ToString();
                RefreshToken new_rt = new RefreshToken();
                new_rt.Token = refreshToken;
                urft.RefreshTokens.Add(new_rt);

                var update_data = Builders<UserRefreshTokens>.Update.Set(u => u.RefreshTokens, urft.RefreshTokens);
                await _user_refresh_token_collection.UpdateOneAsync(session, u => u.UserId == user_id, update_data);
                await session.CommitTransactionAsync();
                return refreshToken;
            }
            catch(Exception)
            {
                await session.AbortTransactionAsync();
                return "";
            }
        }
    }


    public async Task<UserRefreshTokens> GetUserRefreshTokenByUserId(string user_id, IClientSessionHandle session)
    {
        return await _user_refresh_token_collection.Find(session, urt => urt.UserId == user_id).FirstOrDefaultAsync();
    }

    
   
    public string RefreshAccessTokenAsync(string user_id)
    {




        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);

        // Tạo Access Token
        var accessTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("UserId", user_id.ToString()),
            }),
            Expires = DateTime.UtcNow.AddSeconds(_jwtLifespan),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
        var token = tokenHandler.WriteToken(accessToken);
        Console.WriteLine("Token Generated: ",token);
        return token;
    }

    public async Task<string> IsValidRefreshTokenAsync(string refresh_token)
    {
        // Tìm người dùng có refresh token hợp lệ
        var urft = await _user_refresh_token_collection
            .Find(urt => urt.RefreshTokens
                .Any(t => t.Token == refresh_token && t.ExpiryDate > DateTime.UtcNow)) 
            .FirstOrDefaultAsync();

        return urft != null ? urft.UserId : null;
    }
}