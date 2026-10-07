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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [SportCenters] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [District] nvarchar(100) NULL,
        [City] nvarchar(100) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [ImageUrl] nvarchar(500) NULL,
        [OpenTime] time NOT NULL,
        [CloseTime] time NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_SportCenters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [Sports] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [IconUrl] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Sports] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [PasswordHash] nvarchar(512) NOT NULL,
        [Role] nvarchar(20) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [Courts] (
        [Id] uniqueidentifier NOT NULL,
        [SportCenterId] uniqueidentifier NOT NULL,
        [SportId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [PricePerHour] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Courts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Courts_SportCenters_SportCenterId] FOREIGN KEY ([SportCenterId]) REFERENCES [SportCenters] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Courts_Sports_SportId] FOREIGN KEY ([SportId]) REFERENCES [Sports] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [StaffAssignments] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [SportCenterId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_StaffAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StaffAssignments_SportCenters_SportCenterId] FOREIGN KEY ([SportCenterId]) REFERENCES [SportCenters] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_StaffAssignments_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [Bookings] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NULL,
        [CourtId] uniqueidentifier NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [DepositAmount] decimal(18,2) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [PaymentStatus] nvarchar(30) NOT NULL,
        [HoldExpiresAt] datetime2 NULL,
        [CheckInCode] nvarchar(64) NULL,
        [IsWalkIn] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Bookings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Bookings_Courts_CourtId] FOREIGN KEY ([CourtId]) REFERENCES [Courts] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Bookings_Users_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [BookingSlots] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [CourtId] uniqueidentifier NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_BookingSlots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookingSlots_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [BookingId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Method] nvarchar(20) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [TransactionReference] nvarchar(100) NULL,
        [PaidAt] datetime2 NULL,
        [CollectedByUserId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_CheckInCode] ON [Bookings] ([CheckInCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_CourtId_StartTime] ON [Bookings] ([CourtId], [StartTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Bookings_CustomerId] ON [Bookings] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BookingSlots_BookingId] ON [BookingSlots] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BookingSlots_CourtId_StartTime] ON [BookingSlots] ([CourtId], [StartTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courts_SportCenterId] ON [Courts] ([SportCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Courts_SportId] ON [Courts] ([SportId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_BookingId] ON [Payments] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sports_Name] ON [Sports] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StaffAssignments_SportCenterId] ON [StaffAssignments] ([SportCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_StaffAssignments_UserId_SportCenterId] ON [StaffAssignments] ([UserId], [SportCenterId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005154552_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005154552_InitialCreate', N'8.0.31');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] DROP CONSTRAINT [FK_Bookings_Courts_CourtId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] DROP CONSTRAINT [FK_Bookings_Users_CustomerId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] DROP CONSTRAINT [FK_BookingSlots_Bookings_BookingId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] DROP CONSTRAINT [FK_Courts_SportCenters_SportCenterId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] DROP CONSTRAINT [FK_Courts_Sports_SportId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] DROP CONSTRAINT [FK_Payments_Bookings_BookingId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [StaffAssignments] DROP CONSTRAINT [FK_StaffAssignments_SportCenters_SportCenterId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [StaffAssignments] DROP CONSTRAINT [FK_StaffAssignments_Users_UserId];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_Users_Email] ON [Users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_StaffAssignments_SportCenterId] ON [StaffAssignments];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_StaffAssignments_UserId_SportCenterId] ON [StaffAssignments];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_Sports_Name] ON [Sports];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_Notifications_UserId_IsRead] ON [Notifications];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_BookingSlots_CourtId_StartTime] ON [BookingSlots];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_Bookings_CheckInCode] ON [Bookings];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_Bookings_CourtId_StartTime] ON [Bookings];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DROP INDEX [IX_Bookings_CustomerId] ON [Bookings];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sports]') AND [c].[name] = N'UpdatedAt');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Sports] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Sports] DROP COLUMN [UpdatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'Address');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [SportCenters] DROP COLUMN [Address];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'CloseTime');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [SportCenters] DROP COLUMN [CloseTime];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'IsActive');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [SportCenters] DROP COLUMN [IsActive];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'OpenTime');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [SportCenters] DROP COLUMN [OpenTime];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'Method');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Payments] DROP COLUMN [Method];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'Status');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Payments] DROP COLUMN [Status];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'TransactionReference');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Payments] DROP COLUMN [TransactionReference];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'Type');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [Payments] DROP COLUMN [Type];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'UpdatedAt');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [Payments] DROP COLUMN [UpdatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'UpdatedAt');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [Notifications] DROP COLUMN [UpdatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BookingSlots]') AND [c].[name] = N'EndTime');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [BookingSlots] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [BookingSlots] DROP COLUMN [EndTime];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BookingSlots]') AND [c].[name] = N'StartTime');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [BookingSlots] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [BookingSlots] DROP COLUMN [StartTime];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BookingSlots]') AND [c].[name] = N'UpdatedAt');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [BookingSlots] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [BookingSlots] DROP COLUMN [UpdatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'CheckInCode');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [CheckInCode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'EndTime');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [EndTime];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'IsWalkIn');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [IsWalkIn];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'Note');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [Note];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'StartTime');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [Bookings] DROP COLUMN [StartTime];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[StaffAssignments].[UserId]', N'StaffUserId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[SportCenters].[ImageUrl]', N'CoverImageUrl', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[Payments].[CollectedByUserId]', N'RelatedPaymentId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[Courts].[PricePerHour]', N'BasePricePerHour', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[BookingSlots].[Price]', N'UnitPrice', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[Bookings].[Status]', N'BookingCode', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC sp_rename N'[Bookings].[CustomerId]', N'CustomerUserId', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'UpdatedAt');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [Users] ALTER COLUMN [UpdatedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Role');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [Users] ALTER COLUMN [Role] tinyint NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PhoneNumber');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var21 + '];');
    EXEC(N'UPDATE [Users] SET [PhoneNumber] = N'''' WHERE [PhoneNumber] IS NULL');
    ALTER TABLE [Users] ALTER COLUMN [PhoneNumber] nvarchar(20) NOT NULL;
    ALTER TABLE [Users] ADD DEFAULT N'' FOR [PhoneNumber];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PasswordHash');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [Users] ALTER COLUMN [PasswordHash] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'IsActive');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [Users] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Email');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [Users] ALTER COLUMN [Email] nvarchar(255) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CreatedAt');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var25 + '];');
    ALTER TABLE [Users] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [Users] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'Id');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [Users] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Users] ADD [AvatarUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Users] ADD [EmailVerifiedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Users] ADD [PhoneVerifiedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StaffAssignments]') AND [c].[name] = N'IsActive');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [StaffAssignments] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [StaffAssignments] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StaffAssignments]') AND [c].[name] = N'AssignedAt');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [StaffAssignments] DROP CONSTRAINT [' + @var28 + '];');
    ALTER TABLE [StaffAssignments] ALTER COLUMN [AssignedAt] datetimeoffset NOT NULL;
    ALTER TABLE [StaffAssignments] ADD DEFAULT ((sysutcdatetime())) FOR [AssignedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[StaffAssignments]') AND [c].[name] = N'Id');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [StaffAssignments] DROP CONSTRAINT [' + @var29 + '];');
    ALTER TABLE [StaffAssignments] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sports]') AND [c].[name] = N'IsActive');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Sports] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [Sports] ADD DEFAULT CAST(1 AS bit) FOR [IsActive];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sports]') AND [c].[name] = N'CreatedAt');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Sports] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [Sports] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [Sports] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Sports]') AND [c].[name] = N'Id');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Sports] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [Sports] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Sports] ADD [Code] nvarchar(30) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Sports] ADD [DisplayOrder] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'UpdatedAt');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var33 + '];');
    ALTER TABLE [SportCenters] ALTER COLUMN [UpdatedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var34 sysname;
    SELECT @var34 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'District');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var34 + '];');
    EXEC(N'UPDATE [SportCenters] SET [District] = N'''' WHERE [District] IS NULL');
    ALTER TABLE [SportCenters] ALTER COLUMN [District] nvarchar(100) NOT NULL;
    ALTER TABLE [SportCenters] ADD DEFAULT N'' FOR [District];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var35 sysname;
    SELECT @var35 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'CreatedAt');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var35 + '];');
    ALTER TABLE [SportCenters] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [SportCenters] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var36 sysname;
    SELECT @var36 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'City');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var36 + '];');
    EXEC(N'UPDATE [SportCenters] SET [City] = N'''' WHERE [City] IS NULL');
    ALTER TABLE [SportCenters] ALTER COLUMN [City] nvarchar(100) NOT NULL;
    ALTER TABLE [SportCenters] ADD DEFAULT N'' FOR [City];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var37 sysname;
    SELECT @var37 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SportCenters]') AND [c].[name] = N'Id');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [SportCenters] DROP CONSTRAINT [' + @var37 + '];');
    ALTER TABLE [SportCenters] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [SportCenters] ADD [AddressLine] nvarchar(300) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [SportCenters] ADD [Latitude] decimal(9,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [SportCenters] ADD [Longitude] decimal(9,6) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [SportCenters] ADD [Status] tinyint NOT NULL DEFAULT CAST(1 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [SportCenters] ADD [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N'Asia/Ho_Chi_Minh';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [SportCenters] ADD [Ward] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var38 sysname;
    SELECT @var38 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'PaidAt');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var38 + '];');
    ALTER TABLE [Payments] ALTER COLUMN [PaidAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var39 sysname;
    SELECT @var39 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'CreatedAt');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var39 + '];');
    ALTER TABLE [Payments] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [Payments] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var40 sysname;
    SELECT @var40 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Payments]') AND [c].[name] = N'Id');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [Payments] DROP CONSTRAINT [' + @var40 + '];');
    ALTER TABLE [Payments] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD [ConfirmedByUserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD [PaidByUserId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD [PaymentKind] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD [PaymentMethod] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD [ProviderTransactionId] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD [TransactionStatus] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var41 sysname;
    SELECT @var41 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'IsRead');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var41 + '];');
    ALTER TABLE [Notifications] ADD DEFAULT CAST(0 AS bit) FOR [IsRead];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var42 sysname;
    SELECT @var42 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'CreatedAt');
    IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var42 + '];');
    ALTER TABLE [Notifications] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [Notifications] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var43 sysname;
    SELECT @var43 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'Id');
    IF @var43 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var43 + '];');
    ALTER TABLE [Notifications] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Notifications] ADD [DeepLink] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Notifications] ADD [ReadAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Notifications] ADD [ReferenceId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Notifications] ADD [ReferenceType] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Notifications] ADD [Type] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var44 sysname;
    SELECT @var44 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Courts]') AND [c].[name] = N'UpdatedAt');
    IF @var44 IS NOT NULL EXEC(N'ALTER TABLE [Courts] DROP CONSTRAINT [' + @var44 + '];');
    ALTER TABLE [Courts] ALTER COLUMN [UpdatedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var45 sysname;
    SELECT @var45 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Courts]') AND [c].[name] = N'Status');
    IF @var45 IS NOT NULL EXEC(N'ALTER TABLE [Courts] DROP CONSTRAINT [' + @var45 + '];');
    ALTER TABLE [Courts] ALTER COLUMN [Status] tinyint NOT NULL;
    ALTER TABLE [Courts] ADD DEFAULT CAST(1 AS tinyint) FOR [Status];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var46 sysname;
    SELECT @var46 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Courts]') AND [c].[name] = N'CreatedAt');
    IF @var46 IS NOT NULL EXEC(N'ALTER TABLE [Courts] DROP CONSTRAINT [' + @var46 + '];');
    ALTER TABLE [Courts] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [Courts] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var47 sysname;
    SELECT @var47 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Courts]') AND [c].[name] = N'Id');
    IF @var47 IS NOT NULL EXEC(N'ALTER TABLE [Courts] DROP CONSTRAINT [' + @var47 + '];');
    ALTER TABLE [Courts] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] ADD [Code] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] ADD [CoverImageUrl] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] ADD [Description] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] ADD [SurfaceType] nvarchar(100) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var48 sysname;
    SELECT @var48 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BookingSlots]') AND [c].[name] = N'CreatedAt');
    IF @var48 IS NOT NULL EXEC(N'ALTER TABLE [BookingSlots] DROP CONSTRAINT [' + @var48 + '];');
    ALTER TABLE [BookingSlots] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [BookingSlots] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var49 sysname;
    SELECT @var49 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BookingSlots]') AND [c].[name] = N'Id');
    IF @var49 IS NOT NULL EXEC(N'ALTER TABLE [BookingSlots] DROP CONSTRAINT [' + @var49 + '];');
    ALTER TABLE [BookingSlots] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD [EndAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD [HoldExpiresAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD [IsOccupying] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD [PriceRuleId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD [ReservationState] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD [StartAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var50 sysname;
    SELECT @var50 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'UpdatedAt');
    IF @var50 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var50 + '];');
    ALTER TABLE [Bookings] ALTER COLUMN [UpdatedAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var51 sysname;
    SELECT @var51 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'PaymentStatus');
    IF @var51 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var51 + '];');
    ALTER TABLE [Bookings] ALTER COLUMN [PaymentStatus] tinyint NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var52 sysname;
    SELECT @var52 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'HoldExpiresAt');
    IF @var52 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var52 + '];');
    ALTER TABLE [Bookings] ALTER COLUMN [HoldExpiresAt] datetimeoffset NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var53 sysname;
    SELECT @var53 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'CreatedAt');
    IF @var53 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var53 + '];');
    ALTER TABLE [Bookings] ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
    ALTER TABLE [Bookings] ADD DEFAULT ((sysutcdatetime())) FOR [CreatedAt];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    DECLARE @var54 sysname;
    SELECT @var54 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Bookings]') AND [c].[name] = N'Id');
    IF @var54 IS NOT NULL EXEC(N'ALTER TABLE [Bookings] DROP CONSTRAINT [' + @var54 + '];');
    ALTER TABLE [Bookings] ADD DEFAULT ((newsequentialid())) FOR [Id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [BookingStatus] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CancellationPolicyId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CenterNameSnapshot] nvarchar(200) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CourtNameSnapshot] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CreatedByUserId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CustomerEmailSnapshot] nvarchar(255) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CustomerNameSnapshot] nvarchar(150) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [CustomerPhoneSnapshot] nvarchar(20) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [DepositPercentSnapshot] decimal(5,2) NOT NULL DEFAULT 0.0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [DurationMinutes] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [EndAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [QrToken] nvarchar(150) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [Source] tinyint NOT NULL DEFAULT CAST(0 AS tinyint);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [SportNameSnapshot] nvarchar(100) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD [StartAt] datetimeoffset NOT NULL DEFAULT '0001-01-01T00:00:00.0000000+00:00';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [BookingStatusHistories] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [BookingId] uniqueidentifier NOT NULL,
        [FromStatus] tinyint NULL,
        [ToStatus] tinyint NOT NULL,
        [ChangedByUserId] uniqueidentifier NULL,
        [Reason] nvarchar(500) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_BookingStatusHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookingStatusHistories_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_BookingStatusHistories_Users_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [CancellationPolicies] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [Name] nvarchar(150) NOT NULL,
        [Version] int NOT NULL,
        [EffectiveFrom] datetimeoffset NOT NULL,
        [EffectiveTo] datetimeoffset NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_CancellationPolicies] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [CancellationRequests] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [BookingId] uniqueidentifier NOT NULL,
        [RequestedByUserId] uniqueidentifier NOT NULL,
        [RequestSource] tinyint NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] tinyint NOT NULL DEFAULT CAST(1 AS tinyint),
        [CalculatedRefundAmount] decimal(18,2) NOT NULL DEFAULT 0.0,
        [ApprovedRefundAmount] decimal(18,2) NULL,
        [ProcessedByUserId] uniqueidentifier NULL,
        [ProcessedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_CancellationRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CancellationRequests_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_CancellationRequests_Users_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [Users] ([Id]),
        CONSTRAINT [FK_CancellationRequests_Users_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [CheckIns] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [BookingId] uniqueidentifier NOT NULL,
        [StaffUserId] uniqueidentifier NOT NULL,
        [Method] tinyint NOT NULL,
        [CheckedInAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        [OutstandingPaymentOverride] bit NOT NULL DEFAULT CAST(0 AS bit),
        [OverrideReason] nvarchar(500) NULL,
        [Note] nvarchar(500) NULL,
        CONSTRAINT [PK_CheckIns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CheckIns_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_CheckIns_Users_StaffUserId] FOREIGN KEY ([StaffUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [CourtBlocks] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [CourtId] uniqueidentifier NOT NULL,
        [StartAt] datetimeoffset NOT NULL,
        [EndAt] datetimeoffset NOT NULL,
        [Type] tinyint NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_CourtBlocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CourtBlocks_Courts_CourtId] FOREIGN KEY ([CourtId]) REFERENCES [Courts] ([Id]),
        CONSTRAINT [FK_CourtBlocks_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [OperatingHourExceptions] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [SportCenterId] uniqueidentifier NOT NULL,
        [Date] date NOT NULL,
        [IsClosed] bit NOT NULL DEFAULT CAST(0 AS bit),
        [OpenTime] time NULL,
        [CloseTime] time NULL,
        [Reason] nvarchar(300) NULL,
        CONSTRAINT [PK_OperatingHourExceptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OperatingHourExceptions_SportCenters_SportCenterId] FOREIGN KEY ([SportCenterId]) REFERENCES [SportCenters] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [OperatingHours] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [SportCenterId] uniqueidentifier NOT NULL,
        [DayOfWeek] tinyint NOT NULL,
        [OpenTime] time NULL,
        [CloseTime] time NULL,
        [IsClosed] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_OperatingHours] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OperatingHours_SportCenters_SportCenterId] FOREIGN KEY ([SportCenterId]) REFERENCES [SportCenters] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [PriceRules] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [CourtId] uniqueidentifier NOT NULL,
        [DayOfWeek] tinyint NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [PricePerHour] decimal(18,2) NOT NULL,
        [EffectiveFrom] date NULL,
        [EffectiveTo] date NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_PriceRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PriceRules_Courts_CourtId] FOREIGN KEY ([CourtId]) REFERENCES [Courts] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [UserId] uniqueidentifier NOT NULL,
        [TokenHash] nvarchar(500) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [RevokedAt] datetimeoffset NULL,
        [DeviceInfo] nvarchar(250) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [Reviews] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [BookingId] uniqueidentifier NOT NULL,
        [CustomerUserId] uniqueidentifier NOT NULL,
        [Rating] tinyint NOT NULL,
        [Comment] nvarchar(1000) NULL,
        [IsVisible] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Reviews_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]),
        CONSTRAINT [FK_Reviews_Users_CustomerUserId] FOREIGN KEY ([CustomerUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [SystemSettings] (
        [Id] tinyint NOT NULL,
        [HoldDurationMinutes] int NOT NULL,
        [MinBookingLeadMinutes] int NOT NULL,
        [DefaultDepositPercent] decimal(5,2) NOT NULL,
        [AllowOutstandingCheckIn] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UpdatedByUserId] uniqueidentifier NULL,
        [UpdatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SystemSettings_Users_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [Users] ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [UserDevices] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [UserId] uniqueidentifier NOT NULL,
        [DeviceToken] nvarchar(500) NOT NULL,
        [Platform] tinyint NOT NULL,
        [DeviceName] nvarchar(150) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [LastSeenAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_UserDevices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserDevices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [UserPreferences] (
        [UserId] uniqueidentifier NOT NULL,
        [PreferredAreaName] nvarchar(150) NULL,
        [PreferredLatitude] decimal(9,6) NULL,
        [PreferredLongitude] decimal(9,6) NULL,
        [BookingNotifications] bit NOT NULL DEFAULT CAST(1 AS bit),
        [PaymentNotifications] bit NOT NULL DEFAULT CAST(1 AS bit),
        [ReminderNotifications] bit NOT NULL DEFAULT CAST(1 AS bit),
        [UpdatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_UserPreferences] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [VerificationCodes] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [UserId] uniqueidentifier NULL,
        [Target] nvarchar(255) NOT NULL,
        [Purpose] tinyint NOT NULL,
        [CodeHash] nvarchar(500) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [ConsumedAt] datetimeoffset NULL,
        [AttemptCount] int NOT NULL DEFAULT 0,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT ((sysutcdatetime())),
        CONSTRAINT [PK_VerificationCodes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VerificationCodes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE TABLE [CancellationPolicyRules] (
        [Id] uniqueidentifier NOT NULL DEFAULT ((newsequentialid())),
        [CancellationPolicyId] uniqueidentifier NOT NULL,
        [MinHoursBeforeStart] int NOT NULL,
        [MaxHoursBeforeStart] int NULL,
        [RefundPercent] decimal(5,2) NOT NULL,
        CONSTRAINT [PK_CancellationPolicyRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CancellationPolicyRules_CancellationPolicies_CancellationPolicyId] FOREIGN KEY ([CancellationPolicyId]) REFERENCES [CancellationPolicies] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Users_Email] ON [Users] ([Email]) WHERE [Email] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Users_PhoneNumber] ON [Users] ([PhoneNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_StaffAssignments_SportCenterId_IsActive] ON [StaffAssignments] ([SportCenterId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_StaffAssignments_OneActivePerStaff] ON [StaffAssignments] ([StaffUserId]) WHERE ([IsActive]=(1))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Sports_Code] ON [Sports] ([Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_SportCenters_District_City] ON [SportCenters] ([District], [City]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_SportCenters_Status] ON [SportCenters] ([Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Payments_ConfirmedByUserId] ON [Payments] ([ConfirmedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Payments_PaidByUserId] ON [Payments] ([PaidByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Payments_RelatedPaymentId] ON [Payments] ([RelatedPaymentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Payments_Status_CreatedAt] ON [Payments] ([TransactionStatus], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Payments_ProviderTransactionId] ON [Payments] ([ProviderTransactionId]) WHERE ([ProviderTransactionId] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Notifications_User_Read_CreatedAt] ON [Notifications] ([UserId], [IsRead], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Courts_Center_Sport_Status] ON [Courts] ([SportCenterId], [SportId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Courts_Center_Code] ON [Courts] ([SportCenterId], [Code]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_BookingSlots_Court_StartAt_EndAt] ON [BookingSlots] ([CourtId], [StartAt], [EndAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_BookingSlots_PriceRuleId] ON [BookingSlots] ([PriceRuleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BookingSlots_ActiveCourtStart] ON [BookingSlots] ([CourtId], [StartAt]) WHERE ([IsOccupying]=(1))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Bookings_CancellationPolicyId] ON [Bookings] ([CancellationPolicyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Bookings_Court_StartAt] ON [Bookings] ([CourtId], [StartAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Bookings_CreatedByUser] ON [Bookings] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Bookings_Customer_StartAt] ON [Bookings] ([CustomerUserId], [StartAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Bookings_Status_StartAt] ON [Bookings] ([BookingStatus], [StartAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Bookings_BookingCode] ON [Bookings] ([BookingCode]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Bookings_QrToken] ON [Bookings] ([QrToken]) WHERE ([QrToken] IS NOT NULL)');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_BookingStatusHistories_Booking_CreatedAt] ON [BookingStatusHistories] ([BookingId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_BookingStatusHistories_ChangedByUserId] ON [BookingStatusHistories] ([ChangedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_CancellationPolicies_Name_Version] ON [CancellationPolicies] ([Name], [Version]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_CancellationPolicies_OneActive] ON [CancellationPolicies] ([IsActive]) WHERE ([IsActive]=(1))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CancellationPolicyRules_Policy_MinHours] ON [CancellationPolicyRules] ([CancellationPolicyId], [MinHoursBeforeStart]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CancellationRequests_Booking_Status] ON [CancellationRequests] ([BookingId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CancellationRequests_ProcessedByUserId] ON [CancellationRequests] ([ProcessedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CancellationRequests_RequestedByUserId] ON [CancellationRequests] ([RequestedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CancellationRequests_Status_CreatedAt] ON [CancellationRequests] ([Status], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CheckIns_StaffUserId] ON [CheckIns] ([StaffUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_CheckIns_BookingId] ON [CheckIns] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CourtBlocks_Court_Start_End] ON [CourtBlocks] ([CourtId], [StartAt], [EndAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_CourtBlocks_CreatedByUserId] ON [CourtBlocks] ([CreatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_OperatingHourExceptions_Center_Date] ON [OperatingHourExceptions] ([SportCenterId], [Date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_OperatingHours_Center_Day] ON [OperatingHours] ([SportCenterId], [DayOfWeek]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_PriceRules_Court_Day_Active] ON [PriceRules] ([CourtId], [DayOfWeek], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId_ExpiresAt] ON [RefreshTokens] ([UserId], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_Reviews_CustomerUserId] ON [Reviews] ([CustomerUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Reviews_BookingId] ON [Reviews] ([BookingId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_SystemSettings_UpdatedByUserId] ON [SystemSettings] ([UpdatedByUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_UserDevices_UserId_IsActive] ON [UserDevices] ([UserId], [IsActive]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE UNIQUE INDEX [UX_UserDevices_DeviceToken] ON [UserDevices] ([DeviceToken]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_VerificationCodes_Target_Purpose_ExpiresAt] ON [VerificationCodes] ([Target], [Purpose], [ExpiresAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    CREATE INDEX [IX_VerificationCodes_UserId] ON [VerificationCodes] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_CancellationPolicies_CancellationPolicyId] FOREIGN KEY ([CancellationPolicyId]) REFERENCES [CancellationPolicies] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Courts_CourtId] FOREIGN KEY ([CourtId]) REFERENCES [Courts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Bookings] ADD CONSTRAINT [FK_Bookings_Users_CustomerUserId] FOREIGN KEY ([CustomerUserId]) REFERENCES [Users] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD CONSTRAINT [FK_BookingSlots_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD CONSTRAINT [FK_BookingSlots_Courts_CourtId] FOREIGN KEY ([CourtId]) REFERENCES [Courts] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [BookingSlots] ADD CONSTRAINT [FK_BookingSlots_PriceRules_PriceRuleId] FOREIGN KEY ([PriceRuleId]) REFERENCES [PriceRules] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] ADD CONSTRAINT [FK_Courts_SportCenters_SportCenterId] FOREIGN KEY ([SportCenterId]) REFERENCES [SportCenters] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Courts] ADD CONSTRAINT [FK_Courts_Sports_SportId] FOREIGN KEY ([SportId]) REFERENCES [Sports] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Bookings_BookingId] FOREIGN KEY ([BookingId]) REFERENCES [Bookings] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Payments_RelatedPaymentId] FOREIGN KEY ([RelatedPaymentId]) REFERENCES [Payments] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Users_ConfirmedByUserId] FOREIGN KEY ([ConfirmedByUserId]) REFERENCES [Users] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [Payments] ADD CONSTRAINT [FK_Payments_Users_PaidByUserId] FOREIGN KEY ([PaidByUserId]) REFERENCES [Users] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [StaffAssignments] ADD CONSTRAINT [FK_StaffAssignments_SportCenters_SportCenterId] FOREIGN KEY ([SportCenterId]) REFERENCES [SportCenters] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    ALTER TABLE [StaffAssignments] ADD CONSTRAINT [FK_StaffAssignments_Users_StaffUserId] FOREIGN KEY ([StaffUserId]) REFERENCES [Users] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261007001747_UpdateDomainEntitiesAndSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261007001747_UpdateDomainEntitiesAndSchema', N'8.0.31');
END;
GO

COMMIT;
GO

