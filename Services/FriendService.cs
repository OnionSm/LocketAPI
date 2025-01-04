using MongoDB.Driver;

public class FriendService
{
    private readonly IMongoCollection<User> _userCollection;

    public FriendService(IMongoDatabase database)
    {
        _userCollection = database.GetCollection<User>("User");
    }

    public async Task<List<object>> GetListFriendInfoAsync(string user_id, IClientSessionHandle session)
    {
        if (string.IsNullOrEmpty(user_id))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(user_id));
        }

        var user = await _userCollection.Find(session, u => u.Id == user_id).FirstOrDefaultAsync();
        if (user == null || user.Friends == null || !user.Friends.Any())
        {
            
            return new List<object>();
        }

      
        var friends = await _userCollection
            .Find(session, u => user.Friends.Contains(u.Id))
            .ToListAsync();

     
        var friendInfoList = friends
            .Where(friend => friend != null)
            .Select(friend => new
            {
                id = friend.Id,
                first_name = friend.FirstName,
                last_name = friend.LastName,
                UserAvatarURL = friend.UserAvatarURL
            })
            .ToList<object>(); 

        return friendInfoList;
    }


    public async Task<User> GetUserDataAsync(string user_id_request, string public_user_id)
    {
        return await _userCollection
            .Find(u => u.PublicUserId == public_user_id && !u.Friends.Contains(user_id_request))
            .FirstOrDefaultAsync();
    }


}