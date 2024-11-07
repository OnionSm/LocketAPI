using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[Route("api/conversation")]
[ApiController]
public class ConversationController : ControllerBase
{
    private IMongoClient _mongo_client;
    private ConversationService _conversation_service;
    public ConversationController(IMongoClient client, ConversationService service)
    {
        _mongo_client = client;
        _conversation_service = service;
    }

    [HttpPost]
    public async Task<IActionResult> CreateNewConversation([FromBody] Conversation conversation)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                await _conversation_service.CreateNewConversationAsync(conversation, session);
                await session.CommitTransactionAsync();
                return CreatedAtAction(nameof(GetConversationById), new { id = conversation.Id}, conversation);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Conversation>> GetConversationById(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try 
            {
                Conversation conversation = await _conversation_service.GetConversationByIdAsync(id, session);
                if(conversation == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                await session.CommitTransactionAsync();
                return Ok(conversation);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
        
    }

    
}