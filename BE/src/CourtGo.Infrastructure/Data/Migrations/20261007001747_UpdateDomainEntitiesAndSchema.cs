using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CourtGo.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDomainEntitiesAndSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Courts_CourtId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_CustomerId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingSlots_Bookings_BookingId",
                table: "BookingSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Courts_SportCenters_SportCenterId",
                table: "Courts");

            migrationBuilder.DropForeignKey(
                name: "FK_Courts_Sports_SportId",
                table: "Courts");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Bookings_BookingId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffAssignments_SportCenters_SportCenterId",
                table: "StaffAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffAssignments_Users_UserId",
                table: "StaffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StaffAssignments_SportCenterId",
                table: "StaffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StaffAssignments_UserId_SportCenterId",
                table: "StaffAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Sports_Name",
                table: "Sports");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_BookingSlots_CourtId_StartTime",
                table: "BookingSlots");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CheckInCode",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CourtId_StartTime",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "CloseTime",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "OpenTime",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TransactionReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "CheckInCode",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsWalkIn",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "StaffAssignments",
                newName: "StaffUserId");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "SportCenters",
                newName: "CoverImageUrl");

            migrationBuilder.RenameColumn(
                name: "CollectedByUserId",
                table: "Payments",
                newName: "RelatedPaymentId");

            migrationBuilder.RenameColumn(
                name: "PricePerHour",
                table: "Courts",
                newName: "BasePricePerHour");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "BookingSlots",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Bookings",
                newName: "BookingCode");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "Bookings",
                newName: "CustomerUserId");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Role",
                table: "Users",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailVerifiedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PhoneVerifiedAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "StaffAssignments",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "StaffAssignments",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "StaffAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Sports",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Sports",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Sports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Sports",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Sports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "SportCenters",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "SportCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "SportCenters",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "SportCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SportCenters",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "SportCenters",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "SportCenters",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "SportCenters",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                table: "SportCenters",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "SportCenters",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Asia/Ho_Chi_Minh");

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "SportCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Payments",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Payments",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedByUserId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidByUserId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "PaymentKind",
                table: "Payments",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "PaymentMethod",
                table: "Payments",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderTransactionId",
                table: "Payments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "TransactionStatus",
                table: "Payments",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "DeepLink",
                table: "Notifications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadAt",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReferenceId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "Notifications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Type",
                table: "Notifications",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Courts",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Status",
                table: "Courts",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Courts",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Courts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Courts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Courts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Courts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurfaceType",
                table: "Courts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "BookingSlots",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "BookingSlots",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndAt",
                table: "BookingSlots",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "HoldExpiresAt",
                table: "BookingSlots",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOccupying",
                table: "BookingSlots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceRuleId",
                table: "BookingSlots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "ReservationState",
                table: "BookingSlots",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartAt",
                table: "BookingSlots",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "PaymentStatus",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "HoldExpiresAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "(sysutcdatetime())",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "(newsequentialid())",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<byte>(
                name: "BookingStatus",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "CancellationPolicyId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CenterNameSnapshot",
                table: "Bookings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CourtNameSnapshot",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmailSnapshot",
                table: "Bookings",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerNameSnapshot",
                table: "Bookings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoneSnapshot",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DepositPercentSnapshot",
                table: "Bookings",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                table: "Bookings",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Source",
                table: "Bookings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "SportNameSnapshot",
                table: "Bookings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartAt",
                table: "Bookings",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateTable(
                name: "BookingStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<byte>(type: "tinyint", nullable: true),
                    ToStatus = table.Column<byte>(type: "tinyint", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CancellationPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CancellationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestSource = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)1),
                    CalculatedRefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    ApprovedRefundAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CancellationRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())"),
                    OutstandingPaymentOverride = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OverrideReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckIns_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CheckIns_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CourtBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    CourtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Type = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourtBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourtBlocks_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CourtBlocks_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OperatingHourExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    SportCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingHourExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatingHourExceptions_SportCenters_SportCenterId",
                        column: x => x.SportCenterId,
                        principalTable: "SportCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OperatingHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    SportCenterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatingHours_SportCenters_SportCenterId",
                        column: x => x.SportCenterId,
                        principalTable: "SportCenters",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PriceRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    CourtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PricePerHour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceRules_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeviceInfo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Reviews_Users_CustomerUserId",
                        column: x => x.CustomerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    HoldDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    MinBookingLeadMinutes = table.Column<int>(type: "int", nullable: false),
                    DefaultDepositPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AllowOutstandingCheckIn = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Platform = table.Column<byte>(type: "tinyint", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreferredAreaName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PreferredLatitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    PreferredLongitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    BookingNotifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PaymentNotifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReminderNotifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VerificationCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Target = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "(sysutcdatetime())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationCodes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CancellationPolicyRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    CancellationPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinHoursBeforeStart = table.Column<int>(type: "int", nullable: false),
                    MaxHoursBeforeStart = table.Column<int>(type: "int", nullable: true),
                    RefundPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationPolicyRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationPolicyRules_CancellationPolicies_CancellationPolicyId",
                        column: x => x.CancellationPolicyId,
                        principalTable: "CancellationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users",
                column: "PhoneNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignments_SportCenterId_IsActive",
                table: "StaffAssignments",
                columns: new[] { "SportCenterId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_StaffAssignments_OneActivePerStaff",
                table: "StaffAssignments",
                column: "StaffUserId",
                unique: true,
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "UX_Sports_Code",
                table: "Sports",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportCenters_District_City",
                table: "SportCenters",
                columns: new[] { "District", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_SportCenters_Status",
                table: "SportCenters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ConfirmedByUserId",
                table: "Payments",
                column: "ConfirmedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidByUserId",
                table: "Payments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RelatedPaymentId",
                table: "Payments",
                column: "RelatedPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_CreatedAt",
                table: "Payments",
                columns: new[] { "TransactionStatus", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_Payments_ProviderTransactionId",
                table: "Payments",
                column: "ProviderTransactionId",
                unique: true,
                filter: "([ProviderTransactionId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_Read_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Courts_Center_Sport_Status",
                table: "Courts",
                columns: new[] { "SportCenterId", "SportId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Courts_Center_Code",
                table: "Courts",
                columns: new[] { "SportCenterId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingSlots_Court_StartAt_EndAt",
                table: "BookingSlots",
                columns: new[] { "CourtId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingSlots_PriceRuleId",
                table: "BookingSlots",
                column: "PriceRuleId");

            migrationBuilder.CreateIndex(
                name: "UX_BookingSlots_ActiveCourtStart",
                table: "BookingSlots",
                columns: new[] { "CourtId", "StartAt" },
                unique: true,
                filter: "([IsOccupying]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CancellationPolicyId",
                table: "Bookings",
                column: "CancellationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Court_StartAt",
                table: "Bookings",
                columns: new[] { "CourtId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedByUser",
                table: "Bookings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Customer_StartAt",
                table: "Bookings",
                columns: new[] { "CustomerUserId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_StartAt",
                table: "Bookings",
                columns: new[] { "BookingStatus", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_Bookings_BookingCode",
                table: "Bookings",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Bookings_QrToken",
                table: "Bookings",
                column: "QrToken",
                unique: true,
                filter: "([QrToken] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_Booking_CreatedAt",
                table: "BookingStatusHistories",
                columns: new[] { "BookingId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_ChangedByUserId",
                table: "BookingStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_CancellationPolicies_Name_Version",
                table: "CancellationPolicies",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CancellationPolicies_OneActive",
                table: "CancellationPolicies",
                column: "IsActive",
                unique: true,
                filter: "([IsActive]=(1))");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationPolicyRules_Policy_MinHours",
                table: "CancellationPolicyRules",
                columns: new[] { "CancellationPolicyId", "MinHoursBeforeStart" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_Booking_Status",
                table: "CancellationRequests",
                columns: new[] { "BookingId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_ProcessedByUserId",
                table: "CancellationRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_RequestedByUserId",
                table: "CancellationRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequests_Status_CreatedAt",
                table: "CancellationRequests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CheckIns_StaffUserId",
                table: "CheckIns",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_CheckIns_BookingId",
                table: "CheckIns",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourtBlocks_Court_Start_End",
                table: "CourtBlocks",
                columns: new[] { "CourtId", "StartAt", "EndAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CourtBlocks_CreatedByUserId",
                table: "CourtBlocks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_OperatingHourExceptions_Center_Date",
                table: "OperatingHourExceptions",
                columns: new[] { "SportCenterId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_OperatingHours_Center_Day",
                table: "OperatingHours",
                columns: new[] { "SportCenterId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceRules_Court_Day_Active",
                table: "PriceRules",
                columns: new[] { "CourtId", "DayOfWeek", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerUserId",
                table: "Reviews",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Reviews_BookingId",
                table: "Reviews",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserId",
                table: "SystemSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId_IsActive",
                table: "UserDevices",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_DeviceToken",
                table: "UserDevices",
                column: "DeviceToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_Target_Purpose_ExpiresAt",
                table: "VerificationCodes",
                columns: new[] { "Target", "Purpose", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCodes_UserId",
                table: "VerificationCodes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_CancellationPolicies_CancellationPolicyId",
                table: "Bookings",
                column: "CancellationPolicyId",
                principalTable: "CancellationPolicies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Courts_CourtId",
                table: "Bookings",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_CreatedByUserId",
                table: "Bookings",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_CustomerUserId",
                table: "Bookings",
                column: "CustomerUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSlots_Bookings_BookingId",
                table: "BookingSlots",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSlots_Courts_CourtId",
                table: "BookingSlots",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSlots_PriceRules_PriceRuleId",
                table: "BookingSlots",
                column: "PriceRuleId",
                principalTable: "PriceRules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_SportCenters_SportCenterId",
                table: "Courts",
                column: "SportCenterId",
                principalTable: "SportCenters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_Sports_SportId",
                table: "Courts",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Bookings_BookingId",
                table: "Payments",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Payments_RelatedPaymentId",
                table: "Payments",
                column: "RelatedPaymentId",
                principalTable: "Payments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ConfirmedByUserId",
                table: "Payments",
                column: "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_PaidByUserId",
                table: "Payments",
                column: "PaidByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffAssignments_SportCenters_SportCenterId",
                table: "StaffAssignments",
                column: "SportCenterId",
                principalTable: "SportCenters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffAssignments_Users_StaffUserId",
                table: "StaffAssignments",
                column: "StaffUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_CancellationPolicies_CancellationPolicyId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Courts_CourtId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_CreatedByUserId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_CustomerUserId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingSlots_Bookings_BookingId",
                table: "BookingSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingSlots_Courts_CourtId",
                table: "BookingSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingSlots_PriceRules_PriceRuleId",
                table: "BookingSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_Courts_SportCenters_SportCenterId",
                table: "Courts");

            migrationBuilder.DropForeignKey(
                name: "FK_Courts_Sports_SportId",
                table: "Courts");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Bookings_BookingId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Payments_RelatedPaymentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ConfirmedByUserId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_PaidByUserId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffAssignments_SportCenters_SportCenterId",
                table: "StaffAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffAssignments_Users_StaffUserId",
                table: "StaffAssignments");

            migrationBuilder.DropTable(
                name: "BookingStatusHistories");

            migrationBuilder.DropTable(
                name: "CancellationPolicyRules");

            migrationBuilder.DropTable(
                name: "CancellationRequests");

            migrationBuilder.DropTable(
                name: "CheckIns");

            migrationBuilder.DropTable(
                name: "CourtBlocks");

            migrationBuilder.DropTable(
                name: "OperatingHourExceptions");

            migrationBuilder.DropTable(
                name: "OperatingHours");

            migrationBuilder.DropTable(
                name: "PriceRules");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "VerificationCodes");

            migrationBuilder.DropTable(
                name: "CancellationPolicies");

            migrationBuilder.DropIndex(
                name: "UX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_Users_PhoneNumber",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StaffAssignments_SportCenterId_IsActive",
                table: "StaffAssignments");

            migrationBuilder.DropIndex(
                name: "UX_StaffAssignments_OneActivePerStaff",
                table: "StaffAssignments");

            migrationBuilder.DropIndex(
                name: "UX_Sports_Code",
                table: "Sports");

            migrationBuilder.DropIndex(
                name: "IX_SportCenters_District_City",
                table: "SportCenters");

            migrationBuilder.DropIndex(
                name: "IX_SportCenters_Status",
                table: "SportCenters");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ConfirmedByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaidByUserId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RelatedPaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Status_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "UX_Payments_ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_User_Read_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Courts_Center_Sport_Status",
                table: "Courts");

            migrationBuilder.DropIndex(
                name: "UQ_Courts_Center_Code",
                table: "Courts");

            migrationBuilder.DropIndex(
                name: "IX_BookingSlots_Court_StartAt_EndAt",
                table: "BookingSlots");

            migrationBuilder.DropIndex(
                name: "IX_BookingSlots_PriceRuleId",
                table: "BookingSlots");

            migrationBuilder.DropIndex(
                name: "UX_BookingSlots_ActiveCourtStart",
                table: "BookingSlots");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CancellationPolicyId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Court_StartAt",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CreatedByUser",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Customer_StartAt",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_StartAt",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "UQ_Bookings_BookingCode",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "UX_Bookings_QrToken",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "SportCenters");

            migrationBuilder.DropColumn(
                name: "ConfirmedByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentKind",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TransactionStatus",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeepLink",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "SurfaceType",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "IsOccupying",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "PriceRuleId",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "ReservationState",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "StartAt",
                table: "BookingSlots");

            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancellationPolicyId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CenterNameSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CourtNameSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerEmailSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerNameSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CustomerPhoneSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DepositPercentSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "QrToken",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SportNameSnapshot",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StartAt",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "StaffUserId",
                table: "StaffAssignments",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "CoverImageUrl",
                table: "SportCenters",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "RelatedPaymentId",
                table: "Payments",
                newName: "CollectedByUserId");

            migrationBuilder.RenameColumn(
                name: "BasePricePerHour",
                table: "Courts",
                newName: "PricePerHour");

            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "BookingSlots",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "CustomerUserId",
                table: "Bookings",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "BookingCode",
                table: "Bookings",
                newName: "Status");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "StaffAssignments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedAt",
                table: "StaffAssignments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "StaffAssignments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Sports",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Sports",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Sports",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Sports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "SportCenters",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "District",
                table: "SportCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SportCenters",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "SportCenters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SportCenters",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "SportCenters",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CloseTime",
                table: "SportCenters",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "SportCenters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OpenTime",
                table: "SportCenters",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAt",
                table: "Payments",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AddColumn<string>(
                name: "Method",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TransactionReference",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Payments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "Notifications",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Notifications",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Courts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Courts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldDefaultValue: (byte)1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Courts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Courts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "BookingSlots",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "BookingSlots",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "BookingSlots",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "BookingSlots",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "BookingSlots",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentStatus",
                table: "Bookings",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "tinyint");

            migrationBuilder.AlterColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "(sysutcdatetime())");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Bookings",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "(newsequentialid())");

            migrationBuilder.AddColumn<string>(
                name: "CheckInCode",
                table: "Bookings",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsWalkIn",
                table: "Bookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Bookings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignments_SportCenterId",
                table: "StaffAssignments",
                column: "SportCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAssignments_UserId_SportCenterId",
                table: "StaffAssignments",
                columns: new[] { "UserId", "SportCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_Sports_Name",
                table: "Sports",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingSlots_CourtId_StartTime",
                table: "BookingSlots",
                columns: new[] { "CourtId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CheckInCode",
                table: "Bookings",
                column: "CheckInCode");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CourtId_StartTime",
                table: "Bookings",
                columns: new[] { "CourtId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId",
                table: "Bookings",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Courts_CourtId",
                table: "Bookings",
                column: "CourtId",
                principalTable: "Courts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_CustomerId",
                table: "Bookings",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSlots_Bookings_BookingId",
                table: "BookingSlots",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_SportCenters_SportCenterId",
                table: "Courts",
                column: "SportCenterId",
                principalTable: "SportCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_Sports_SportId",
                table: "Courts",
                column: "SportId",
                principalTable: "Sports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Bookings_BookingId",
                table: "Payments",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffAssignments_SportCenters_SportCenterId",
                table: "StaffAssignments",
                column: "SportCenterId",
                principalTable: "SportCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffAssignments_Users_UserId",
                table: "StaffAssignments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
