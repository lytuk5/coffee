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


    public void Add(Order order)
    {
        database.Orders.Add(order);
        database.SaveChanges();
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
