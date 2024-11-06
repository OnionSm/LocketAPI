using MongoDB.Driver;

public class UserFriendRequestService 
{
    private readonly IMongoCollection<UserFriendRequest> _user_friend_request_collection;
    public UserFriendRequestService(IMongoDatabase database)
    {
        _user_friend_request_collection = database.GetCollection<UserFriendRequest>("UserFriendRequest");
    }

    // CREATE 
    public async Task CreateNewUserFriendRequestAsync(UserFriendRequest user_friend_request)
    {
        await _user_friend_request_collection.InsertOneAsync(user_friend_request);
    }
    // READ
    public async Task<List<UserFriendRequest>> GetAllUserFriendRequestAsync()
    {
        return await _user_friend_request_collection.Find(u => true).ToListAsync();
    }

    public async Task<UserFriendRequest> GetUserFriendRequestByUserIdAsync(string user_id)
    {
        return await _user_friend_request_collection.Find(u => u.UserId == user_id).FirstOrDefaultAsync();
    }

    public async Task<bool> AddNewFriendRequestAsync(FriendRequest request)
    {
        var user_1_friend_request =  await GetUserFriendRequestByUserIdAsync(request.SenderId);
        var user_2_friend_request = await GetUserFriendRequestByUserIdAsync(request.ReceiverId);

        if(user_1_friend_request == null ||
        user_2_friend_request == null)
        {
            return false;
        }

        // Check add friend invitation which was sent by sender or receiver of current request?
        var check_existence_1 = user_1_friend_request.FriendRequests
        .Find(r => r.SenderId == request.SenderId && r.ReceiverId == request.ReceiverId);
        var check_existence_2 = user_1_friend_request.FriendRequests
        .Find(r => r.SenderId == request.SenderId && r.ReceiverId == request.ReceiverId);

        if(check_existence_1 != null || check_existence_2 != null)
        {
            return false;
        }

        user_1_friend_request.FriendRequests.Add(request);
        user_2_friend_request.FriendRequests.Add(request);

        var update_data_1 = Builders<UserFriendRequest>.Update
        .Set(u => u.FriendRequests, user_1_friend_request.FriendRequests);

        var update_data_2 = Builders<UserFriendRequest>.Update
        .Set(u => u.FriendRequests, user_2_friend_request.FriendRequests);

        var update_result_1 = await _user_friend_request_collection
        .UpdateOneAsync(u => u.UserId == request.SenderId, update_data_1);
        var update_result_2 = await _user_friend_request_collection
        .UpdateOneAsync(u => u.UserId == request.ReceiverId, update_data_2);
        
        return update_result_1.IsAcknowledged && update_result_1.ModifiedCount > 0
            && update_result_2.IsAcknowledged && update_result_2.ModifiedCount > 0;
    } 

    public async Task<bool> ResponeFriendRequestAsync(FriendRequest request, bool is_accept)
    {
        var user_1_friend_request =  await GetUserFriendRequestByUserIdAsync(request.SenderId);
        var user_2_friend_request = await GetUserFriendRequestByUserIdAsync(request.ReceiverId);

        if(user_1_friend_request == null ||
        user_2_friend_request == null)
        {
            return false;
        }

        foreach(var rq in user_1_friend_request.FriendRequests)
        {
            if(rq.Id == request.Id)
            {
                rq.UpdateAt = DateTime.UtcNow;
                rq.ResponeStatus = is_accept? AddFriendResponeStatus.ACCEPTED : AddFriendResponeStatus.DENIED;
            }
        }

        foreach(var rq in user_2_friend_request.FriendRequests)
        {
            if(rq.Id == request.Id)
            {
                rq.UpdateAt = DateTime.UtcNow;
                rq.ResponeStatus = is_accept? AddFriendResponeStatus.ACCEPTED : AddFriendResponeStatus.DENIED;
            }
        }

        var update_data_1 = Builders<UserFriendRequest>.Update
        .Set(u => u.FriendRequests, user_1_friend_request.FriendRequests);

        var update_data_2 = Builders<UserFriendRequest>.Update
        .Set(u => u.FriendRequests, user_2_friend_request.FriendRequests);

        var update_result_1 = await _user_friend_request_collection
        .UpdateOneAsync(u => u.UserId == request.SenderId, update_data_1);
        var update_result_2 = await _user_friend_request_collection
        .UpdateOneAsync(u => u.UserId == request.ReceiverId, update_data_2);
        
        return update_result_1.IsAcknowledged && update_result_1.ModifiedCount > 0
            && update_result_2.IsAcknowledged && update_result_2.ModifiedCount > 0;
    }

}