using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ConversationRespone
{
    [BsonId] // Đặt làm ID trong MongoDB
    public string Id { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string GroupAvatarUrl { get; set; } = "";
    [BsonElement("participants")]
    public List<string> Participants { get; set; } = new List<string>();

    [BsonElement("list_messages")]
    public List<Message> ListMessages { get; set; } = new List<Message>();

    [BsonElement("last_message")]
    public Message? LastMessage { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
