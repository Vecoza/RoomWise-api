using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RoomWise.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationReminderFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationAddOns_AddOns_AddOnId",
                table: "ReservationAddOns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationAddOns",
                table: "ReservationAddOns");

            migrationBuilder.AddColumn<bool>(
                name: "CheckInReminderSent",
                table: "Reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CheckOutReminderSent",
                table: "Reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ReservationAddOns",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<decimal>(
                name: "LineTotal",
                table: "ReservationAddOns",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "PaymentMethods",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentMethodId",
                table: "PaymentMethods",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AddOns",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AddOns",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "AddOns",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldMaxLength: 3);

            migrationBuilder.AddColumn<string>(
                name: "PricingModel",
                table: "AddOns",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationAddOns",
                table: "ReservationAddOns",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationAddOns_ReservationId",
                table: "ReservationAddOns",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationAddOns_AddOns_AddOnId",
                table: "ReservationAddOns",
                column: "AddOnId",
                principalTable: "AddOns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationAddOns_AddOns_AddOnId",
                table: "ReservationAddOns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationAddOns",
                table: "ReservationAddOns");

            migrationBuilder.DropIndex(
                name: "IX_ReservationAddOns_ReservationId",
                table: "ReservationAddOns");

            migrationBuilder.DropColumn(
                name: "CheckInReminderSent",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CheckOutReminderSent",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ReservationAddOns");

            migrationBuilder.DropColumn(
                name: "LineTotal",
                table: "ReservationAddOns");

            migrationBuilder.DropColumn(
                name: "StripePaymentMethodId",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "PricingModel",
                table: "AddOns");

            migrationBuilder.AlterColumn<string>(
                name: "StripeCustomerId",
                table: "PaymentMethods",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AddOns",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AddOns",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(400)",
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "AddOns",
                type: "char(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationAddOns",
                table: "ReservationAddOns",
                columns: new[] { "ReservationId", "AddOnId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationAddOns_AddOns_AddOnId",
                table: "ReservationAddOns",
                column: "AddOnId",
                principalTable: "AddOns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
