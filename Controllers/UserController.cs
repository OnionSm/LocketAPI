using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/user")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly UserService _userService;
    private readonly UserConversationService _user_conversation_service;
    private readonly UserFriendRequestService _user_friend_request_service;
    private readonly UserSessionService _user_session_service;
    public UserController(IMongoClient client,
    UserService userService,
    UserConversationService user_conversation_service,
    UserFriendRequestService user_friend_request_service,
    UserSessionService user_session_service)
    {
        _mongo_client = client;
        _userService = userService;
        _user_conversation_service = user_conversation_service;
        _user_friend_request_service = user_friend_request_service;
        _user_session_service = user_session_service;
    }

    // CREATE - POST: /api/user
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
                
                // Kiểm tra người dùng đã tồn tại chưa
                User user_result = await _userService.GetUserByIdAsync(user.Id, session);
                if (user_result != null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể tạo mới user, user đã tồn tại");
                }
                
                // Tạo người dùng mới
                await _userService.CreateUserAsync(user, session);

                bool result_phone_number = false;
                bool result_email = false;

                // Kiểm tra số điện thoại
                if (user.PhoneNumber != null)
                {
                    var user_temp = await _userService.GetUserByPhoneNumberAsync(user.PhoneNumber, session);
                    if (user_temp == null)
                    {
                        result_phone_number = true;
                    }
                }

                // Kiểm tra email
                if (user.Email != null)
                {
                    var user_temp = await _userService.GetUserByPhoneNumberAsync(user.Email, session);
                    if (user_temp == null)
                    {
                        result_email = true;
                    }
                }

                // Nếu số điện thoại và email đều không hợp lệ
                if (!result_phone_number && !result_email)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Số điện thoại và email đã được sử dụng");
                }

                // Create User Session History 
                var check_ss_existence = await _user_session_service.GetUserSessionHistoryByUserIdAsync(user.Id, session);
                if(check_ss_existence != null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thêm lịch sử phiên đăng nhập do lịch sử này đã được tạo");
                }
                UserSessionHistory ush = new UserSessionHistory();
                ush.UserId = user.Id;
                var create_result = await _user_session_service.CreateNewUserSessionHistoryAsync(ush, session);
                if(!create_result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể thêm lịch sử phiên đăng nhập");
                }
                
                // Tạo UserConversation
                UserConversation user_conversation = new UserConversation
                {
                    UserId = user.Id
                };
                bool result = await _user_conversation_service.CreateUserConversationAsync(user_conversation, session);
                if (!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể tạo mới hội thoại user");
                }

                // Tạo UserFriendRequest
                UserFriendRequest ufr = new UserFriendRequest
                {
                    UserId = user.Id
                };
                await _user_friend_request_service.CreateNewUserFriendRequestAsync(ufr, session);

                var result2 = await _user_friend_request_service.GetUserFriendRequestByUserIdAsync(user.Id, session);
                if (result2 == null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể tạo mới user friend request");
                }

                // Commit transaction
                await session.CommitTransactionAsync();
                return Ok(user);
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch {ex}");
            }
        }
    }



    [HttpPost("phone/{phone_number}")]
    public async Task<ActionResult<bool>> CheckValidPhoneNumber(string phone_number)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                if (string.IsNullOrWhiteSpace(phone_number))
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Số điện thoại không hợp lệ");
                }
                var result = await _userService.GetUserByPhoneNumberAsync(phone_number, session);
                if (result != null)
                {
                    await session.AbortTransactionAsync();
                    return Ok(false);  
                }
                await session.CommitTransactionAsync();
                return Ok(true);  
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
    }

    [HttpPost("email")]
    public async Task<ActionResult<bool>> CheckValidEmail([FromForm] string email)
    {
        Console.WriteLine("Has been called");
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Email không hợp lệ");
                }
                var result = await _userService.GetUserByEmailAsync(email, session);
                if (result == null)
                {
                    await session.AbortTransactionAsync();
                    return Ok(false);  // Email không tồn tại
                }
                await session.CommitTransactionAsync();
                return Ok(true); 
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error {e}");
            }
        }
    }



    // READ - GET: /api/user
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var users = await _userService.GetAllUsersAsync(session);
                await session.CommitTransactionAsync();
                return Ok(users);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Có lỗi trong quá trình xử lí, error: {e}");
            }
        }
    }

    // READ - GET: /api/user/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(string id)
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user = await _userService.GetUserByIdAsync(id, session);
                if (user == null)
                {
                    await session.CommitTransactionAsync();
                    return NotFound();
                }
                await session.CommitTransactionAsync();
                return Ok(user);
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Có lỗi trong quá trình xử lí, error: {e}");
            }
        }
    }

    [HttpGet("id/{user_id}")]
    public async Task<ActionResult<User>>  GetUserData(string user_id, string token)
    {
        var user_data = await _userService.GetUserDataByTokenAsync(user_id, token);
        if(user_data == null)
        {
            return BadRequest("Không thể lấy thông tin người dùng");
        }
        return Ok(user_data);
    }

    // UPDATE - PUT: /api/user/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<string>> UpdateUser(string id, [FromBody] User updatedUser)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var success = await _userService.UpdateUserAsync(id, updatedUser, session);
                if (!success)
                {
                    await session.AbortTransactionAsync();
                    return NotFound("Cập nhật người dùng thất bại!");
                }
                await session.CommitTransactionAsync();
                return Ok("Cập nhật thành công");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
    }

    [HttpPut("{user_id_1}-{user_id_2}")]
    public async Task<ActionResult<string>> AddNewFriend(string user_id_1, string user_id_2)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try 
            {
                bool result = await _userService.AddNewFriendAsync(user_id_1, user_id_2, session);
                if(!result)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Gửi lời mời kết bạn thất bại!");
                }
                await session.CommitTransactionAsync();
                return Ok("Gửi lời mời kết bạn thành công!");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
        
    }

    // DELETE - DELETE: /api/user/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult<string>> DeleteUser(string id)
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var success = await _userService.DeleteUserAsync(id, session);
                if (!success)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Xóa người dùng thất bại!");
                }
                await session.CommitTransactionAsync();
                return Ok("Xóa người dùng thành công!");
            }
            catch(Exception e)
            {
                await session.AbortTransactionAsync();
                return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
            }
        }
        
    }
}
