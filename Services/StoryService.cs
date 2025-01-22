using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

public class StoryService 
{
    private readonly IMongoClient _mongo_client;
    private readonly IMongoCollection<Story> _story_collection;

    public StoryService(IMongoClient client, IMongoDatabase database)
    {
        _mongo_client = client;
        _story_collection = database.GetCollection<Story>("Story");
    }

    public async Task CreateNewStoryAsync(Story story, IClientSessionHandle session)
    {
        await _story_collection.InsertOneAsync(session, story);
    }

    // Lấy danh sách story bạn bè
    public async Task<List<Story>> GetStoryFromUserIdAsync(string user_id, string uploader, IClientSessionHandle session)
    {
        List<Story> list_story = await _story_collection
            .Find(s => s.UserId == uploader && s.Receivers.Contains(user_id))
            .ToListAsync();

        return list_story;
    }

    // Lấy danh sách story của bản thân 
    public async Task<List<Story>> GetMyStoryAsync(string user_id, IClientSessionHandle session)
    {
        List<Story> list_story = await _story_collection
            .Find(s => s.UserId == user_id)
            .ToListAsync();
        return list_story;
    }

    public async Task<Story> GetStoryAsync(string story_id)
    {
        var story = await _story_collection.Find(s => s.Id == story_id).FirstOrDefaultAsync();
        return story;
    }

}