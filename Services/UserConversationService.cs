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
    public async Task<bool> CreateUserConversationAsync(UserConversation user_conversation)
    {
        try
        {
            await _user_conversation_collection.InsertOneAsync(user_conversation);
            return true; 
        }
        catch (Exception)
        {
            return false;
        }
    }

    // READ
    public async Task<List<UserConversation>> GetAllUserConversationAsync()
    {
        return await _user_conversation_collection.Find(userconversation => true).ToListAsync();
    }

    public async Task<UserConversation> GetUserConversationByIdAsync(string id)
    {
        return await _user_conversation_collection.Find(userconversation => userconversation.Id == id).FirstOrDefaultAsync();
    }

    public async Task<UserConversation> GetUserConversationByUserId(string user_id)
    {
        return await _user_conversation_collection.Find(userconversation => userconversation.UserId == user_id).FirstOrDefaultAsync();
    }

    // UPDATE
    public async Task<bool> AddNewMessageAsync(string user_id, Message message)
    {
        // Tìm kiếm tài liệu UserConversation theo user_id
        var user_conversation = await _user_conversation_collection
            .Find(conversation => conversation.UserId == user_id)
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

        // Tạo UpdateDefinition để cập nhật danh sách UserConversations
        var updateDefinition = Builders<UserConversation>.Update
            .Set(u => u.UserConversations, user_conversation.UserConversations);

        // Cập nhật tài liệu trong MongoDB
        var result = await _user_conversation_collection
            .UpdateOneAsync(
                userconversation => userconversation.UserId == user_id,
                updateDefinition
            );

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }


    // DELETE
    
}