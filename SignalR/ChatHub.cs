using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class ChatHub: Hub
{
    public async Task SendMessage(Message message)
    {
        // Kiểm tra điều kiện (ví dụ: chỉ gửi thông điệp nếu message không trống)
        if (message != null)
        {
            Console.WriteLine("Message from {0}: {1}", message);
            // Gửi thông điệp đến tất cả client
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
        else
        {
            // Nếu không có thông điệp, gửi phản hồi riêng cho client gọi
            await Clients.Caller.SendAsync("ReceiveMessage", "Server", "Your message is empty.");
        }
    }
}