using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Conversation
{
    [BsonId] // Đặt làm ID trong MongoDB
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("participants")]
    public List<string> Participants { get; set; } = new List<string>();
    
    [BsonElement("list_messages")]
    public List<Message> ListMessages {get; set;} = new List<Message>();

    [BsonElement("last_message")]
    public Message LastMessage { get; set; } = new Message();

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
