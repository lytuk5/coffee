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
        }

        database.Orders.Add(order);
        database.SaveChanges();
        transaction.Commit();
        return null;
    }

    public bool Cancel(string orderId, Guid customerId)
    {
        var now = DateTime.Now;
        return database.Orders
            .Where(order => order.Id == orderId &&
                order.CustomerAccountId == customerId &&
                order.Status == OrderStatus.Pending)
            .ExecuteUpdate(update => update
                .SetProperty(order => order.Status, OrderStatus.Cancelled)
                .SetProperty(order => order.UpdatedAt, now)) == 1;
    }
    public bool SetStatus(string orderId, OrderStatus status)
    {
        if (status is not (OrderStatus.Cancelled or OrderStatus.Delivered))
            return false;

        var now = DateTime.Now;
        return database.Orders
            .Where(order => order.Id == orderId && order.Status == OrderStatus.Pending)
            .ExecuteUpdate(update => update
                .SetProperty(order => order.Status, status)
                .SetProperty(order => order.UpdatedAt, now)) == 1;
    }
}
