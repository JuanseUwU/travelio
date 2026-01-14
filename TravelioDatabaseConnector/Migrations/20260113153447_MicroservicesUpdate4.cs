using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelioDatabaseConnector.Migrations
{
    /// <inheritdoc />
    public partial class MicroservicesUpdate4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 505,
                column: "UriBase",
                value: "http://integrationcaribbean.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 5,
                column: "Activo",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 505,
                column: "UriBase",
                value: "http://skyandes.runasp.net/api/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 5,
                column: "Activo",
                value: false);
        }
    }
}
