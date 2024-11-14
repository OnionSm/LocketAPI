using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;



[Route("api/refresh-token")]
[ApiController]
public class RefreshTokenController: ControllerBase
{
    private readonly RefreshTokenService _refresh_token_service;
    private readonly IMongoClient _mongo_client;
    private readonly UserService _user_service; 
    public RefreshTokenController(RefreshTokenService rft_service, IMongoClient client, UserService user_service)
    {
        _refresh_token_service = rft_service;
        _mongo_client = client;
        _user_service = user_service;
    }

    [HttpPost]
    public async Task<ActionResult<string>> RefreshToken([FromForm] string refresh_token)
    {
       
        try
        {
            // var user_id = User.FindFirst("UserId")?.Value;

            // if (string.IsNullOrEmpty(user_id))
            // {
            //     return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
            // }

            var user_id = await _refresh_token_service.IsValidRefreshTokenAsync(refresh_token);
            if (user_id == null)
            {
                return Unauthorized("Vui lòng đăng nhập lại");
            }
            string token = _user_service.GenerateJwtToken(user_id);
            if (token == null || token == "") 
            {
                return Unauthorized("Vui lòng đăng nhập lại");
            }
            return Ok(token);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
 
        
    }
}