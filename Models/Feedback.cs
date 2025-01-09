public class Feedback
{
    public string UserEmail { get; set; } = "";
    public string Description { get; set; } = "";
    public FeedbackType? TypeFeedback { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
}