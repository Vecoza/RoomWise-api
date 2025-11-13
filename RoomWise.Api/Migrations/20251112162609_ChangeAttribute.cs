using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RoomWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Reservations_ReservationId1",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Hotels_HotelId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Promotions_PromotionId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_RoomTypes_RoomTypeId",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlists",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_RoomAvailabilities_RoomTypeId",
                table: "RoomAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_HotelId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReservationId1",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_HotelImages_HotelId",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "ReservationId1",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Wishlists",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Wishlists",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "RoomTypeId1",
                table: "Reservations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlists",
                table: "Wishlists",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_UserId_HotelId",
                table: "Wishlists",
                columns: new[] { "UserId", "HotelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomAvailabilities_RoomTypeId_Date",
                table: "RoomAvailabilities",
                columns: new[] { "RoomTypeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HotelId_CreatedAt",
                table: "Reviews",
                columns: new[] { "HotelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HotelId_UserId",
                table: "Reviews",
                columns: new[] { "HotelId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ConfirmationNumber",
                table: "Reservations",
                column: "ConfirmationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomTypeId1",
                table: "Reservations",
                column: "RoomTypeId1");

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId_SortOrder",
                table: "HotelImages",
                columns: new[] { "HotelId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Hotels_HotelId",
                table: "Reservations",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Promotions_PromotionId",
                table: "Reservations",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_RoomTypes_RoomTypeId",
                table: "Reservations",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_RoomTypes_RoomTypeId1",
                table: "Reservations",
                column: "RoomTypeId1",
                principalTable: "RoomTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Hotels_HotelId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Promotions_PromotionId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_RoomTypes_RoomTypeId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_RoomTypes_RoomTypeId1",
                table: "Reservations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Wishlists",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Wishlists_UserId_HotelId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_RoomAvailabilities_RoomTypeId_Date",
                table: "RoomAvailabilities");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_HotelId_CreatedAt",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_HotelId_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ConfirmationNumber",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RoomTypeId1",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_HotelImages_HotelId_SortOrder",
                table: "HotelImages");

            migrationBuilder.DropColumn(
                name: "RoomTypeId1",
                table: "Reservations");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Wishlists",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Wishlists",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "ReservationId1",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Wishlists",
                table: "Wishlists",
                columns: new[] { "UserId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAvailabilities_RoomTypeId",
                table: "RoomAvailabilities",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HotelId",
                table: "Reviews",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReservationId1",
                table: "Payments",
                column: "ReservationId1");

            migrationBuilder.CreateIndex(
                name: "IX_HotelImages_HotelId",
                table: "HotelImages",
                column: "HotelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Reservations_ReservationId1",
                table: "Payments",
                column: "ReservationId1",
                principalTable: "Reservations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Hotels_HotelId",
                table: "Reservations",
                column: "HotelId",
                principalTable: "Hotels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Promotions_PromotionId",
                table: "Reservations",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_RoomTypes_RoomTypeId",
                table: "Reservations",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
