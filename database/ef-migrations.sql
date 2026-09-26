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
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926084813_AddProductCatalogue'
)
BEGIN
    CREATE TABLE [Categories] (
        [CategoryID] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryID])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926084813_AddProductCatalogue'
)
BEGIN
    CREATE TABLE [Products] (
        [ProductID] int NOT NULL IDENTITY,
        [CategoryID] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [UnitPrice] money NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Products] PRIMARY KEY ([ProductID]),
        CONSTRAINT [CK_Products_UnitPrice] CHECK ([UnitPrice] >= 0.0001 AND [UnitPrice] <= 922337203685477.5807),
        CONSTRAINT [FK_Products_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926084813_AddProductCatalogue'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Categories_Name] ON [Categories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926084813_AddProductCatalogue'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryID_IsActive] ON [Products] ([CategoryID], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926084813_AddProductCatalogue'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Products_CategoryID_Name] ON [Products] ([CategoryID], [Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926084813_AddProductCatalogue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260926084813_AddProductCatalogue', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    DROP INDEX [IX_Orders_IsPaid] ON [Orders];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [OrderItems] DROP CONSTRAINT [CK_OrderItems_Price];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    EXEC sp_rename N'[OrderItems].[Price]', N'UnitPrice', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    EXEC sp_rename N'[OrderItems].[ItemName]', N'ProductName', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [Orders] ADD [CancelledAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [Orders] ADD [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME());
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [Orders] ADD [PaidAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [Orders] ADD [Status] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [ProductID] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [OrderItems] ADD [Quantity] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    UPDATE [Orders] SET [Status] = CASE WHEN [IsPaid] = CAST(1 AS bit) THEN CAST(1 AS tinyint) ELSE CAST(0 AS tinyint) END, [PaidAt] = CASE WHEN [IsPaid] = CAST(1 AS bit) THEN [CreatedAt] ELSE NULL END;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'IsPaid');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Orders] DROP COLUMN [IsPaid];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    CREATE INDEX [IX_Orders_Status_CreatedAt] ON [Orders] ([Status], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    EXEC(N'ALTER TABLE [Orders] ADD CONSTRAINT [CK_Orders_Status] CHECK ([Status] IN (0, 1, 2))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductID] ON [OrderItems] ([ProductID]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    EXEC(N'ALTER TABLE [OrderItems] ADD CONSTRAINT [CK_OrderItems_Quantity] CHECK ([Quantity] > 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    EXEC(N'ALTER TABLE [OrderItems] ADD CONSTRAINT [CK_OrderItems_UnitPrice] CHECK ([UnitPrice] >= 0.0001 AND [UnitPrice] <= 922337203685477.5807)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260926091539_UpgradeOrderDomain'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260926091539_UpgradeOrderDomain', N'10.0.12');
END;

COMMIT;
GO
