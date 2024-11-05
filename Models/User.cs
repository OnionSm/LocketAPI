using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = "";

    [Range(0, 120, ErrorMessage = "Age must be between 0 and 120")]
    public int Age { get; set; }

    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
    public string Gender { get; set; } = "";

    [RegularExpression(@"^\d+$", ErrorMessage = "PhoneNumber must contain only numbers")]
    public string PhoneNumber { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [RegularExpression(@"^[\w\.\-]+@gmail\.com$", ErrorMessage = "Email must be a Gmail address")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Email password is required")]
    public string Password { get; set; } = "";
    public string UserAvatarURL {get; set;} = "";
    public List<string> Friends {get; set;} = new List<string>();
    
    public List<string> conversations {get; set;} = new List<string>();
}
