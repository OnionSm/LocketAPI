using MongoDB.Driver;

public class MessageService
{
    private readonly IMongoCollection<Message> _messageCollection;
    private readonly IMongoCollection<User> _userCollection;
    public MessageService(IMongoDatabase database)
    {
        _messageCollection = database.GetCollection<Message>("Message");
        _userCollection = database.GetCollection<User>("User");
    }

    //CREATE
    public async Task CreatMessageAsync(Message message)
    {
        await _messageCollection.InsertOneAsync(message);
    }

    //READ
    public async Task<Message> GetMessageByIdAsync(string id)
    {
        return await _messageCollection.Find(message => message.Id == id).FirstOrDefaultAsync();
    }
    public async Task<List<Message>> GetAllUserMessageSendedAsync(string id)
    {
        return await _messageCollection.Find(message => message.SenderId == id).ToListAsync();
    }

    // public async Task<List<Message>> GetAllUserMessageReceivedAsync(string id)
    // {
    //     return await _messageCollection.Find(message => message.ReceiverId == id).ToListAsync();
    // }

    // public async Task<List<Message>> GetAllMessageSendedFrom2UserAsync(string senderid, string receivedid)
    // {
    //     return await _messageCollection.Find(message => message.SenderId == senderid && message.ReceiverId == receivedid).ToListAsync();
    // }
    // public async Task<List<Message>> GetAllMessageFrom2UserAsync(string user1, string user2)
    // {
    //     return await _messageCollection.Find(message => (
    //         message.SenderId == user1 || message.SenderId == user2 && message.ReceiverId == user1 || message.ReceiverId == user2
    //         )).ToListAsync();
    // }
    
    //UPDATE
    public async Task<bool> UpdateOneMessageAysnc(string userid, Message messageUpdate)
    {
        var updateDefinition = Builders<Message>.Update
        .Set(m => m.Content, messageUpdate.Content);

        var result = await _messageCollection.UpdateOneAsync(
        message => message.SenderId == userid,
        updateDefinition);

        return result.IsAcknowledged && result.ModifiedCount> 0;
    }


    //DELETE 
    public async Task<bool> DeleteOneMessageAsync(string messageId)
    {
        var result = await _messageCollection.DeleteOneAsync(message => message.Id == messageId);

        return result.IsAcknowledged && result.DeletedCount > 0;
    }

}