public enum MessageStatus
{
    Sent,       // Tin nhắn đã được gửi
    Delivered,  // Tin nhắn đã được nhận bởi máy chủ hoặc thiết bị người nhận
    Read,       // Tin nhắn đã được người nhận đọc
    Failed,     // Tin nhắn không gửi được
    Deleted,    // Tin nhắn đã bị xóa
    Edited      // Tin nhắn đã được chỉnh sửa
}