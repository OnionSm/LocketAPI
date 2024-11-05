using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

public class Story
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("user_id")]
    public string UserId {get; set;} = "";

    [BsonElement("receivers")]
    public List<string> Receivers {get; set;} = new List<string>();

    [BsonElement("image_url")]
    public string ImageURL {get; set;} = "";

    [BsonElement("description")]
    public string Description{get; set;} = "";

    [BsonElement("created_at")]
    public DateTime created_at {get; set;} = DateTime.UtcNow;

    
}
