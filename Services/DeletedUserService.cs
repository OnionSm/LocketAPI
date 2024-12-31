    using Microsoft.AspNetCore.Components;
    using MongoDB.Driver;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Extensions.Options;

    public class DeletedUserService
    {
        
        private readonly IMongoCollection<User> _deleted_user_collection;

        public DeletedUserService(IMongoDatabase database)
        {
            _deleted_user_collection = database.GetCollection<User>("DeletedUser");
        }

       public async Task<bool> AddDeletedUserAsync(User user, IClientSessionHandle session)
        {
            try
            {
                await _deleted_user_collection.InsertOneAsync(session, user);
                return true; // Thành công
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                Console.WriteLine($"Error inserting user: {ex.Message}");
                return false; // Thất bại
            }
        }
    }