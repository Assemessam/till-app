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
    WHERE [MigrationId] = N'20260913185859_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [OrderID] int NOT NULL IDENTITY,
        [OrderName] nvarchar(100) NOT NULL,
        [Amount] money NOT NULL,
        [IsPaid] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderID]),
        CONSTRAINT [CK_Orders_Amount] CHECK ([Amount] >= 0 AND [Amount] <= 922337203685477.5807)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913185859_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [OrderItemID] int NOT NULL IDENTITY,
        [OrderID] int NOT NULL,
        [ItemName] nvarchar(100) NOT NULL,
        [Price] money NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderItemID]),
        CONSTRAINT [CK_OrderItems_Price] CHECK ([Price] >= 0.0001 AND [Price] <= 922337203685477.5807),
        CONSTRAINT [FK_OrderItems_Orders_OrderID] FOREIGN KEY ([OrderID]) REFERENCES [Orders] ([OrderID]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913185859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderID] ON [OrderItems] ([OrderID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913185859_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_IsPaid] ON [Orders] ([IsPaid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260913185859_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913185859_InitialCreate', N'10.0.12');
END;

COMMIT;
GO
