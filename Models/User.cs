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
    // [Range(0, 120, ErrorMessage = "Age must be between 0 and 120")]
    public int? Age { get; set; }

    [BsonElement("gender")]
    // [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
    public string? Gender { get; set; } = "";

    [BsonElement("phone_number")]
    [RegularExpression(@"^\d+$", ErrorMessage = "PhoneNumber must contain only numbers")]
    public string? PhoneNumber { get; set; } 

    [BsonElement("email")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [RegularExpression(@"^[\w\.\-]+@gmail\.com$", ErrorMessage = "Email must be a Gmail address")]
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
}
