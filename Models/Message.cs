using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("conversation_id")]
    public string ConversationId {get; set;} = "";

    [BsonElement("sender_id")]
    public string SenderId {get; set;} = "";

    [BsonElement("content")]
    public string Content {get; set;} = "";

    [BsonElement("status")]
    public MessageStatus? Status {get; set;} 

    [BsonElement("send_at")]
    public DateTime SendAt {get; set;} = DateTime.UtcNow;

    [BsonElement("reply_to_story_id")]
    public string? ReplyToStoryId {get; set;} 



}
   
