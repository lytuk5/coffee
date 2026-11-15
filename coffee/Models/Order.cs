namespace CafeDuaHau.Models;

public enum OrderStatus
{
    Pending,    // Đã đặt
    Cancelled,  // Đã hủy
    Delivered   // Đã giao hàng
}

public static class OrderStatusDisplay
{
    public static string Label(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Đã đặt",
        OrderStatus.Cancelled => "Đã hủy",
        OrderStatus.Delivered => "Đã giao hàng",
        _ => "Không xác định"
    };
}

public class OrderItem
{
    public string ItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Price { get; set; }
    public int Quantity { get; set; }
    public string Note { get; set; } = "";
    public string? Size { get; set; }
}

public class Order
{
    public string Id { get; set; } = "";

    /// <summary>Mã trình duyệt đối với các đơn được đặt trước khi có tài khoản khách.</summary>
    public string CustomerKey { get; set; } = "";

    public Guid? CustomerAccountId { get; set; }

    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string DeliveryAddress { get; set; } = "";
    public List<OrderItem> Items { get; set; } = new();
    public int TotalAmount { get; set; }
    public string Note { get; set; } = "";
    public string PaymentMethod { get; set; } = "cash";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
