using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class RefreshToken
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id {get; set;} = ObjectId.GenerateNewId().ToString();

    public string? Token { get; set; } 
    
    public DateTime ExpiryDate { get; set; }  = DateTime.UtcNow.AddDays(7);
    public bool IsRevoked { get; set; }  = false;
}