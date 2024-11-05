using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Chat 
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {get; set;} = ObjectId.GenerateNewId().ToString();
    public List<string> ParticipantIds {get; set;} = new List<string>();
    public List<Message> ChatMessages {get; set;} = new List<Message>();
}