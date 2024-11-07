using Microsoft.AspNetCore.Components;
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
    public async Task CreateUserAsync(User user, IClientSessionHandle session)
    {
        await _usersCollection.InsertOneAsync(session, user);
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
    public async Task<bool> UpdateUserAsync(string id, User updatedUser, IClientSessionHandle session)
    {
        var result = await _usersCollection.ReplaceOneAsync(session, user => user.Id == id, updatedUser);
        return result.IsAcknowledged && result.ModifiedCount > 0;
    }

    public async Task<bool> AddNewFriendAsync(string user_id_1, string user_id_2, IClientSessionHandle session)
    {
        var users = await _usersCollection
                            .Find(session, u => u.Id == user_id_1 || u.Id == user_id_2)
                            .ToListAsync();

        var user_1 = users.FirstOrDefault(u => u.Id == user_id_1);
        var user_2 = users.FirstOrDefault(u => u.Id == user_id_2);

        if (user_1 == null || user_2 == null)
        {
            return false; 
        }
        user_1.Friends.Add(user_id_2);
        user_2.Friends.Add(user_id_1);

     
        var update_user_1 = Builders<User>.Update.Set(u => u.Friends, user_1.Friends);
        var update_user_2 = Builders<User>.Update.Set(u => u.Friends, user_2.Friends);

        var update_result_1 = await _usersCollection.UpdateOneAsync(session, u => u.Id == user_id_1, update_user_1);
        var update_result_2 = await _usersCollection.UpdateOneAsync(session, u => u.Id == user_id_2, update_user_2);

        // Kiểm tra kết quả của cả hai thao tác
        return update_result_1.IsAcknowledged && update_result_1.ModifiedCount > 0
            && update_result_2.IsAcknowledged && update_result_2.ModifiedCount > 0;
    }

    public async Task<bool> DeleteUserAsync(string id, IClientSessionHandle session)
    {
        var result = await _usersCollection.DeleteOneAsync(session, user => user.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public async Task<User> GetUserByPhoneNumberAsync(string phone_number, IClientSessionHandle session)
    {
        var result = await _usersCollection.Find(session, user => user.PhoneNumber == phone_number).FirstOrDefaultAsync();
        return result;
    }

    public async Task<User> GetUserByEmailAsync(string email, IClientSessionHandle session)
    {
        return await _usersCollection.Find(session, user => user.Email == email).FirstOrDefaultAsync();
    }

}
