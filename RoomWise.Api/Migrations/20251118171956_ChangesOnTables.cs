using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangesOnTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservationAddOns_ReservationId",
                table: "ReservationAddOns");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAddOns_ReservationId_AddOnId",
                table: "ReservationAddOns",
                columns: new[] { "ReservationId", "AddOnId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservationAddOns_ReservationId_AddOnId",
                table: "ReservationAddOns");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAddOns_ReservationId",
                table: "ReservationAddOns",
                column: "ReservationId");
        }
    }
}
