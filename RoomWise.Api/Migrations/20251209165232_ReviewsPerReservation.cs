using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReviewsPerReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_HotelId_UserId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ReservationId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReservationId_UserId",
                table: "Reviews",
                columns: new[] { "ReservationId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_ReservationId_UserId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_HotelId_UserId",
                table: "Reviews",
                columns: new[] { "HotelId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReservationId",
                table: "Reviews",
                column: "ReservationId");
        }
    }
}
