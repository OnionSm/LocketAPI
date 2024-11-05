using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

public class UserService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UserService(IMongoDatabase database)
    {
        _usersCollection = database.GetCollection<User>("User");
    }

    // CREATE
    public async Task CreateUserAsync(User user)
    {
        await _usersCollection.InsertOneAsync(user);
    }

    // READ - Retrieve all users
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _usersCollection.Find(user => true).ToListAsync();
    }

    // READ - Retrieve user by ID
    public async Task<User> GetUserByIdAsync(string id)
    {
        return await _usersCollection.Find(user => user.Id == id).FirstOrDefaultAsync();
    }

    // UPDATE
    public async Task<bool> UpdateUserAsync(string id, User updatedUser)
    {
        var result = await _usersCollection.ReplaceOneAsync(user => user.Id == id, updatedUser);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    // DELETE
    public async Task<bool> DeleteUserAsync(string id)
    {
        var result = await _usersCollection.DeleteOneAsync(user => user.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }
}
