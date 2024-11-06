using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UserFriendRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {get; set;} = ObjectId.GenerateNewId().ToString();

    [BsonElement("user_id")]
    public string UserId {get; set;} = "";

    [BsonElement("friend_requests")]
    public List<FriendRequest> FriendRequests {get; set;} = new List<FriendRequest>();
}