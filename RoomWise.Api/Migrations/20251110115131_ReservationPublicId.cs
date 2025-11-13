using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReservationPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"pgcrypto\";");

            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Reservations",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.Sql("UPDATE \"Reservations\" SET \"PublicId\" = gen_random_uuid() WHERE \"PublicId\" = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PublicId",
                table: "Reservations",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_PublicId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Reservations");
        }
    }
}
