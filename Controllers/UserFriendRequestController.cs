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

    [HttpPost("add-friend")]
    public async Task<ActionResult<string>> SendFriendshipInvitation([FromBody] FriendRequest request)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try 
            {
                session.StartTransaction();
                var result = await _user_friend_request_service.AddNewFriendRequestAsync(request, session);
                if(!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể gửi lời mời kết bạn");
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

    [HttpGet("receiver={id}")]
    public async Task<ActionResult<List<FriendRequest>>> GetFriendRequestReceiveByUseId(string id)
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
                    return NotFound("Không tìm thấy danh sách lời mời kết bạn");
                }
                List<FriendRequest> friend_requests = new List<FriendRequest>();
                foreach(FriendRequest rq in user_friend_request.FriendRequests)
                {
                    if(rq.ResponeStatus == null && rq.SenderId != id)
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

    
        
    [HttpPut("respone/{request_id}-{sender_id}-{receiver_id}-{state}")]
    public async Task<ActionResult<string>> ExecuteFriendRequest(string request_id, string sender_id, string receiver_id, bool state)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                // Bắt đầu giao dịch
                session.StartTransaction();

                // Xử lý yêu cầu kết bạn
                bool result = await _user_friend_request_service.ResponeFriendRequestAsync(request_id, sender_id, receiver_id, state, session);
                if (!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thực thi với lời mời kết bạn");
                }

                var add_fr_to_user_list_result = await _user_service.AddNewFriendAsync(receiver_id, sender_id, session);
                if(!add_fr_to_user_list_result) 
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thực thi với lời mời kết bạn");
                }


                // Tạo cuộc trò chuyện mới
                Conversation conversation = new Conversation();
                conversation.Participants.Add(sender_id);
                conversation.Participants.Add(receiver_id);

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