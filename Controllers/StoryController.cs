using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;

[Authorize]
[Route("api/story")]
[ApiController]
public class StoryController: ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly StoryService _story_service;
    private readonly UserService _user_serivice;

    public StoryController(IMongoClient client, StoryService story_service, UserService user_service)
    {
        _mongo_client = client;
        _story_service = story_service;
        _user_serivice = user_service;
    }


    [HttpPost("create_story")]
    public async Task<IActionResult> CreateNewStory([FromForm] Story story)
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
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



    [HttpGet("get_story")]
    public async Task<IActionResult> GetStoryFromFriend()
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

                User user = await _user_serivice.GetUserDataByUserIdAsync(user_id, session);
                if (user == null)
                {
                    await session.AbortTransactionAsync();
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                
                var list_friend = user.Friends;
                List<Story> list_story = new List<Story>();

                foreach (string friend in list_friend)
                {
                    var stories = await _story_service.GetStoryFromUserIdAsync(user_id, friend, session);
                    foreach (var story in stories)
                    {
                        list_story.Add(story);
                    }
                }
                await session.CommitTransactionAsync();
                return Ok(list_story);
            }
            catch(Exception)
            {
                await session.AbortTransactionAsync();
                return BadRequest();
            }
        }
    }

}