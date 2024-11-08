using MongoDB.Driver.Authentication;
using MongoDB.Driver;

public class LoginService
{
    private readonly IMongoCollection<User> _user_collection;
    public LoginService(IMongoDatabase database)
    {
        _user_collection = database.GetCollection<User>("User");
    }

    public async Task<User> AuthenticateByEmailAsync(string email, string password)
    {
        var user = await _user_collection.Find(u => u.Email == email && u.Password == password).FirstOrDefaultAsync();
        return user;
    }

    
}



