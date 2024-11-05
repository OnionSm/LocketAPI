using Microsoft.AspNetCore.Mvc;

[Route("api/chat")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    public ChatController(ChatService service)
    {
        _chatService = service;
    }

    // CREATE : POST
    [HttpPost]
    public async Task<IActionResult> CreateChat([FromBody] Chat chat)
    {
        await _chatService.CreateChatAsync(chat);
        return CreatedAtAction(nameof(GetChatById), new {id = chat.Id}, chat);
    }
    
    // READ : GET
    [HttpGet("{id}")]
    public async Task<ActionResult<Chat>> GetChatById(string id)
    {
        Chat chat = await _chatService.GetChatByChatIdAsyn(id);
        if(chat == null)
        {
            return NotFound();
        }
        return Ok(chat);
    }

    // READ : GET
    [HttpGet("user/{id}")]
    public async Task<ActionResult<List<Chat>>> GetChatByUserId(string id)
    {
        List<Chat> listChats = await _chatService.GetChatByUserIdAsync(id);
        if(listChats == null)
        {
            return NotFound();            
        }
        return Ok(listChats);
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteChat(string id)
    {
        bool result = await _chatService.DeleteChatByIdAsync(id);
        if(!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}