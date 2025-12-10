using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationIdToReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "Reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ReservationId",
                table: "Reviews",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Reservations_ReservationId",
                table: "Reviews",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Reservations_ReservationId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ReservationId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "Reviews");
        }
    }
}
