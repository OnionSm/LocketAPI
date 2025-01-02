using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;

[Authorize] 
[Route("api/userconversation")]
[ApiController]
public class UserConversationController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly UserConversationService _user_conversation_service;
    private readonly UserService _user_service;
    public UserConversationController(IMongoClient client, UserConversationService service, UserService user_service)
    {
        _mongo_client = client;
        _user_conversation_service = service;
        _user_service = user_service;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateNewUserConversation([FromBody] UserConversation user_conversation)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                await _user_conversation_service.CreateUserConversationAsync(user_conversation, session);
                await session.CommitTransactionAsync();
                return CreatedAtAction(nameof(GetUserConversationById), new { id = user_conversation.Id }, user_conversation);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
        
    }

    [HttpGet]
    public async Task<ActionResult<List<ConversationRespone>>> GetUserConversationById()
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user_id = User.FindFirst("UserId")?.Value;
                Console.WriteLine(user_id);
                if (string.IsNullOrEmpty(user_id))
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                UserConversation user_conversation = await _user_conversation_service.GetUserConversationByIdAsync(user_id, session);
                Console.WriteLine(user_conversation);
                if (user_conversation == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                List<ConversationRespone> data_respone = new List<ConversationRespone>();
                foreach(Conversation c in user_conversation.UserConversations)
                {
                    ConversationRespone cv_respone = new ConversationRespone();
                    cv_respone.Id = c.Id;
                    cv_respone.Participants = c.Participants;
                    cv_respone.ListMessages = c.ListMessages;
                    cv_respone.LastMessage = c.LastMessage;
                    cv_respone.CreatedAt = c.CreatedAt;
                    cv_respone.UpdatedAt = c.UpdatedAt;
                    foreach (string member in c.Participants)
                    {
                        if (member != user_id)
                        {
                            var user_data = await _user_service.GetUserDataByUserIdAsync(member,session);
                            if(user_data != null)
                            {
                                cv_respone.GroupName = user_data.FirstName + " " + user_data.LastName;
                                cv_respone.GroupAvatarUrl = user_data.UserAvatarURL;
                                break;
                            }
                        }
                    }
                    data_respone.Add(cv_respone);
                }
                await session.CommitTransactionAsync();
                return Ok(data_respone);
            }
            catch (Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
    }

    [HttpGet("conversation/{conversation_id}")]
    public async Task<ActionResult<Conversation>> GetConversationById(string conversation_id)
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
                UserConversation user_conversation = await _user_conversation_service.GetUserConversationByIdAsync(user_id, session);
                if(user_conversation == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                var conversation = user_conversation.UserConversations.FirstOrDefault(c => c.Id == conversation_id);
                if(conversation == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                await session.CommitTransactionAsync();
                return Ok(conversation);
            }
            catch (Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }

    }

    // [HttpGet]
    // public async Task<ActionResult<List<UserConversation>>> GetAllUserConversation()
    // {
    //     using (var session = await _mongo_client.StartSessionAsync())
    //     {
    //         session.StartTransaction();
    //         try
    //         {
    //             List<UserConversation> list_user_conversation = await _user_conversation_service.GetAllUserConversationAsync(session);
    //             if(list_user_conversation == null)
    //             {
    //                 await session.AbortTransactionAsync();
    //                 return NotFound();
    //             }
    //             await session.CommitTransactionAsync();
    //             return Ok(list_user_conversation);
    //         }
    //         catch(Exception e)
    //         {
    //             await session.AbortTransactionAsync();
    //             return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
    //         }
    //     }
        
    // }

    

    [HttpGet("user/{id}")]
    public async Task<ActionResult<UserConversation>> GetUserConversationByUserId(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                UserConversation user_conversation = await _user_conversation_service.GetUserConversationByUserId(id, session);
                if(user_conversation == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                await session.CommitTransactionAsync();
                return Ok(user_conversation);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
    }
}
