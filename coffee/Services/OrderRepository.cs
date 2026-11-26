using CafeDuaHau.Data;
using CafeDuaHau.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeDuaHau.Services;

public class OrderRepository(CafeDbContext database)
{
    public List<Order> GetByCustomer(Guid customerId) =>
        database.Orders.AsNoTracking()
            .Where(order => order.CustomerAccountId == customerId)
            .OrderByDescending(order => order.CreatedAt)
            .ToList();

    public List<Order> GetAll(string? keyword)
    {
        var orders = database.Orders.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            var orderCode = term.TrimStart('#');
            orders = orders.Where(order =>
                (orderCode.Length > 0 && order.Id.Contains(orderCode)) ||
                order.CustomerName.Contains(term) ||
                order.CustomerPhone.Contains(term) ||
                (order.CustomerAccountId.HasValue && database.CustomerAccounts.Any(customer =>
                    customer.Id == order.CustomerAccountId && customer.UserName.Contains(term))));
        }

        return orders.OrderByDescending(order => order.CreatedAt).ToList();
    }


    public string? Add(Order order)
    {
        using var transaction = database.Database.BeginTransaction();
        foreach (var group in order.Items.GroupBy(item => item.ItemId).OrderBy(group => group.Key))
        {
            var quantity = group.Sum(item => item.Quantity);
            var updated = database.MenuItems
                .Where(item => item.Id == group.Key && !item.IsSoldOut &&
                    (item.StockQuantity == null || item.StockQuantity >= quantity))
                .ExecuteUpdate(update => update
                    .SetProperty(item => item.StockQuantity, item => item.StockQuantity - quantity)
                    .SetProperty(item => item.IsSoldOut, item => item.StockQuantity == quantity));
            if (updated == 0)
                return group.First().Name;

            var stockDeducted = database.MenuItems.Any(item => item.Id == group.Key && item.StockQuantity != null);
            foreach (var line in group)
                line.StockDeducted = stockDeducted;
        }

        database.Orders.Add(order);
        database.SaveChanges();
        transaction.Commit();
        return null;
    }

    public bool Cancel(string orderId, Guid customerId) =>
        UpdatePendingOrder(orderId, OrderStatus.Cancelled, customerId);

    public bool SetStatus(string orderId, OrderStatus status) =>
        status is OrderStatus.Cancelled or OrderStatus.Delivered && UpdatePendingOrder(orderId, status);

    private bool UpdatePendingOrder(string orderId, OrderStatus status, Guid? customerId = null)
    {
        using var transaction = database.Database.BeginTransaction();
        var now = DateTime.Now;
        var updated = database.Orders
            .Where(order => order.Id == orderId && order.Status == OrderStatus.Pending &&
                (!customerId.HasValue || order.CustomerAccountId == customerId))
            .ExecuteUpdate(update => update
                .SetProperty(order => order.Status, status)
                .SetProperty(order => order.UpdatedAt, now));
        if (updated == 0)
            return false;

        if (status == OrderStatus.Cancelled)
        {
            var lines = database.Orders.Where(order => order.Id == orderId)
                .SelectMany(order => order.Items)
                .Where(line => line.StockDeducted)
                .Select(line => new { line.ItemId, line.Quantity })
                .ToList();

            foreach (var group in lines.GroupBy(line => line.ItemId).OrderBy(group => group.Key))
            {
                var quantity = group.Sum(line => line.Quantity);
                database.MenuItems.Where(item => item.Id == group.Key && item.StockQuantity != null)
                    .ExecuteUpdate(update => update
                        .SetProperty(item => item.StockQuantity, item => item.StockQuantity + quantity)
                        .SetProperty(item => item.IsSoldOut, false));
            }
        }

        transaction.Commit();
        return true;
    }
}
