using MongoDB.Driver;

public class UserSessionService
{
    private readonly IMongoCollection<UserSessionHistory> _user_session_history_collection;
    public UserSessionService(IMongoDatabase database)
    {
        _user_session_history_collection = database.GetCollection<UserSessionHistory>("UserSessionHistory");
    }

    public async Task<bool> CreateNewUserSessionHistoryAsync(UserSessionHistory ush, IClientSessionHandle session)
    {
        try
        {
            await _user_session_history_collection.InsertOneAsync(session, ush);
            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }

    public async Task<List<UserSessionHistory>> GetAllUserSessionHistoryAsync(IClientSessionHandle session)
    {
        var list_ush = await _user_session_history_collection.Find(session, ush => true).ToListAsync();
        return list_ush;
    }

    public async Task<UserSessionHistory> GetUserSessionHistoryByUserIdAsync(string user_id, IClientSessionHandle session)
    {
        var ush = await _user_session_history_collection.Find(session, ush => ush.UserId == user_id).FirstOrDefaultAsync();
        return ush;
    } 

    public async Task<bool> AddNewSessionAsync(string user_id, UserSession user_session, IClientSessionHandle session)
    {
        try
        {
            var ush = await GetUserSessionHistoryByUserIdAsync(user_id, session);
            if(ush == null)
            {
                return false;
            }
            ush.UserSessions.Add(user_session);
            ush.CurrentSession = user_session;

            var update_data = Builders<UserSessionHistory>.Update
            .Set(u => u.UserSessions, ush.UserSessions)
            .Set(u => u.CurrentSession, ush.CurrentSession);

            await _user_session_history_collection.UpdateOneAsync(session, ush => ush.UserId == user_id, update_data);
            return true;
        }
        catch(Exception)
        {
            return false;
        }
    }
    
}