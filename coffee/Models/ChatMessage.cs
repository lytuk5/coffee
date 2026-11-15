namespace CafeDuaHau.Models;

public class ChatMessage
{
    public long Id { get; set; }
    public Guid CustomerAccountId { get; set; }
    public bool FromAdmin { get; set; }
    public string Text { get; set; } = "";
    public DateTimeOffset SentAt { get; set; }
}
