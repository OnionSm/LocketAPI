using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[Route("api/users/friend-request")]
[ApiController]
public class UserFriendRequestController: ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly UserFriendRequestService _user_friend_request_service;
    private readonly ConversationService _conservation_service;
    private readonly UserConversationService _user_conversation_service;

    private readonly UserService _user_service;
    public UserFriendRequestController(IMongoClient client,
    UserFriendRequestService service,
    ConversationService service2,
    UserConversationService service3,
    UserService user_service)
    {
        _mongo_client = client;
        _user_friend_request_service = service;
        _conservation_service = service2;
        _user_conversation_service = service3;
        _user_service = user_service;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateNewUserFriendRequest([FromBody] UserFriendRequest ufr)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
                await _user_friend_request_service.CreateNewUserFriendRequestAsync(ufr, session);
                await session.CommitTransactionAsync();
                return CreatedAtAction(nameof(GetUserFriendRequestById), new {id = ufr.Id}, ufr);
            }
            catch(Exception)
            {
                await session.AbortTransactionAsync();
                return BadRequest("Đã xảy ra lỗi khi thực hiện giao dịch");
            }
        }
    }

    [Authorize]
    [HttpPost("add-friend")]
    public async Task<ActionResult<string>> SendFriendshipInvitation([FromForm] FriendRequest request)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try 
            {
                session.StartTransaction();

                var user_id = User.FindFirst("UserId")?.Value;
                if(string.IsNullOrEmpty(user_id))
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                request.SenderId = user_id;
                var result = await _user_friend_request_service.AddNewFriendRequestAsync(request, session);
                if(!result)
                {
                    await session.AbortTransactionAsync();
                    // return BadRequest("Không thể gửi lời mời kết bạn");
                    return Ok();
                }
                await session.CommitTransactionAsync();
                return Ok("Gửi lời mời kết bạn thành công!");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
        
    }

    [Authorize]
    [HttpGet("receive")]
    public async Task<ActionResult<List<FriendRequest>>> GetFriendRequestReceiveByUseId()
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user_id = User.FindFirst("UserId")?.Value;
                if(string.IsNullOrEmpty(user_id))
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                var user_friend_request = await _user_friend_request_service.GetUserFriendRequestByUserIdAsync(user_id, session);
                if(user_friend_request == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound("Không tìm thấy danh sách lời mời kết bạn");
                }
                List<object> friend_requests = new List<object>();
                
                foreach(FriendRequest rq in user_friend_request.FriendRequests)
                {
                    if(rq.ResponeStatus == null && rq.SenderId != user_id)
                    {
                        var user = await _user_service.GetUserDataByUserIdAsync(rq.SenderId, session);
                        if(user != null)
                        {
                            friend_requests.Add(new Dictionary<string, object>
                            {
                                { "id", rq.Id },
                                { "first_name", user.FirstName },
                                { "last_name", user.LastName },
                                { "senderId", rq.SenderId },
                                { "userAvatarURL", user.UserAvatarURL }
                            });
                        }
                    }
                }
                await session.CommitTransactionAsync();
                return Ok(friend_requests);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
        
    }

    [HttpGet("sender={id}")]
    public async Task<ActionResult<List<FriendRequest>>> GetFriendRequestSendByUseId(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user_friend_request = await _user_friend_request_service.GetUserFriendRequestByUserIdAsync(id, session);
                if(user_friend_request == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound("Không tìm thấy danh sách yêu cầu kết bạn");
                }
                List<FriendRequest> friend_requests = new List<FriendRequest>();
                foreach(FriendRequest rq in user_friend_request.FriendRequests)
                {
                    if(rq.ResponeStatus == null && rq.SenderId == id)
                    {
                        friend_requests.Add(rq);
                    }
                }
                await session.CommitTransactionAsync();
                return Ok(friend_requests);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }

        
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<UserFriendRequest>> GetUserFriendRequestById(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                UserFriendRequest ufr = await _user_friend_request_service.GetUserFriendRequestByUserIdAsync(id, session);
                if(ufr == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                await session.CommitTransactionAsync();
                return Ok(ufr);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
    }

    
    [Authorize]
    [HttpPut("respone-request")]
    public async Task<ActionResult<string>> ExecuteFriendRequest([FromForm] string request_id, [FromForm] string sender_id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
                var user_id = User.FindFirst("UserId")?.Value;

                if(string.IsNullOrEmpty(user_id))
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                // Xử lý yêu cầu kết bạn
                bool result = await _user_friend_request_service.ResponeFriendRequestAsync(request_id, user_id, sender_id, session);
                if (!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thực thi với lời mời kết bạn");
                }

                var add_fr_to_user_list_result = await _user_service.AddNewFriendAsync(user_id, sender_id, session);
                if(!add_fr_to_user_list_result) 
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thực thi với lời mời kết bạn");
                }


                // Tạo cuộc trò chuyện mới
                Conversation conversation = new Conversation();
                conversation.Participants.Add(sender_id);
                conversation.Participants.Add(user_id);

                var result2 = await _conservation_service.CreateNewConversationAsync(conversation, session);
                if (!result2)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thực thi với lời mời kết bạn");
                }

                // Thêm cuộc trò chuyện vào danh sách cuộc trò chuyện của người dùng
                var result3 = await _user_conversation_service.AddNewConversationAsync(conversation, session);
                if (!result3)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thực thi với lời mời kết bạn");
                }

                // Cam kết giao dịch
                await session.CommitTransactionAsync();
                return Ok("Kết bạn thành công");
            }
            catch (Exception)
            {
                await session.AbortTransactionAsync();
                return BadRequest("Đã xảy ra lỗi khi thực hiện giao dịch");
            }
        }
    }

}