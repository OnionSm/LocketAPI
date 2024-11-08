using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UserRefreshTokens
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {get; set;} = ObjectId.GenerateNewId().ToString();

    [BsonElement("user_id")]
    public string UserId {get; set;} = "";

    [BsonElement("refresh_tokens")]
    public List<RefreshToken> RefreshTokens {get; set;} = new List<RefreshToken>();
}