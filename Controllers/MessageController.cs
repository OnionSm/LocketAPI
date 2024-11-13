using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.AspNetCore.SignalR;




[Route("api/message")]
[ApiController]
public class MessageController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly MessageService _messageService;

    private readonly ConversationService _conversation_service;
    private readonly UserConversationService _user_conservation_service;

    private readonly UserService _user_service;

    private readonly IHubContext<ChatHub> _hubContext;

    public MessageController(IMongoClient client,
    MessageService service,
    ConversationService conversation_service, 
    UserConversationService user_conservation_service,
    UserService user_service,
    IHubContext<ChatHub> hubContext)
    {
        _mongo_client = client;
        _messageService = service;
        _conversation_service = conversation_service;
        _user_conservation_service = user_conservation_service;
        _user_service = user_service;
        _hubContext = hubContext;
    }

    // CREATE : POST
    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] Message message)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
               

                await _messageService.CreatMessageAsync(message, session);
        
                Message result = await _messageService.GetMessageByIdAsync(message.Id, session);
                if(result != null)
                {
                    // check sender which is in the conversation?
                    Conversation conversation = await _conversation_service.GetConversationByIdAsync(message.ConversationId, session);
                    var result2 = conversation.Participants.FirstOrDefault(p => p == message.SenderId);
                    if(result2 == null)
                    {
                        await session.AbortTransactionAsync();
                        return BadRequest("Không thể tạo tin nhắn");
                    }

                    // add message to conversation
                    await _conversation_service.AddMessageToConversationAsync(message, session);
                    // get list participants from conversation that message will send to
                    List<string> list_participants = await _conversation_service.GetListParticipants(message.ConversationId, session);
                    // send message to user conversation
                    foreach(string participant in list_participants)
                    {
                        await _user_conservation_service.AddNewMessageAsync(participant, message, session);
                    }
                    await session.CommitTransactionAsync();
                    return CreatedAtAction(nameof(GetMessageById), new { id = message.Id }, message);
                }
                else
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể tạo tin nhắn");
                }
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
        
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromForm] string user , [FromForm] string message)
    {
        // Gửi message tới tất cả client đã kết nối qua sự kiện "SendMessage"
        await _hubContext.Clients.All.SendAsync("SendMessage", user, message);
        return Ok("Message sent");
    }

    // READ : GET 
    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetMessageById(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                Message message = await _messageService.GetMessageByIdAsync(id, session);
                if(message == null)
                {
                    await session.AbortTransactionAsync();
                    return NotFound();
                }
                await session.CommitTransactionAsync();
                return Ok(message);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
    }

    // UPDATE : PUT
    [HttpPut("{id}")]
    public async Task<ActionResult<string>> UpdateMessage(string id, [FromBody] Message message)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try 
            {
                bool result = await _messageService.UpdateOneMessageAysnc(id, message, session);
                if(!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể sửa tin nhắn!");
                }
                await session.CommitTransactionAsync();
                return Ok("Sửa tin nhắn thành công!");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
    }

    // DELETE 
    [HttpDelete("{id}")]
    public async Task<ActionResult<string>> DeleteMessage(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                bool result = await _messageService.DeleteOneMessageAsync(id, session);
                if(!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Xóa tin nhắn không thành công!");
                }
                await session.CommitTransactionAsync();
                return Ok("Xóa tin nhắn thành công!");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
    }
}