using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

[Authorize] 
[Route("api/userconversation")]
[ApiController]
public class UserConversationController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly UserConversationService _user_conversation_service;
    public UserConversationController(IMongoClient client, UserConversationService service)
    {
        _mongo_client = client;
        _user_conversation_service = service;
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
    public async Task<ActionResult<UserConversation>> GetUserConversationById()
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
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                UserConversation user_conversation = await _user_conversation_service.GetUserConversationByIdAsync(user_id, session);
                Console.WriteLine(user_conversation);
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
