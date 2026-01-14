using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelioDatabaseConnector.Migrations
{
    /// <inheritdoc />
    public partial class MicroservicesUpdate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 603,
                column: "UriBase",
                value: "https://hoteles-api-gateway-service.onrender.com/api/integracion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 603,
                column: "UriBase",
                value: "https://reca.azurewebsites.net/api/v1/hoteles");
        }
    }
}
