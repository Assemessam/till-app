/*
    Standalone TillApp schema creation script for SQL Server.
    This script contains no credentials and is independent of EF Core migrations.
*/

IF DB_ID(N'TillApp') IS NULL
BEGIN
    CREATE DATABASE [TillApp];
END;
GO

USE [TillApp];
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Categories]
    (
        [CategoryID] int IDENTITY(1, 1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryID]),
        CONSTRAINT [UX_Categories_Name] UNIQUE ([Name])
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Products]
    (
        [ProductID] int IDENTITY(1, 1) NOT NULL,
        [CategoryID] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [UnitPrice] money NOT NULL,
        [IsActive] bit NOT NULL
            CONSTRAINT [DF_Products_IsActive] DEFAULT (CONVERT(bit, 1)),
        CONSTRAINT [PK_Products] PRIMARY KEY ([ProductID]),
        CONSTRAINT [CK_Products_UnitPrice]
            CHECK ([UnitPrice] >= 0.0001 AND [UnitPrice] <= 922337203685477.5807),
        CONSTRAINT [UX_Products_CategoryID_Name] UNIQUE ([CategoryID], [Name]),
        CONSTRAINT [FK_Products_Categories_CategoryID]
            FOREIGN KEY ([CategoryID]) REFERENCES [dbo].[Categories] ([CategoryID])
    );

    CREATE INDEX [IX_Products_CategoryID_IsActive]
        ON [dbo].[Products] ([CategoryID], [IsActive]);
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Orders]
    (
        [OrderID] int IDENTITY(1, 1) NOT NULL,
        [OrderName] nvarchar(100) NOT NULL,
        [Amount] money NOT NULL,
        [IsPaid] bit NOT NULL
            CONSTRAINT [DF_Orders_IsPaid] DEFAULT (CONVERT(bit, 0)),
        CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderID]),
        CONSTRAINT [CK_Orders_Amount]
            CHECK ([Amount] >= 0 AND [Amount] <= 922337203685477.5807)
    );

    CREATE INDEX [IX_Orders_IsPaid] ON [dbo].[Orders] ([IsPaid]);
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[OrderItems]
    (
        [OrderItemID] int IDENTITY(1, 1) NOT NULL,
        [OrderID] int NOT NULL,
        [ItemName] nvarchar(100) NOT NULL,
        [Price] money NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderItemID]),
        CONSTRAINT [CK_OrderItems_Price]
            CHECK ([Price] >= 0.0001 AND [Price] <= 922337203685477.5807),
        CONSTRAINT [FK_OrderItems_Orders_OrderID]
            FOREIGN KEY ([OrderID]) REFERENCES [dbo].[Orders] ([OrderID]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_OrderItems_OrderID] ON [dbo].[OrderItems] ([OrderID]);
END;
GO
