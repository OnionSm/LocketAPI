using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;


[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly FeedbackService _feedbackService;
    private readonly IMongoClient _mongoClient;

    public FeedbackController(FeedbackService feedbackService, IMongoClient mongoClient)
    {
        _feedbackService = feedbackService;
        _mongoClient = mongoClient;
    }

    [HttpPost]
    public async Task<IActionResult> FeedbackIncident([FromForm] Feedback feedbackRequest)
    {
        // Kiểm tra report có trống hay không
        if (string.IsNullOrEmpty(feedbackRequest.UserEmail) || string.IsNullOrEmpty(feedbackRequest.Description))
        {
            return BadRequest("UserEmail và Description không được để trống.");
        }
        
        try
        {
            await _feedbackService.CreateFeedbackAsync(feedbackRequest);
            return Ok(feedbackRequest);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}