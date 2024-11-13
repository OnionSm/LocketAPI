using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

public class JwtService 
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    private readonly JwtSettings _jwt_settings;

    public JwtService(IOptions<JwtSettings> jwt_setting)
    {
        _jwtAudience = jwt_setting.Value.Audience;
        _jwtIssuer = jwt_setting.Value.Issuer;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _jwtIssuer,

            ValidateAudience = true,
            ValidAudience = _jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Token không hợp lệ.", ex);
        }
    }
}
