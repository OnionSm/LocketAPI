using MongoDB.Driver;

public class FriendService
{
    private readonly IMongoCollection<User> _userCollection;

    public FriendService(IMongoDatabase database)
    {
        _userCollection = database.GetCollection<User>("User");
    }

    public async Task<Dictionary<string, Dictionary<string, string>>> GetListFriendInfoAsync(string user_id, IClientSessionHandle session)
    {
        if (string.IsNullOrEmpty(user_id))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(user_id));
        }
        var user = await _userCollection.Find(session, u => u.Id == user_id).FirstOrDefaultAsync();
        if (user == null || user.Friends == null || !user.Friends.Any())
        {
            return new Dictionary<string, Dictionary<string, string>>();
        }

        var friends = await _userCollection.Find(u => user.Friends.Contains(u.Id)).ToListAsync();

        var list_friend_info = friends
            .Where(info => info != null) 
            .ToDictionary(
                info => info.Id, 
                info => new Dictionary<string, string> 
                {
                    { "FullName", $"{info.FirstName} {info.LastName}" },
                    { "UserAvatarURL", info.UserAvatarURL ?? "default_avatar_url" } 
                }
            );
        return list_friend_info;
    }


}