using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Orders.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaCompensation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "order_states",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemsJson",
                table: "order_states",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsRefund",
                table: "order_states",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "order_states");

            migrationBuilder.DropColumn(
                name: "ItemsJson",
                table: "order_states");

            migrationBuilder.DropColumn(
                name: "NeedsRefund",
                table: "order_states");
        }
    }
}
