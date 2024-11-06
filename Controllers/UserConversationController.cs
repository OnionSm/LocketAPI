using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/userconversation")]
[ApiController]
public class UserConversationController : ControllerBase
{
    private readonly UserConversationService _user_conversation_service;
    public UserConversationController(UserConversationService service)
    {
        _user_conversation_service = service;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateNewUserConversation([FromBody] UserConversation user_conversation)
    {
        await _user_conversation_service.CreateUserConversationAsync(user_conversation);
        return CreatedAtAction(nameof(GetUserConversationById), new { id = user_conversation.Id }, user_conversation);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserConversation>> GetUserConversationById(string id)
    {
        UserConversation user_conversation = await _user_conversation_service.GetUserConversationByIdAsync(id);
        if(user_conversation == null)
        {
            return NotFound();
        }
        return Ok(user_conversation);
    }

    [HttpGet]
    public async Task<ActionResult<List<UserConversation>>> GetAllUserConversation()
    {
        List<UserConversation> list_user_conversation = await _user_conversation_service.GetAllUserConversationAsync();
        if(list_user_conversation == null)
        {
            return NotFound();
        }
        return Ok(list_user_conversation);
    }

    

    [HttpGet("user/{id}")]
    public async Task<ActionResult<UserConversation>> GetUserConversationByUserId(string id)
    {
        UserConversation user_conversation = await _user_conversation_service.GetUserConversationByUserId(id);
        if(user_conversation == null)
        {
            return NotFound();
        }
        return Ok(user_conversation);
    }
}
