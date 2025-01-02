using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

[Authorize] 
[Route("api/user")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IMongoClient _mongo_client;
    private readonly UserService _userService;
    private readonly DeletedUserService _delete_user_service;
    private readonly UserConversationService _user_conversation_service;
    private readonly UserFriendRequestService _user_friend_request_service;
    private readonly UserSessionService _user_session_service;
    public UserController(IMongoClient client,
    UserService userService,
    DeletedUserService deletedUserService,
    UserConversationService user_conversation_service,
    UserFriendRequestService user_friend_request_service,
    UserSessionService user_session_service)
    {
        _mongo_client = client;
        _userService = userService;
        _delete_user_service = deletedUserService;
        _user_conversation_service = user_conversation_service;
        _user_friend_request_service = user_friend_request_service;
        _user_session_service = user_session_service;
    }

    // CREATE - POST: /api/user
    [HttpPost("create")]
    public async Task<ActionResult<User>> CreateUser([FromForm] User user)
    {
        Console.WriteLine("Create User has been called");
        using (var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
                
                // Kiểm tra người dùng đã tồn tại chưa
                User user_result = await _userService.GetUserByIdAsync(user.PublicUserId, session);
                if (user_result != null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Không thể tạo mới user, user_id đã tồn tại");
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
    [HttpGet("all")]
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

    [HttpGet]
    public async Task<ActionResult<User>> GetUserData()
    {
        using(var session = await _mongo_client.StartSessionAsync())
        try
        {
            session.StartTransaction();
            var user_id = User.FindFirst("UserId")?.Value; 

                if (string.IsNullOrEmpty(user_id))
                {
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }

            var user_data = await _userService.GetUserDataByUserIdAsync(user_id,session);
            if(user_data == null)
            {
                return BadRequest("Không thể lấy thông tin người dùng");
            }
            return Ok(user_data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Đã xảy ra lỗi: {ex.Message}");
        }
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
    // [HttpDelete("{id}")]
    // public async Task<ActionResult<string>> DeleteUser(string id)
    // {
    //     using (var session = await _mongo_client.StartSessionAsync())
    //     {
    //         session.StartTransaction();
    //         try
    //         {
    //             var success = await _userService.DeleteUserAsync(id, session);
    //             if (!success)
    //             {
    //                 await session.AbortTransactionAsync();
    //                 return BadRequest("Xóa người dùng thất bại!");
    //             }
    //             await session.CommitTransactionAsync();
    //             return Ok("Xóa người dùng thành công!");
    //         }
    //         catch(Exception e)
    //         {
    //             await session.AbortTransactionAsync();
    //             return BadRequest($"Đã xảy ra lỗi khi thực hiện giao dịch, error: {e}");
    //         }
    //     }
        
    // }

    [HttpDelete("delete")]
    public async Task<ActionResult<string>> DeleteUser()
    {
        using (var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user_id = User.FindFirst("UserId")?.Value; 

                if (string.IsNullOrEmpty(user_id))
                {
                    return Unauthorized("Không tìm thấy thông tin người dùng trong token.");
                }
                var  user  = await _userService.SetDeletedAccountAsync(user_id, session);
                if (user == null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Xóa người dùng thất bại!");
                }
                var res = await _delete_user_service.AddDeletedUserAsync(user, session);
                if (!res)
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


    [HttpPut("change_user_name")]
    public async Task<IActionResult> ChangUsername([FromForm] string first_name, [FromForm] string last_name)
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var user_id = User.FindFirst("UserId")?.Value;
                if (user_id == null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest();
                }
                var res = await _userService.ChangeUsernameAsync(user_id, first_name, last_name, session);
                if (!res)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest();
                }
                await session.CommitTransactionAsync();
                return Ok();
            }
            catch(Exception)
            {
                await session.AbortTransactionAsync();
                return StatusCode(502 , "Bad Gateway");
            }
        }


    }

    [HttpPut("change_avatar")]
    public async Task<IActionResult> ChangeAvatar()
    {
        using(var session = await _mongo_client.StartSessionAsync())
        {
            try
            {
                session.StartTransaction();
                var user_id = User.FindFirst("UserId")?.Value;
                if (user_id == null)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest();
                }
                // Đọc chuỗi Base64 từ Request body
                using var reader = new StreamReader(Request.Body);
                string base64String = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(base64String))
                {
                    await session.AbortTransactionAsync();
                    return BadRequest("Base64 string is empty.");
                }

                var res = await _userService.ChangeAvatarAsync(user_id, base64String, session);
                if (!res)
                {
                    await session.AbortTransactionAsync();
                    return BadRequest();
                }
                await session.CommitTransactionAsync();
                return Ok(new { Message = "Avatar updated successfully"});
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                return StatusCode(500, new { Message = "Error processing avatar", Error = ex.Message });
            }
        }
        
    }
}
