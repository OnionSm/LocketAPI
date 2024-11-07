using MongoDB.Bson;
using MongoDB.Driver;

public class UserFriendRequestService 
{
    private readonly IMongoCollection<UserFriendRequest> _user_friend_request_collection;
    public UserFriendRequestService(IMongoDatabase database)
    {
        _user_friend_request_collection = database.GetCollection<UserFriendRequest>("UserFriendRequest");
    }

    // CREATE 
    public async Task CreateNewUserFriendRequestAsync(UserFriendRequest user_friend_request, IClientSessionHandle session)
    {
        try
        {
            await _user_friend_request_collection.InsertOneAsync(session, user_friend_request);
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
        }
        
    }
    // READ
    public async Task<List<UserFriendRequest>> GetAllUserFriendRequestAsync(IClientSessionHandle session)
    {
        return await _user_friend_request_collection.Find(session, u => true).ToListAsync();
        
    }

    public async Task<UserFriendRequest> GetUserFriendRequestByUserIdAsync(string user_id, IClientSessionHandle session)
    {
        return await _user_friend_request_collection.Find(session, u => u.UserId == user_id).FirstOrDefaultAsync();
    }

    public async Task<bool> AddNewFriendRequestAsync(FriendRequest request, IClientSessionHandle session)
    {
        var user_1_friend_request =  await GetUserFriendRequestByUserIdAsync(request.SenderId, session);
        var user_2_friend_request = await GetUserFriendRequestByUserIdAsync(request.ReceiverId, session);

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
        .UpdateOneAsync(session, u => u.UserId == request.SenderId, update_data_1);
        var update_result_2 = await _user_friend_request_collection
        .UpdateOneAsync(session, u => u.UserId == request.ReceiverId, update_data_2);
        
        return update_result_1.IsAcknowledged && update_result_1.ModifiedCount > 0
            && update_result_2.IsAcknowledged && update_result_2.ModifiedCount > 0;
    } 

    public async Task<bool> ResponeFriendRequestAsync(string request_id, 
    string sender_id, 
    string receiver_id, 
    bool state, 
    IClientSessionHandle session)
    {
        // Find friend resquest form
        var user_send_respone_fr = await GetUserFriendRequestByUserIdAsync(sender_id, session); // who accept the friend invitation
        var user_receive_respone_fr = await GetUserFriendRequestByUserIdAsync(receiver_id, session); // who send the friend invitation

        var request = user_send_respone_fr.FriendRequests.FirstOrDefault(rq => rq.ReceiverId == sender_id || rq.SenderId == receiver_id);
        if(request == null)
        {
            return false;
        }

        if (user_send_respone_fr == null ||  user_receive_respone_fr == null)
        {
            return false;
        }

        foreach (var rq in user_send_respone_fr.FriendRequests)
        {
            if (rq.Id == request_id)
            {
                rq.UpdateAt = DateTime.UtcNow;
                rq.ResponeStatus = state ? AddFriendResponeStatus.ACCEPTED : AddFriendResponeStatus.DENIED;
            }
        }

        foreach (var rq in user_receive_respone_fr.FriendRequests)
        {
            if (rq.Id == request_id)
            {
                rq.UpdateAt = DateTime.UtcNow;
                rq.ResponeStatus = state ? AddFriendResponeStatus.ACCEPTED : AddFriendResponeStatus.DENIED;
            }
        }

        var respone_sender_data = Builders<UserFriendRequest>.Update
            .Set(u => u.FriendRequests, user_send_respone_fr.FriendRequests);

        var respone_receiver_data = Builders<UserFriendRequest>.Update
            .Set(u => u.FriendRequests, user_receive_respone_fr.FriendRequests);

        var sender_result = await _user_friend_request_collection
            .UpdateOneAsync(session, u => u.UserId == request.ReceiverId,respone_sender_data);
        var receiver_result = await _user_friend_request_collection
            .UpdateOneAsync(session, u => u.UserId == request.SenderId, respone_receiver_data);

        return sender_result.IsAcknowledged && sender_result.ModifiedCount > 0
            && receiver_result.IsAcknowledged && receiver_result.ModifiedCount > 0;
    }

}