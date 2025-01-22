using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Newtonsoft.Json;


[Authorize]
[Route("api/story")]
[ApiController]
public class StoryController: ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly StoryService _story_service;
    private readonly UserService _user_serivice;
    private readonly UserConversationService _user_conservation_service;
    private readonly MessageService _messageService;
    private readonly ConversationService _conversation_service;

    private readonly int max_story_iamge_respone = 5;

    public StoryController(IMongoClient client, 
    StoryService story_service, 
    UserService user_service, 
    UserConversationService user_conservation_service, 
    MessageService service,
    ConversationService conversation_service)
    {
        _mongo_client = client;
        _story_service = story_service;
        _user_serivice = user_service;
        _user_conservation_service = user_conservation_service;
        _messageService = service;
        _conversation_service = conversation_service;
    }


    [HttpPost("create_story")]
    public async Task<IActionResult> CreateNewStory([FromForm] Story story)
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
                var user_id = User.FindFirst("UserId")?.Value; 

                if (string.IsNullOrEmpty(user_id))
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                List<string> receivers = new List<string>();
                if (story.Receivers.Count > 0)
                {
                    receivers = story.Receivers[0]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries) 
                    .ToList();
                }
                story.Receivers = receivers;
                story.UserId = user_id;
                await _story_service.CreateNewStoryAsync(story,session);
                await session.CommitTransactionAsync();
                return Ok();
            }
            catch(Exception)
            {
                await session.AbortTransactionAsync();
                return BadRequest();
            }
        }
    }



    [HttpPost("get_story")]
public async Task<IActionResult> GetStoryFromFriend([FromForm] List<String> list_story_in_user)
{
    Console.WriteLine(JsonConvert.SerializeObject(list_story_in_user));
    
    using (var session = await _mongo_client.StartSessionAsync())
    {
        try
        {
            session.StartTransaction();
            
            var user_id = User.FindFirst("UserId")?.Value; 

            if (string.IsNullOrEmpty(user_id))
            {
                await session.AbortTransactionAsync();
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
            }

            User user = await _user_serivice.GetUserDataByUserIdAsync(user_id, session);
            if (user == null)
            {
                await session.AbortTransactionAsync();
                return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
            }
            
            var list_friend = user.Friends;
            List<Story> list_story = new List<Story>();

            // Lấy các story của người dùng
            var my_stories = await _story_service.GetMyStoryAsync(user_id, session);
            foreach (var story in my_stories)
            {
                if (!list_story_in_user.Contains(story.Id))
                {
                    list_story.Add(story);
                }
            }

            // Lấy các story của bạn bè
            foreach (string friend in list_friend)
            {
                var stories = await _story_service.GetStoryFromUserIdAsync(user_id, friend, session);
                foreach (var story in stories)
                {
                    if (!list_story_in_user.Contains(story.Id))
                    {
                        list_story.Add(story);
                    }
                }
            }

            // // Sắp xếp theo created_at giảm dần
            // list_story = list_story.OrderByDescending(story => story.created_at).ToList();

            // // Xử lý hình ảnh
            // for (int i = 0; i < list_story.Count; i++)
            // {
            //     if (i >= 3)
            //     {
            //         list_story[i].ImageURL = ""; // Bỏ hình ảnh với các phần tử còn lại
            //     }
            // }

            await session.CommitTransactionAsync();
            return Ok(list_story);
        }
        catch (Exception)
        {
            await session.AbortTransactionAsync();
            return BadRequest();
        }
    }
}


    [Authorize]
    [HttpPost("get_story/image")]
    public async Task<ActionResult<Story>> GetStoryImage([FromForm] string story_id)
    {
        try
        {
            var user_id = User.FindFirst("UserId")?.Value; 

            if (string.IsNullOrEmpty(user_id))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng");
            }
            var story = await _story_service.GetStoryAsync(story_id);
            if (story == null)
            {
                return NotFound();
            }
            return story;
        }
        catch
        {
            return BadRequest();
        }
    }

    [Authorize]
    [HttpPost("send_story_message")]
    public async Task<ActionResult> SendStoryMessage([FromForm] string story_id, [FromForm] string content)
    {
        using var session = await _mongo_client.StartSessionAsync();
        try
        {
            session.StartTransaction();

            var user_id = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(user_id))
            {
                return Unauthorized("Không tìm thấy thông tin người dùng");
            }

            var story = await _story_service.GetStoryAsync(story_id);
            if (story == null)
            {
                await session.AbortTransactionAsync();
                return NotFound("Không tìm thấy câu chuyện");
            }

            var conversation = await _user_conservation_service.GetConversationByParticipantsAsync(user_id, story.UserId);
            if (conversation == null)
            {
                await session.AbortTransactionAsync();
                return NotFound("Không tìm thấy cuộc trò chuyện");
            }

            var message = new Message
            {
                SenderId = user_id,
                ConversationId = conversation.Id,
                Content = content,
                ReplyToStoryId = story_id
            };

            // Tạo tin nhắn và kiểm tra
            await _messageService.CreatMessageAsync(message, session);
            var createdMessage = await _messageService.GetMessageByIdAsync(message.Id, session);
            if (createdMessage == null)
            {
                await session.AbortTransactionAsync();
                return BadRequest("Không thể tạo tin nhắn");
            }

            // Kiểm tra người gửi có trong cuộc trò chuyện
            var isSenderInConversation = conversation.Participants.Contains(message.SenderId);
            if (!isSenderInConversation)
            {
                await session.AbortTransactionAsync();
                return BadRequest("Người gửi không thuộc cuộc trò chuyện");
            }

            // Thêm tin nhắn vào cuộc trò chuyện
            await _conversation_service.AddMessageToConversationAsync(message, session);

            // Gửi tin nhắn tới tất cả người tham gia cuộc trò chuyện
            var participants = await _conversation_service.GetListParticipants(message.ConversationId, session);
            var addMessageTasks = participants.Select(participant =>
                _user_conservation_service.AddNewMessageAsync(participant, message, session));
            await Task.WhenAll(addMessageTasks);

            await session.CommitTransactionAsync();
            return Ok("Tin nhắn đã được gửi thành công");
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return BadRequest($"Đã xảy ra lỗi: {ex.Message}");
        }
    }

}