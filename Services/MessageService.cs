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
    public async Task CreatMessageAsync(Message message, IClientSessionHandle session)
    {
        await _messageCollection.InsertOneAsync(session, message);
    }

    //READ
    public async Task<Message> GetMessageByIdAsync(string id, IClientSessionHandle session)
    {
        return await _messageCollection.Find(session, message => message.Id == id).FirstOrDefaultAsync();
    }
    public async Task<List<Message>> GetAllUserMessageSendedAsync(string id, IClientSessionHandle session)
    {
        return await _messageCollection.Find(session, message => message.SenderId == id).ToListAsync();
    }

    //UPDATE
    public async Task<bool> UpdateOneMessageAysnc(string userid, Message messageUpdate, IClientSessionHandle session)
    {
        var updateDefinition = Builders<Message>.Update
        .Set(m => m.Content, messageUpdate.Content);

        var result = await _messageCollection.UpdateOneAsync(session,
        message => message.SenderId == userid,
        updateDefinition);

        return result.IsAcknowledged && result.ModifiedCount> 0;
    }


    //DELETE 
    public async Task<bool> DeleteOneMessageAsync(string messageId, IClientSessionHandle session)
    {
        var result = await _messageCollection.DeleteOneAsync(session, message => message.Id == messageId);

        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}