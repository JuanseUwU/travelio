using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelioDatabaseConnector.Migrations
{
    /// <inheritdoc />
    public partial class MicroservicesUpdate2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 602,
                column: "UriBase",
                value: "https://apigateway-zebw.onrender.com/api/integracion");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 604,
                column: "UriBase",
                value: "http://216.173.77.147:8080/api/integracion");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 605,
                column: "UriBase",
                value: "http://23.230.3.250:8080/api/integracion");

            migrationBuilder.UpdateData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 5,
                column: "Activo",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 602,
                column: "UriBase",
                value: "https://apigateway-hyaw.onrender.com/api/integracion");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 604,
                column: "UriBase",
                value: "http://restbrisamar.runasp.net/api/v1/hoteles");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 605,
                column: "UriBase",
                value: "http://restallpahousenyc.runasp.net/api/v1/hoteles");

            migrationBuilder.UpdateData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 5,
                column: "Activo",
                value: true);
        }
    }
}
