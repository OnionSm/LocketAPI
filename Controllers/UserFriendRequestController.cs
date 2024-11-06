using Microsoft.AspNetCore.Mvc;

[Route("api/users/friend-request")]
[ApiController]
public class UserFriendRequestController: ControllerBase
{
    private readonly UserFriendRequestService _user_friend_request_service;
    public UserFriendRequestController(UserFriendRequestService service)
    {
        _user_friend_request_service = service;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateNewUserFriendRequest([FromBody] UserFriendRequest ufr)
    {
        await _user_friend_request_service.CreateNewUserFriendRequestAsync(ufr);
        return CreatedAtAction(nameof(GetUserFriendRequestById), new {id = ufr.Id}, ufr);
    }

    [HttpPost("add-friend")]
    public async Task<ActionResult<string>> SendFriendshipInvitation([FromBody] FriendRequest request)
    {
        var result = await _user_friend_request_service.AddNewFriendRequestAsync(request);
        if(!result)
        {
            return BadRequest("Không thể gửi lời mời kết bạn");
        }
        return Ok("Gửi lời mời kết bạn thành công!");
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserFriendRequest>> GetUserFriendRequestById(string id)
    {
        UserFriendRequest ufr = await _user_friend_request_service.GetUserFriendRequestByUserIdAsync(id);
        if(ufr == null)
        {
            return NotFound();
        }
        return Ok(ufr);
    }

    [HttpPut("respone")]
    public async Task<ActionResult<string>> ExecuteFriendResquest([FromBody] FriendRequest request, bool is_accept)
    {
        bool result = await _user_friend_request_service.ResponeFriendRequestAsync(request, is_accept);
        if(!result)
        {
            return BadRequest("Không thể thực thi với lời mời kết bạn");
        }
        return Ok("Kết bạn thành công");

    }
}