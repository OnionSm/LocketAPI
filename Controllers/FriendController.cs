using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

[Authorize] 
[Route("api/user/friend")]
[ApiController]
public class FriendController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly FriendService _friend_service;

    public FriendController(IMongoClient client, FriendService friend_service)
    {
        _mongo_client = client;
        _friend_service = friend_service;
    }

    
    [HttpGet("info")]
    public async Task<ActionResult> GetListFriendInfo()
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();

                var user_id = User.FindFirst("UserId")?.Value; 
                if (string.IsNullOrEmpty(user_id))
                {
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }

                var list_friend_info = await _friend_service.GetListFriendInfoAsync(user_id, session);
                if(list_friend_info == null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest();
                }
                await session.CommitTransactionAsync();
                return Ok(list_friend_info);
            }
            catch
            {
                await session.AbortTransactionAsync();
                return BadRequest();
            }
        }
    }

    [HttpPost("user")]
    public async Task<ActionResult> GetUserData([FromForm] string public_user_id)
    {
        try
        {
            var user_id = User.FindFirst("UserId")?.Value; 

            if (string.IsNullOrEmpty(user_id))
            {
                return Unauthorized();
            }
            var user = await _friend_service.GetUserDataAsync(user_id, public_user_id);


            if (user == null || user.Id == user_id || user.ShowUser == false)
            {
                return NotFound(); 
            }
        

            return Ok(new
            {
                user.Id,
                user.PublicUserId,
                user.FirstName,
                user.LastName,
                user.UserAvatarURL
            });
        }
        catch (Exception ex)
        {
            // Xử lý lỗi, có thể log thông tin lỗi
            return StatusCode(500, new { message = "An error occurred", details = ex.Message }); // Trả về mã lỗi 500 với thông tin chi tiết
        }
    }
}