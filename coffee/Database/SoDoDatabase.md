# Sơ đồ cơ sở dữ liệu CafeDuaHau

```mermaid
erDiagram
    CustomerAccounts o|--o{ Orders : places
    CustomerAccounts ||--o{ ChatMessages : chats
    Orders ||--o{ OrderItems : contains
    MenuItems ||--o{ MenuSizes : offers
    MenuItems ||--o{ MenuPhotos : displays
    MenuItems ||--o{ MenuReviews : receives
    CustomerAccounts ||--o{ MenuReviews : writes

    AdminAccounts {
        int Id PK
        nvarchar UserName
        nvarchar PasswordHash
    }
    CustomerAccounts {
        uniqueidentifier Id PK
        nvarchar UserName
        nvarchar NormalizedUserName UK
        nvarchar PhoneNumber
        nvarchar ExternalProvider
        nvarchar ExternalSubject
        nvarchar PasswordHash
    }
    MenuItems {
        nvarchar Id PK
        nvarchar Name
        nvarchar Description
        nvarchar CategoryId
        nvarchar ImageUrl
        int Price
        bit IsSoldOut
        bit IsHot
        int DiscountPercent
    }
    MenuSizes {
        nvarchar MenuItemId PK, FK
        int Id PK
        nvarchar Label
        int Price
    }
    ChatMessages {
        bigint Id PK
        uniqueidentifier CustomerAccountId FK
        bit FromAdmin
        nvarchar Text
        datetimeoffset SentAt
    }
    MenuPhotos {
        uniqueidentifier Id PK
        nvarchar MenuItemId FK
        int Position
        nvarchar ContentType
        varbinary Content
    }
    MenuReviews {
        nvarchar MenuItemId PK, FK
        uniqueidentifier CustomerAccountId PK, FK
        int Rating
        nvarchar Comment
        datetime2 CreatedAt
    }
    Orders {
        nvarchar Id PK
        uniqueidentifier CustomerAccountId FK
        nvarchar CustomerKey
        nvarchar CustomerName
        nvarchar CustomerPhone
        nvarchar DeliveryAddress
        int TotalAmount
        nvarchar PaymentMethod
        int Status
        nvarchar Note
        datetime2 CreatedAt
        datetime2 UpdatedAt
    }
    OrderItems {
        nvarchar OrderId PK, FK
        int Id PK
        nvarchar ItemId
        nvarchar Name
        int Price
        int Quantity
        nvarchar Size
        nvarchar Note
    }
```

- Danh mục trên giao diện lấy từ `Data/MenuData.cs`. Món và giá theo kích cỡ lưu trong `MenuItems` và `MenuSizes`.
- `OrderItems` lưu tên, kích cỡ và giá tại thời điểm mua. `ItemId` là mã món lúc đặt, để lịch sử đơn vẫn hiển thị sau khi chủ quán xóa món.
- Đơn mới có khóa ngoại `CustomerAccountId`. Đơn đặt trước khi có đăng nhập giữ nguyên dữ liệu và mã trình duyệt cũ trong `CustomerKey`.
- `AdminAccounts` và `CustomerAccounts` lưu mật khẩu dưới dạng hash.
- Chạy `CafeDuaHau.sql` bằng SQL Server Management Studio hoặc `sqlcmd`. Ứng dụng cũng tự áp dụng migration khi chạy. Sau khi tạo database mới, dùng lệnh `dotnet run --no-launch-profile -- --setup-admin` trong thư mục dự án để thiết lập tài khoản chủ quán.

- Ảnh tải lên lưu trong MenuPhotos. Mỗi khách được đánh giá một lần cho mỗi món thuộc đơn đã giao.
- Trạng thái đơn lưu dưới dạng số trong SQL: 0 đã đặt, 1 đã hủy, 2 đã giao hàng.

- ChatMessages lưu tin nhắn giữa chủ quán và khách. Chỉ chủ quán và khách của cuộc trò chuyện được xem nội dung.

- Tài khoản khách mới có số điện thoại. Tài khoản Google hoặc Facebook được nhận diện bằng cặp ExternalProvider và ExternalSubject, có chỉ mục duy nhất.
