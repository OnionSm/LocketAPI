using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class ChatHub: Hub
{
    public async Task SendMessage(string user, string message)
    {
        // Kiểm tra điều kiện (ví dụ: chỉ gửi thông điệp nếu message không trống)
        if (!string.IsNullOrEmpty(message))
        {
            Console.WriteLine("Message from {0}: {1}", user, message);
            // Gửi thông điệp đến tất cả client
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        else
        {
            // Nếu không có thông điệp, gửi phản hồi riêng cho client gọi
            await Clients.Caller.SendAsync("ReceiveMessage", "Server", "Your message is empty.");
        }
    }
}