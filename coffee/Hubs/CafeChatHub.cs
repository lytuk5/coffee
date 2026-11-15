using CafeDuaHau.Data;
using CafeDuaHau.Models;
using CafeDuaHau.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Hubs;

[Authorize(Roles = "Admin,Customer")]
public class CafeChatHub(CafeDbContext database) : Hub
{
    private const string AdminGroup = "chat-admins";
    private bool IsAdmin => Context.User!.IsInRole("Admin");

    public override async Task OnConnectedAsync()
    {
        var group = IsAdmin ? AdminGroup : CustomerGroup(CustomerIdentity.GetId(Context.User!));
        await Groups.AddToGroupAsync(Context.ConnectionId, group, Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public async Task<List<ChatConversation>> GetConversations()
    {
        if (!IsAdmin)
            throw new HubException("Chỉ chủ quán được xem danh sách khách.");

        var latest = database.ChatMessages.GroupBy(message => message.CustomerAccountId)
            .Select(conversation => new
            {
                CustomerId = conversation.Key,
                LastMessageId = conversation.Max(message => message.Id)
            });

        return await (
            from conversation in latest
            join customer in database.CustomerAccounts on conversation.CustomerId equals customer.Id
            orderby conversation.LastMessageId descending
            select new ChatConversation(customer.Id, customer.UserName))
            .ToListAsync(Context.ConnectionAborted);
    }

    public async Task<List<ChatMessage>> GetMessages(Guid? customerId)
    {
        var conversation = GetConversationId(customerId);
        var recent = await database.ChatMessages.AsNoTracking()
            .Where(message => message.CustomerAccountId == conversation)
            .OrderByDescending(message => message.Id)
            .Take(100)
            .ToListAsync(Context.ConnectionAborted);
        recent.Reverse();
        return recent;
    }

    public async Task<ChatMessage> SendMessage(Guid? customerId, string? text)
    {
        var conversation = GetConversationId(customerId);
        text = text?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > 1000)
            throw new HubException("Tin nhắn phải có từ 1 đến 1.000 ký tự.");

        if (IsAdmin && !await database.CustomerAccounts.AnyAsync(customer => customer.Id == conversation, Context.ConnectionAborted))
            throw new HubException("Khách hàng này không còn tồn tại.");

        var message = new ChatMessage
        {
            CustomerAccountId = conversation,
            FromAdmin = IsAdmin,
            Text = text,
            SentAt = DateTimeOffset.UtcNow
        };

        database.ChatMessages.Add(message);
        await database.SaveChangesAsync(Context.ConnectionAborted);
        await Clients.Groups(AdminGroup, CustomerGroup(conversation)).SendAsync("MessageReceived", message);
        return message;
    }

    [Authorize(Roles = "Admin")]
    public async Task<bool> DeleteCustomerMessage(Guid customerId, long messageId)
    {
        var deleted = await database.ChatMessages
            .Where(message => message.Id == messageId &&
                message.CustomerAccountId == customerId && !message.FromAdmin)
            .ExecuteDeleteAsync(Context.ConnectionAborted);

        if (deleted == 0)
            return false;

        await Clients.Groups(AdminGroup, CustomerGroup(customerId))
            .SendAsync("MessageDeleted", customerId, messageId);
        return true;
    }

    private Guid GetConversationId(Guid? customerId)
    {
        if (IsAdmin)
            return customerId ?? throw new HubException("Hãy chọn khách để trò chuyện.");

        var ownId = CustomerIdentity.GetId(Context.User!);
        if (customerId.HasValue && customerId.Value != ownId)
            throw new HubException("Bạn chỉ có thể xem cuộc trò chuyện của mình.");

        return ownId;
    }

    private static string CustomerGroup(Guid customerId) => $"chat-customer:{customerId:N}";
}

public record ChatConversation(Guid CustomerId, string UserName);
