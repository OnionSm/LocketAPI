using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[Route("api/login")]
[ApiController]
public class LoginController: ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly LoginService _login_service;
    private readonly UserService _user_service;
    private readonly RefreshTokenService _refresh_token_service;
    public LoginController(LoginService login_service, UserService user_service, IMongoClient client, RefreshTokenService rft_service)
    {
        _login_service = login_service;
        _user_service = user_service;
        _mongo_client = client;
        _refresh_token_service = rft_service;
    }

    [HttpPost("email")]
    public async Task<ActionResult<string>> LoginByEmail([FromForm] string Email, [FromForm] string Password)
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user = await _login_service.AuthenticateByEmailAsync(Email, Password);
                if(user == null)
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không thể xác thực người dùng");
                }
                
                string access_token = _user_service.GenerateJwtToken(user);
                if(access_token == "")
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không thể xác thực người dùng");
                }
                
                var refreshToken = await _refresh_token_service.CreateNewRefreshTokenAsync(user.Id);
                TokenResponse token = new TokenResponse();
                token.AccessToken = access_token;
                token.RefreshToken = refreshToken;

                await session.CommitTransactionAsync();
                return Ok(token);
                
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Có lỗi xảy ra trong quá trình thực thi, error: {e}"); 
            }
        }
        
    }
}