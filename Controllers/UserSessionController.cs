using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;


[Route("api/session")]
[ApiController]
public class UserSessionController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly UserSessionService _user_session_service;
    public UserSessionController(IMongoClient client, UserSessionService us_service)
    {
        _mongo_client = client;
        _user_session_service = us_service;
    }

    [HttpPost("ssh/create/{user_id}")]
    public async Task<IActionResult> CreateNewUserSessionHistory(string user_id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var check_existence = await _user_session_service.GetUserSessionHistoryByUserIdAsync(user_id, session);
                if(check_existence != null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thêm lịch sử phiên đăng nhập do lịch sử này đã được tạo");
                }
                UserSessionHistory ush = new UserSessionHistory();
                ush.UserId = user_id;
                var create_result = await _user_session_service.CreateNewUserSessionHistoryAsync(ush, session);
                if(!create_result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thêm lịch sử phiên đăng nhập");
                }
                await session.CommitTransactionAsync();
                return Created(string.Empty, "Tạo thành công lịch sử phiên đăng nhập");

            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Có lỗi xảy ra trong quá trình thêm lịch sử phiên đăng nhập, error {e}");
            }
        }
    }

    [HttpPut("update/user/{user_id}")]
    public async Task<IActionResult> AddNewUserSession(string user_id, [FromBody] UserSession u_session)
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var result = await _user_session_service.AddNewSessionAsync(user_id, u_session, session);
                if(!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thêm mới session");
                }
                await session.CommitTransactionAsync();
                return Ok("Thêm session thành công");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Có lỗi xảy ra trong quá trình thêm phiên đăng nhập, error {e}");   
            }
        }
    }
}