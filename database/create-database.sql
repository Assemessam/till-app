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
