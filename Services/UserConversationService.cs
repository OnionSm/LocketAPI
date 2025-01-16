using MongoDB.Driver;
using System.Text.Json;


public class NewMessageRequest 
{
    public string conversation_id = "";
    public string message_id = "";
}
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

    public async Task<UserConversation> GetUserConversationByIdAsync(string id)
    {
        return await _user_conversation_collection.Find(userconversation => userconversation.UserId == id).FirstOrDefaultAsync();
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

    public async Task<UserConversation> GetListLatestMessageAsync(string user_id, string conversation_id, string message_id)
    {
        // Lấy user conversation từ database
        var userconversation = await _user_conversation_collection
            .Find(u => u.UserId == user_id)
            .FirstOrDefaultAsync();

        if (userconversation == null || userconversation.UserConversations == null)
        {
            Console.WriteLine("user conversation null");
            return new UserConversation();
        }

        // Tạo đối tượng UserConversation
        var user_conversation = new UserConversation
        {
            Id = userconversation.Id,
            UserId = userconversation.UserId,
            UserConversations = new List<Conversation>()
        };
        
        // Tìm conversation tương ứng trên server
        var conversation = userconversation.UserConversations
            .FirstOrDefault(c => c.Id == conversation_id);

        if (conversation != null)
        {
            // Lấy tất cả tin nhắn mới hơn `item.message_id` bằng LINQ
            var list_message = conversation.ListMessages
                .SkipWhile(m => m.Id != message_id) // Bỏ qua đến khi gặp message_id
                .Skip(1) // Bỏ qua message hiện tại
                .ToList();

            if (list_message.Count > 0)
            {
                // Tạo đối tượng Conversation mới
                var new_conversation = new Conversation
                {
                    Id = conversation.Id,
                    Participants = conversation.Participants,
                    ListMessages = list_message,
                    LastMessage = list_message.Last(),
                    CreatedAt = conversation.CreatedAt,
                    UpdatedAt = list_message.Last().SendAt
                };

                user_conversation.UserConversations.Add(new_conversation);
            }
        }
        
        Console.WriteLine("OK");
        return user_conversation;
    }

    public async Task<List<Message>> LoadOlderMessageAsync(string user_id, string conversation_id, string local_oldest_message)
    {
        // Lấy user conversation từ database
        var userconversation = await _user_conversation_collection
            .Find(u => u.UserId == user_id)
            .FirstOrDefaultAsync();

        if (userconversation == null || userconversation.UserConversations == null)
            return new List<Message>();

        // Tìm conversation tương ứng trên server
        var conversation = userconversation.UserConversations
            .FirstOrDefault(c => c.Id == conversation_id);

        if (conversation == null || conversation.ListMessages == null)
            return new List<Message>();

        // Tìm vị trí của local_oldest_message
        int index = conversation.ListMessages.FindIndex(m => m.Id == local_oldest_message);
        if (index == -1)
            return new List<Message>(); // Không tìm thấy local_oldest_message

        // Lấy tối đa 30 tin nhắn cũ
        int count = Math.Min(30, index); // Số lượng tin nhắn cần lấy
        return conversation.ListMessages.GetRange(index - count, count);
    }

    // public async Task<UserConversation> GetInitMessageAsync(string user_id)
    // {
    //     var userconversation = await _user_conversation_collection.Find(u => u.UserId == user_id).FirstOrDefaultAsync();
    //     if (userconversation == null)
    //     {
    //         return new UserConversation(); // Hoặc có thể trả về UserConversation mặc định nếu cần.
    //     }

    //     // Tạo đối tượng UserConversation
    //     var user_conversation = new UserConversation
    //     {
    //         Id = userconversation.Id,
    //         UserId = userconversation.UserId,
    //         UserConversations = new List<Conversation>()
    //     };

    //     foreach (var conversation in userconversation.UserConversations)
    //     {
    //         int last_message_index = conversation.ListMessages.Count - 1;
    //         if (last_message_index < 0)
    //         {
    //             continue; // Nếu danh sách tin nhắn rỗng, bỏ qua conversation này.
    //         }

    //         // Tạo danh sách tin nhắn lấy từ 20 tin cuối cùng hoặc ít hơn.
    //         int start_index = Math.Max(0, last_message_index - 20);
    //         List<Message> list_messages = conversation.ListMessages
    //             .Skip(start_index)
    //             .Take(last_message_index - start_index + 1)
    //             .ToList();

    //         // Kiểm tra danh sách tin nhắn trước khi truy cập Last().
    //         var new_conversation = new Conversation
    //         {
    //             Id = conversation.Id,
    //             Participants = conversation.Participants,
    //             ListMessages = list_messages,
    //             LastMessage = conversation.LastMessage,
    //             CreatedAt = conversation.CreatedAt,
    //             UpdatedAt = list_messages.Any() ? list_messages.Last().SendAt : conversation.UpdatedAt
    //         };

    //         user_conversation.UserConversations.Add(new_conversation);
    //     }

    //     return user_conversation;
    // }

    public async Task<UserConversation> GetLatestMessageAsync(string user_id)
    {
        // Tìm UserConversation theo user_id
        var userconversation = await _user_conversation_collection
            .Find(u => u.UserId == user_id)
            .FirstOrDefaultAsync();

        // Nếu không tìm thấy, trả về null hoặc đối tượng mặc định
        if (userconversation == null)
            return new UserConversation(); // Hoặc return new UserConversation();

        // Sao chép UserConversation bằng LINQ, nếu cần biến đổi dữ liệu
        var user_conversation = new UserConversation
        {
            Id = userconversation.Id,
            UserId = userconversation.UserId,
            UserConversations = userconversation.UserConversations.Select(conversation => new Conversation
            {
                Id = conversation.Id,
                Participants = conversation.Participants,
                ListMessages = new List<Message>(),
                LastMessage = conversation.LastMessage,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            }).ToList()
        };

        return user_conversation;
    }
}