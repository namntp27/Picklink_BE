using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Picklink.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3VenueManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courts_Venues_VenueId",
                table: "Courts");

            migrationBuilder.DropTable(
                name: "CourtFeatures");

            migrationBuilder.DropTable(
                name: "CourtSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Venues_OwnerId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Courts_VenueId_Name",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Courts");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Venues",
                newName: "StreetAddress");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Venues",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,7)",
                oldPrecision: 10,
                oldScale: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Venues",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,7)",
                oldPrecision: 10,
                oldScale: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Venues",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Venues",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ProvinceId",
                table: "Venues",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Venues",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "Venues",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedBy",
                table: "Venues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SubmittedAt",
                table: "Venues",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WardId",
                table: "Venues",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Courts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "Courts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "CourtImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "CourtBlockedSlots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AdministrativeProvinces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CodeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DivisionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrativeProvinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VenueAmenities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueAmenities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueAmenities_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueImages_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueOpeningHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VenueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueOpeningHours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueOpeningHours_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdministrativeWards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvinceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CodeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DivisionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrativeWards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdministrativeWards_AdministrativeProvinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "AdministrativeProvinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Venues_OwnerId_Status",
                table: "Venues",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Venues_ProvinceId",
                table: "Venues",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_WardId",
                table: "Venues",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_Courts_VenueId_Code",
                table: "Courts",
                columns: new[] { "VenueId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeProvinces_Code",
                table: "AdministrativeProvinces",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeWards_Code",
                table: "AdministrativeWards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeWards_ProvinceId_Name",
                table: "AdministrativeWards",
                columns: new[] { "ProvinceId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_VenueAmenities_VenueId",
                table: "VenueAmenities",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueImages_VenueId",
                table: "VenueImages",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueOpeningHours_VenueId_DayOfWeek",
                table: "VenueOpeningHours",
                columns: new[] { "VenueId", "DayOfWeek" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_Venues_VenueId",
                table: "Courts",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_AdministrativeProvinces_ProvinceId",
                table: "Venues",
                column: "ProvinceId",
                principalTable: "AdministrativeProvinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_AdministrativeWards_WardId",
                table: "Venues",
                column: "WardId",
                principalTable: "AdministrativeWards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courts_Venues_VenueId",
                table: "Courts");

            migrationBuilder.DropForeignKey(
                name: "FK_Venues_AdministrativeProvinces_ProvinceId",
                table: "Venues");

            migrationBuilder.DropForeignKey(
                name: "FK_Venues_AdministrativeWards_WardId",
                table: "Venues");

            migrationBuilder.DropTable(
                name: "AdministrativeWards");

            migrationBuilder.DropTable(
                name: "VenueAmenities");

            migrationBuilder.DropTable(
                name: "VenueImages");

            migrationBuilder.DropTable(
                name: "VenueOpeningHours");

            migrationBuilder.DropTable(
                name: "AdministrativeProvinces");

            migrationBuilder.DropIndex(
                name: "IX_Venues_OwnerId_Status",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_ProvinceId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_WardId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Courts_VenueId_Code",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "SlotDurationMinutes",
                table: "Courts");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "CourtImages");

            migrationBuilder.RenameColumn(
                name: "StreetAddress",
                table: "Venues",
                newName: "Address");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Venues",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,7)",
                oldPrecision: 10,
                oldScale: 7);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Venues",
                type: "decimal(10,7)",
                precision: 10,
                scale: 7,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,7)",
                oldPrecision: 10,
                oldScale: 7);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Venues",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Venues",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Venues",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "Venues",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Courts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "CourtBlockedSlots",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CourtFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourtFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourtFeatures_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourtSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourtId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourtSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourtSchedules_Courts_CourtId",
                        column: x => x.CourtId,
                        principalTable: "Courts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Venues_OwnerId",
                table: "Venues",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Courts_VenueId_Name",
                table: "Courts",
                columns: new[] { "VenueId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_CourtFeatures_CourtId",
                table: "CourtFeatures",
                column: "CourtId");

            migrationBuilder.CreateIndex(
                name: "IX_CourtSchedules_CourtId",
                table: "CourtSchedules",
                column: "CourtId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courts_Venues_VenueId",
                table: "Courts",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
