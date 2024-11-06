using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[Route("api/user")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly UserConversationService _user_conversation_service;

    public UserController(UserService userService, UserConversationService user_conversation_service)
    {
        _userService = userService;
        _user_conversation_service = user_conversation_service;
    }

    // CREATE - POST: /api/user
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        await _userService.CreateUserAsync(user);
        User user_result  = await _userService.GetUserByIdAsync(user.Id);
        if(user_result == null)
        {
            return BadRequest("Không thể tạo mới user");
        }
        UserConversation user_conversation = new UserConversation();
        user_conversation.UserId = user_result.Id;
        bool result = await _user_conversation_service.CreateUserConversationAsync(user_conversation);
        if(!result)
        {
            return BadRequest("Không thể tạo mới hội thoại user");
        }
        return Ok(user_result);
    }

    [HttpPost("phone")]
    public async Task<ActionResult<string>> CheckValidPhoneNumber([FromForm] string phone_number)
    {
        if (string.IsNullOrWhiteSpace(phone_number))
        {
            return BadRequest("Vui lòng nhập số điện thoại!");
        }

        var result = await _userService.GetUserByPhoneNumberAsync(phone_number);
        if (result != null)
        {
            return BadRequest("Số điện thoại này đã có người dùng!");
        }
        return Ok("Nhập số điện thoại thành công");
    }

    [HttpPost("email")]
    public async Task<ActionResult<string>> CheckValidEmail([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Vui lòng nhập email!");
        }

        var result = await _userService.GetUserByEmailAsync(email);
        if (result != null)
        {
            return BadRequest("Email này đã được sử dụng!");
        }
        return Ok("Nhập email thành công");
    }


    // READ - GET: /api/user
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // READ - GET: /api/user/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    // UPDATE - PUT: /api/user/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] User updatedUser)
    {
        var success = await _userService.UpdateUserAsync(id, updatedUser);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("{user_id_1}-{user_id_2}")]
    public async Task<IActionResult> AddNewFriend(string user_id_1, string user_id_2)
    {
        bool result = await _userService.AddNewFriendAsync(user_id_1, user_id_2);
        if(!result)
        {
            return NotFound();
        }
        return NoContent();
    }

    // DELETE - DELETE: /api/user/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
        {
            return NotFound();
        }
        return NoContent();
    }
}
