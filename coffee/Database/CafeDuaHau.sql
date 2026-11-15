-- SQL Server: tạo CafeDuaHau nếu chưa có, cập nhật schema và tạo dữ liệu thực đơn mẫu.
-- Tài khoản admin được tạo bằng: dotnet run --no-launch-profile -- --setup-admin
USE [master];
GO
IF DB_ID(N'CafeDuaHau') IS NULL
    CREATE DATABASE [CafeDuaHau];
GO
USE [CafeDuaHau];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062717_InitialOrders'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] nvarchar(32) NOT NULL,
        [CustomerKey] nvarchar(64) NOT NULL,
        [CustomerName] nvarchar(max) NOT NULL,
        [CustomerPhone] nvarchar(max) NOT NULL,
        [DeliveryAddress] nvarchar(max) NOT NULL,
        [TotalAmount] int NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [PaymentMethod] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062717_InitialOrders'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [OrderId] nvarchar(32) NOT NULL,
        [Id] int NOT NULL IDENTITY,
        [ItemId] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Price] int NOT NULL,
        [Quantity] int NOT NULL,
        [Note] nvarchar(max) NOT NULL,
        [Size] nvarchar(max) NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderId], [Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062717_InitialOrders'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerKey_CreatedAt] ON [Orders] ([CustomerKey], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062717_InitialOrders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115062717_InitialOrders', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062753_MenuAndAdmin'
)
BEGIN
    CREATE TABLE [AdminAccounts] (
        [Id] int NOT NULL IDENTITY,
        [UserName] nvarchar(64) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AdminAccounts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062753_MenuAndAdmin'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] nvarchar(32) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CategoryId] nvarchar(max) NOT NULL,
        [Price] int NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [IsSoldOut] bit NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062753_MenuAndAdmin'
)
BEGIN
    CREATE TABLE [MenuSizes] (
        [MenuItemId] nvarchar(32) NOT NULL,
        [Id] int NOT NULL IDENTITY,
        [Label] nvarchar(max) NOT NULL,
        [Price] int NOT NULL,
        CONSTRAINT [PK_MenuSizes] PRIMARY KEY ([MenuItemId], [Id]),
        CONSTRAINT [FK_MenuSizes_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062753_MenuAndAdmin'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'Description', N'ImageUrl', N'IsSoldOut', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[MenuItems]'))
        SET IDENTITY_INSERT [MenuItems] ON;
    EXEC(N'INSERT INTO [MenuItems] ([Id], [CategoryId], [Description], [ImageUrl], [IsSoldOut], [Name], [Price])
    VALUES (N''bac-xiu'', N''ca-phe'', N''Nhiều sữa, ít cà phê, vị nhẹ dễ uống.'', N''/images/menu/bac-xiu.jpg'', CAST(0 AS bit), N''Bạc xỉu'', 0),
    (N''banh-croissant'', N''banh'', N''Bánh sừng bò bơ giòn xốp.'', N''/images/menu/banh-croissant.jpg'', CAST(1 AS bit), N''Bánh croissant'', 35000),
    (N''ca-phe-den'', N''ca-phe'', N''Cà phê phin truyền thống, đậm đà.'', N''/images/menu/ca-phe-den.jpg'', CAST(0 AS bit), N''Cà phê đen'', 0),
    (N''ca-phe-sua'', N''ca-phe'', N''Cà phê phin pha sữa đặc béo ngọt.'', N''/images/menu/ca-phe-sua.jpg'', CAST(0 AS bit), N''Cà phê sữa'', 0),
    (N''cappuccino'', N''ca-phe'', N''Espresso với lớp bọt sữa mịn.'', N''/images/menu/cappuccino.jpg'', CAST(0 AS bit), N''Cappuccino'', 45000),
    (N''nuoc-ep-cam'', N''sinh-to'', N''Cam tươi vắt nguyên chất.'', N''/images/menu/nuoc-ep-cam.jpg'', CAST(0 AS bit), N''Nước ép cam'', 40000),
    (N''sinh-to-bo'', N''sinh-to'', N''Bơ sáp xay cùng sữa, béo mịn.'', N''/images/menu/sinh-to-bo.webp'', CAST(0 AS bit), N''Sinh tố bơ'', 45000),
    (N''sinh-to-dua-hau'', N''sinh-to'', N''Dưa hấu xay mịn, mát lạnh giải nhiệt.'', N''/images/menu/sinh-to-dua-hau.jpg'', CAST(0 AS bit), N''Sinh tố dưa hấu'', 0),
    (N''tiramisu'', N''banh'', N''Bánh tiramisu vị cà phê và cacao.'', N''/images/menu/tiramisu.jpg'', CAST(0 AS bit), N''Tiramisu'', 45000),
    (N''tra-dao-cam-sa'', N''tra'', N''Trà đào thanh mát, thêm cam và sả thơm.'', N''/images/menu/tra-dao-cam-sa.jpg'', CAST(0 AS bit), N''Trà đào cam sả'', 0),
    (N''tra-sua-tran-chau'', N''tra'', N''Trà sữa truyền thống với trân châu dai giòn.'', N''/images/menu/tra-sua-tran-chau.jpg'', CAST(0 AS bit), N''Trà sữa trân châu'', 0),
    (N''tra-vai'', N''tra'', N''Trà xanh kết hợp vải thiều ngọt thanh.'', N''/images/menu/tra-vai.jpeg'', CAST(0 AS bit), N''Trà vải'', 38000)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryId', N'Description', N'ImageUrl', N'IsSoldOut', N'Name', N'Price') AND [object_id] = OBJECT_ID(N'[MenuItems]'))
        SET IDENTITY_INSERT [MenuItems] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062753_MenuAndAdmin'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'MenuItemId', N'Label', N'Price') AND [object_id] = OBJECT_ID(N'[MenuSizes]'))
        SET IDENTITY_INSERT [MenuSizes] ON;
    EXEC(N'INSERT INTO [MenuSizes] ([Id], [MenuItemId], [Label], [Price])
    VALUES (1, N''bac-xiu'', N''M'', 29000),
    (2, N''bac-xiu'', N''L'', 35000),
    (1, N''ca-phe-den'', N''M'', 25000),
    (2, N''ca-phe-den'', N''L'', 30000),
    (1, N''ca-phe-sua'', N''M'', 29000),
    (2, N''ca-phe-sua'', N''L'', 35000),
    (1, N''sinh-to-dua-hau'', N''M'', 35000),
    (2, N''sinh-to-dua-hau'', N''L'', 42000),
    (1, N''tra-dao-cam-sa'', N''M'', 39000),
    (2, N''tra-dao-cam-sa'', N''L'', 45000),
    (1, N''tra-sua-tran-chau'', N''M'', 35000),
    (2, N''tra-sua-tran-chau'', N''L'', 42000)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'MenuItemId', N'Label', N'Price') AND [object_id] = OBJECT_ID(N'[MenuSizes]'))
        SET IDENTITY_INSERT [MenuSizes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115062753_MenuAndAdmin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115062753_MenuAndAdmin', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    DROP INDEX [IX_Orders_CustomerKey_CreatedAt] ON [Orders];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    ALTER TABLE [Orders] ADD [CustomerAccountId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    CREATE TABLE [CustomerAccounts] (
        [Id] uniqueidentifier NOT NULL,
        [UserName] nvarchar(64) NOT NULL,
        [NormalizedUserName] nvarchar(64) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CustomerAccounts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerAccountId_CreatedAt] ON [Orders] ([CustomerAccountId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomerAccounts_NormalizedUserName] ON [CustomerAccounts] ([NormalizedUserName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_CustomerAccounts_CustomerAccountId] FOREIGN KEY ([CustomerAccountId]) REFERENCES [CustomerAccounts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115064730_CustomerAccountsAndOrderOwnership'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115064730_CustomerAccountsAndOrderOwnership', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115071101_MenuPhotosAndReviews'
)
BEGIN
    CREATE TABLE [MenuPhotos] (
        [Id] uniqueidentifier NOT NULL,
        [MenuItemId] nvarchar(32) NOT NULL,
        [Position] int NOT NULL,
        [ContentType] nvarchar(20) NOT NULL,
        [Content] varbinary(max) NOT NULL,
        CONSTRAINT [PK_MenuPhotos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuPhotos_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115071101_MenuPhotosAndReviews'
)
BEGIN
    CREATE TABLE [MenuReviews] (
        [MenuItemId] nvarchar(32) NOT NULL,
        [CustomerAccountId] uniqueidentifier NOT NULL,
        [Rating] int NOT NULL,
        [Comment] nvarchar(1000) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_MenuReviews] PRIMARY KEY ([MenuItemId], [CustomerAccountId]),
        CONSTRAINT [CK_MenuReviews_Rating] CHECK ([Rating] BETWEEN 1 AND 5),
        CONSTRAINT [FK_MenuReviews_CustomerAccounts_CustomerAccountId] FOREIGN KEY ([CustomerAccountId]) REFERENCES [CustomerAccounts] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MenuReviews_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115071101_MenuPhotosAndReviews'
)
BEGIN
    CREATE INDEX [IX_MenuPhotos_MenuItemId_Position] ON [MenuPhotos] ([MenuItemId], [Position]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115071101_MenuPhotosAndReviews'
)
BEGIN
    CREATE INDEX [IX_MenuReviews_CustomerAccountId] ON [MenuReviews] ([CustomerAccountId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115071101_MenuPhotosAndReviews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115071101_MenuPhotosAndReviews', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072413_RealtimeChat'
)
BEGIN
    CREATE TABLE [ChatMessages] (
        [Id] bigint NOT NULL IDENTITY,
        [CustomerAccountId] uniqueidentifier NOT NULL,
        [FromAdmin] bit NOT NULL,
        [Text] nvarchar(1000) NOT NULL,
        [SentAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChatMessages_CustomerAccounts_CustomerAccountId] FOREIGN KEY ([CustomerAccountId]) REFERENCES [CustomerAccounts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072413_RealtimeChat'
)
BEGIN
    CREATE INDEX [IX_ChatMessages_CustomerAccountId_Id] ON [ChatMessages] ([CustomerAccountId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072413_RealtimeChat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115072413_RealtimeChat', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115081437_CustomerPhoneAndSocialLogin'
)
BEGIN
    ALTER TABLE [CustomerAccounts] ADD [ExternalProvider] nvarchar(16) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115081437_CustomerPhoneAndSocialLogin'
)
BEGIN
    ALTER TABLE [CustomerAccounts] ADD [ExternalSubject] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115081437_CustomerPhoneAndSocialLogin'
)
BEGIN
    ALTER TABLE [CustomerAccounts] ADD [PhoneNumber] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115081437_CustomerPhoneAndSocialLogin'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CustomerAccounts_ExternalProvider_ExternalSubject] ON [CustomerAccounts] ([ExternalProvider], [ExternalSubject]) WHERE [ExternalProvider] IS NOT NULL AND [ExternalSubject] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115081437_CustomerPhoneAndSocialLogin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115081437_CustomerPhoneAndSocialLogin', N'10.0.15');
END;

COMMIT;
GO
