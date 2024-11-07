using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Driver;

public class ConversationService 
{
    private readonly IMongoCollection<Conversation> _conversation_collection;
    public ConversationService(IMongoDatabase database)
    {
        _conversation_collection = database.GetCollection<Conversation>("Conversation");
    }

    // CREATE 
    public async Task<bool> CreateNewConversationAsync(Conversation conversation, IClientSessionHandle session)
    {
        try
        {
            await _conversation_collection.InsertOneAsync(session, conversation);
            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }

    // READ
    public async Task<List<Conversation>> GetAllConversationAsync(IClientSessionHandle session)
    {
        return await _conversation_collection.Find(session, conversation => true).ToListAsync();
    }

    public async Task<Conversation> GetConversationByIdAsync(string id, IClientSessionHandle session)
    {
        return await _conversation_collection.Find(session, conversation => conversation.Id == id).FirstOrDefaultAsync();
    }

    // UPDATE
    public async Task<bool> UpdateParticipantsAsync(string id, List<string> new_participants, IClientSessionHandle session)
    {
        var updateDefinition = Builders<Conversation>.Update
        .Set(m => m.Participants, new_participants);
        
        var result = await _conversation_collection.UpdateOneAsync(session, conversation => conversation.Id == id, updateDefinition);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AddMessageToConversationAsync(Message new_message, IClientSessionHandle session)
    {
        var conversation = await _conversation_collection
            .Find(conversation => conversation.Id == new_message.ConversationId)
            .FirstOrDefaultAsync();

        if (conversation == null || conversation.ListMessages == null)
        {
            return false; 
        }

        List<Message> list_messages = conversation.ListMessages;
        list_messages.Add(new_message);

        var updateDefinition = Builders<Conversation>.Update
        .Set(conversation => conversation.ListMessages, list_messages)
        .Set(conversation => conversation.LastMessage, new_message)
        .Set(conversation => conversation.UpdatedAt, new_message.SendAt);

      
        var result = await _conversation_collection
        .UpdateOneAsync(session, conversation => conversation.Id == new_message.ConversationId, updateDefinition);

        return result.IsAcknowledged && result.ModifiedCount > 0;
    }
    
    // GET
    public async Task<List<string>> GetListParticipants(string id, IClientSessionHandle session)
    {
        Conversation conversation = await _conversation_collection
            .Find(session, c => c.Id == id)
            .FirstOrDefaultAsync();

        if (conversation != null)
        {
            return conversation.Participants;
        }
        return new List<string>(); 
    }

}