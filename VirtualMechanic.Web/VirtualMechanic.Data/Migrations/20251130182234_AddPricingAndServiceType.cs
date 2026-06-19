using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualMechanic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndServiceType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_Users_UserId",
                table: "ServiceRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "AppUsers");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceCost",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "ServiceRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelCost",
                table: "ServiceRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Mechanics",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Mechanics",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppUsers",
                table: "AppUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_AppUsers_UserId",
                table: "ServiceRequests",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceRequests_AppUsers_UserId",
                table: "ServiceRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppUsers",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "ServiceCost",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "TravelCost",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Mechanics");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Mechanics");

            migrationBuilder.RenameTable(
                name: "AppUsers",
                newName: "Users");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceRequests_Users_UserId",
                table: "ServiceRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
