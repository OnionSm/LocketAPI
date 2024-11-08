using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class RefreshTokenService
{
    private readonly IMongoClient _mongo_client;
    private readonly IMongoCollection<UserRefreshTokens> _user_refresh_token_collection;
    
    private readonly IConfiguration _configuration;
    public RefreshTokenService(IMongoDatabase database, IMongoClient client, IConfiguration configuration)
    {
        _user_refresh_token_collection = database.GetCollection<UserRefreshTokens>("UserRefreshTokens");
        _mongo_client = client;
       _configuration = configuration;
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

    public async Task<string> RefreshAccessToken(string refreshToken, string user_id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                // Lấy giá trị từ appsettings.json
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var jwtSecret = jwtSettings["SecretKey"];
                var jwtLifespan = int.Parse(jwtSettings["TokenLifespan"]);
                var jwtIssuer = jwtSettings["Issuer"];
                var jwtAudience = jwtSettings["Audience"];

                // Kiểm tra refresh token có hợp lệ hay không (so với cơ sở dữ liệu hoặc bộ nhớ)
                // Nếu hợp lệ, tạo lại Access Token mới
                var check_token_expired = await IsValidRefreshToken(refreshToken, user_id, session);
                if (check_token_expired)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.UTF8.GetBytes(jwtSecret);

                    var accessTokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[] 
                        {
                            new Claim("UserId", user_id), 
                           
                        }),
                        Expires = DateTime.UtcNow.AddMinutes(jwtLifespan),
                        Issuer = jwtIssuer,
                        Audience = jwtAudience,
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };

                    var accessToken = tokenHandler.CreateToken(accessTokenDescriptor);
                    await session.CommitTransactionAsync();
                    return tokenHandler.WriteToken(accessToken);
                }
                else
                {
                    // Nếu refresh token không hợp lệ, yêu cầu người dùng đăng nhập lại
                    await session.AbortTransactionAsync();
                    return "";
                }
            }
            catch(Exception)
            {
               await session.AbortTransactionAsync();
               return ""; 
            }
        }
        
    }

    private async Task<bool> IsValidRefreshToken(string refreshToken, string user_id, IClientSessionHandle session)
    {
        var urft = await GetUserRefreshTokenByUserId(user_id, session);
        if(urft == null)
        {
            return false;
        }

        var storedRefreshToken = urft.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);

        if (storedRefreshToken != null && storedRefreshToken.ExpiryDate > DateTime.UtcNow)
        {
            return true; // Refresh token hợp lệ
        }
        else
        {
            return false; // Refresh token không hợp lệ hoặc đã hết hạn
        }
    }
}