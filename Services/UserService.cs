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

    public async Task<bool> AddNewFriendAsync(string user_id_1, string user_id_2)
    {
        // Tìm cả hai người dùng trong một lần gọi
        var users = await _usersCollection
                            .Find(u => u.Id == user_id_1 || u.Id == user_id_2)
                            .ToListAsync();

        // Kiểm tra xem có đủ dữ liệu người dùng không
        var user_1 = users.FirstOrDefault(u => u.Id == user_id_1);
        var user_2 = users.FirstOrDefault(u => u.Id == user_id_2);

        if (user_1 == null || user_2 == null)
        {
            return false; // Một trong các người dùng không tồn tại
        }

        // Cập nhật danh sách bạn bè cho user_1 và user_2
        user_1.Friends.Add(user_id_2);
        user_2.Friends.Add(user_id_1);

        // Tạo cập nhật dữ liệu cho cả hai người dùng
        var update_user_1 = Builders<User>.Update.Set(u => u.Friends, user_1.Friends);
        var update_user_2 = Builders<User>.Update.Set(u => u.Friends, user_2.Friends);

        // Thực hiện cả hai cập nhật trong một lần
        var update_result_1 = await _usersCollection.UpdateOneAsync(u => u.Id == user_id_1, update_user_1);
        var update_result_2 = await _usersCollection.UpdateOneAsync(u => u.Id == user_id_2, update_user_2);

        // Kiểm tra kết quả của cả hai thao tác
        return update_result_1.IsAcknowledged && update_result_1.ModifiedCount > 0
            && update_result_2.IsAcknowledged && update_result_2.ModifiedCount > 0;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var result = await _usersCollection.DeleteOneAsync(user => user.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    public async Task<User> GetUserByPhoneNumberAsync(string phone_number)
    {
        var result = await _usersCollection.Find(user => user.PhoneNumber == phone_number).FirstOrDefaultAsync();
        return result;
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
    }

}
