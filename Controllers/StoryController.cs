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

                var my_stories = await _story_service.GetMyStoryAsync(user_id, session);
                foreach(var story in my_stories)
                {
                    bool has_exist = false;
                    foreach(string id in list_story_in_user)
                    {
                        if(story.Id == id)
                        {
                            has_exist = true; 
                            break; 
                        }
                    }
                    if(!has_exist)
                    {
                        list_story.Add(story);
                    }
                }

                foreach (string friend in list_friend)
                {
                    var stories = await _story_service.GetStoryFromUserIdAsync(user_id, friend, session);
                    foreach (var story in stories)
                    {
                        bool has_exist = false;
                        foreach(string id in list_story_in_user)
                        {
                            if(story.Id == id)
                            {
                                has_exist = true;
                                break;
                            }
                        }
                        if(!has_exist)
                        {   
                            list_story.Add(story);
                        }
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

    [Authorize]
    [HttpGet("get_story/image")]
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

}