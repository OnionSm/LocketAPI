using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


public class FriendRequest 
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("request_id")]
    public string Id {get; set;} = ObjectId.GenerateNewId().ToString();

    [BsonElement("sender_id")]
    public string SenderId {get; set;} = "";

    [BsonElement("receiver_id")]
    public string ReceiverId {get; set;} = "";

    [BsonElement("respone_status")]
    public AddFriendResponeStatus? ResponeStatus {get; set;}

    [BsonElement("send_at")]
    public DateTime SendAt {get; set;} = DateTime.UtcNow;

    [BsonElement("update_at")]
    public DateTime? UpdateAt {get; set;}
}