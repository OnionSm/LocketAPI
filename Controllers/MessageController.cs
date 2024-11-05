using Microsoft.AspNetCore.Mvc;



[Route("api/message")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly MessageService _messageService;
    public MessageController(MessageService service)
    {
        _messageService = service;
    }

    // CREATE : POST
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] Message message)
    {
        await _messageService.CreatMessageAsync(message);
        return CreatedAtAction(nameof(GetMessageById), new { id = message.Id }, message);
    }

    // READ : GET 
    [HttpGet("{id}")]
    public async Task<ActionResult> GetMessageById(string id)
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