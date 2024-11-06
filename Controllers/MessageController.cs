using Microsoft.AspNetCore.Mvc;



[Route("api/message")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;

    private readonly ConversationService _conversation_service;
    private readonly UserConversationService _user_conservation_service;

    public MessageController(MessageService service, ConversationService conversation_service, UserConversationService user_conservation_service)
    {
        _messageService = service;
        _conversation_service = conversation_service;
        _user_conservation_service = user_conservation_service;
    }

    // CREATE : POST
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] Message message)
    {
        await _messageService.CreatMessageAsync(message);
        
        //return CreatedAtAction(nameof(GetMessageById), new { id = message.Id }, message);
        var result = GetMessageById(message.Id);
        if(result == null)
        {
            // add message to conversation
            await _conversation_service.AddMessageToConversationAsync(message);
            // get list participants from conversation that message will send to
            List<string> list_participants = await _conversation_service.GetListParticipants(message.ConversationId);
            // send message to user conversation
            foreach(string participant in list_participants)
            {
                await _user_conservation_service.AddNewMessageAsync(participant, message);
            }
            return CreatedAtAction(nameof(GetMessageById), new { id = message.Id }, message);
        }
        else
        {
            return NotFound();
        }
    }

    // READ : GET 
    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetMessageById(string id)
    {
        Message message = await _messageService.GetMessageByIdAsync(id);
        if(message == null)
        {
            return NotFound();
        }
        return Ok(message);
    }

    // UPDATE : PUT
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMessage(string id, [FromBody] Message message)
    {
        bool result = await _messageService.UpdateOneMessageAysnc(id, message);
        if(!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // DELETE 
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMessage(string id)
    {
        bool result = await _messageService.DeleteOneMessageAsync(id);
        if(!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}