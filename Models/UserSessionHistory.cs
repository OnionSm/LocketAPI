using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UserSessionHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("user_session_history_id")]
    public string Id { get; set;} = ObjectId.GenerateNewId().ToString();

    [BsonElement("user_id")]
    public string UserId {get; set;} = "";
    
    [BsonElement("user_sessions")]
    public List<UserSession> UserSessions {get; set;} = new List<UserSession>();

    [BsonElement("current_session")]
    public UserSession? CurrentSession {get; set;}

}