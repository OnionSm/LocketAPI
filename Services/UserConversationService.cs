using MongoDB.Driver;

public class UserConversationService
{
    private readonly IMongoCollection<UserConversation> _user_conversation_collection;
    public UserConversationService(IMongoDatabase database)
    {
        _user_conversation_collection = database.GetCollection<UserConversation>("UserConversation");
    }

    // CREATE
    // create userconservation for one user
    public async Task<bool> CreateUserConversationAsync(UserConversation user_conversation, IClientSessionHandle session)
    {
        try
        {
            await _user_conversation_collection.InsertOneAsync(session, user_conversation);
            return true; 
        }
        catch (Exception)
        {
            return false;
        }
    }

    // READ
    public async Task<List<UserConversation>> GetAllUserConversationAsync(IClientSessionHandle session)
    {
        return await _user_conversation_collection.Find(session, userconversation => true).ToListAsync();
    }

    public async Task<UserConversation> GetUserConversationByIdAsync(string id, IClientSessionHandle session)
    {
        return await _user_conversation_collection.Find(session, userconversation => userconversation.UserId == id).FirstOrDefaultAsync();
    }

    public async Task<UserConversation> GetUserConversationByUserId(string user_id, IClientSessionHandle session)
    {
        return await _user_conversation_collection.Find(session, userconversation => userconversation.UserId == user_id).FirstOrDefaultAsync();
    }

    // UPDATE
    public async Task<bool> AddNewMessageAsync(string user_id, Message message, IClientSessionHandle session)
    {
        // Tìm kiếm tài liệu UserConversation theo user_id
        var user_conversation = await _user_conversation_collection
            .Find(session, conversation => conversation.UserId == user_id)
            .FirstOrDefaultAsync();

        if (user_conversation == null)
        {
            return false;
        }

        // Tìm Conversation có Id khớp với ConversationId của tin nhắn
        var conversation = user_conversation.UserConversations
            .FirstOrDefault(c => c.Id == message.ConversationId);

        if (conversation == null)
        {
            return false; // Không tìm thấy Conversation phù hợp
        }

        // Thêm tin nhắn mới vào ListMessages của Conversation
        conversation.ListMessages.Add(message);
        conversation.LastMessage = message;
        conversation.UpdatedAt = DateTime.UtcNow;

        // Tạo UpdateDefinition để cập nhật danh sách UserConversations
        var updateDefinition = Builders<UserConversation>.Update
            .Set(u => u.UserConversations, user_conversation.UserConversations);

        // Cập nhật tài liệu trong MongoDB
        var result = await _user_conversation_collection
            .UpdateOneAsync(session,
                userconversation => userconversation.UserId == user_id,
                updateDefinition
            );
        


        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AddNewConversationAsync(Conversation conversation, IClientSessionHandle session)
    {
        bool allUpdatesSuccessful = true;

        foreach (string user in conversation.Participants)
        {
            var userConversation = await _user_conversation_collection
                .Find(session, uc => uc.UserId == user).FirstOrDefaultAsync();

            if (userConversation == null)
            {
                allUpdatesSuccessful = false;
                break;
            }

            userConversation.UserConversations.Add(conversation);

            var updateData = Builders<UserConversation>.Update
                .Set(uc => uc.UserConversations, userConversation.UserConversations);

            var updateResult = await _user_conversation_collection.UpdateOneAsync(session,
                uc => uc.UserId == user, updateData);

            if (!updateResult.IsAcknowledged || updateResult.ModifiedCount == 0)
            {
                allUpdatesSuccessful = false;
                break;
            }
        }
        return allUpdatesSuccessful;
    }

    // DELETE
    
}