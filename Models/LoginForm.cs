using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
public class LoginForm
{
    public string Email { get; set; } = "";
    public string Password { get; set;}  = "";
}