using MongoDB.Driver.Authentication;
using MongoDB.Driver;

public class FeedbackService
{
    private readonly IMongoCollection<Feedback> _feedback_collection;

    public FeedbackService(IMongoDatabase database)
    {
        _feedback_collection = database.GetCollection<Feedback>("Feedback");
    }

    public async Task CreateFeedbackAsync(Feedback feedback)
    {   
        await _feedback_collection.InsertOneAsync(feedback);
    }
}