using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UserSession
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("session_id")]
    public string Id {get; set;} = ObjectId.GenerateNewId().ToString();

    [BsonElement("user_id")]
    public string UserId {get; set;} = "";

    [BsonElement("device_info")]
    public string DeviceInfo {get; set;} = "";

    [BsonElement("ip_address")]
    public string IpAddress {get; set;} = "";

    [BsonElement("login_time")]
    public DateTime LoginTime {get; set;} = DateTime.UtcNow;

    [BsonElement("logout_time")]
    public DateTime? LogoutTime {get; set;}
}