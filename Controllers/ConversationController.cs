using Microsoft.AspNetCore.Mvc;

[Route("api/conversation")]
[ApiController]
public class ConversationController : ControllerBase
{
    private ConversationService _conversation_service;
    public ConversationController(ConversationService service)
    {
        _conversation_service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateNewConversation([FromBody] Conversation conversation)
    {
        await _conversation_service.CreateNewConversationAsync(conversation);
        return CreatedAtAction(nameof(GetConversationById), new { id = conversation.Id}, conversation);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Conversation>> GetConversationById(string id)
    {
        Conversation conversation = await _conversation_service.GetConversationByIdAsync(id);
        if(conversation == null)
        {
            return NotFound();
        }
        return Ok(conversation);
    }

    
}