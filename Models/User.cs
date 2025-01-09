using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("public_user_id")]
    public string PublicUserId {get; set;} = "";

    [BsonElement("first_name")]
    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = "";

    [BsonElement("last_name")]
    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = "";

    [BsonElement("age")]
    public int? Age { get; set; }

    [BsonElement("gender")]
    public string? Gender { get; set; } = "";

    [BsonElement("phone_number")]
    public string PhoneNumber { get; set; } = "";

    [BsonElement("email")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; } 

    [BsonElement("password")]
    [Required(ErrorMessage = "Email password is required")]
    public string Password { get; set; } = "";

    [BsonElement("user_avatar_url")]
    public string UserAvatarURL { get; set; } = "";
    
    [BsonElement("friends")]
    public List<string> Friends { get; set; } = new List<string>();

    [BsonElement("account_deleted")]
    public bool AccountDeleted {get; set;} = false;

    [BsonElement("show_user")]
    public bool ShowUser {get; set;} = true;
}
