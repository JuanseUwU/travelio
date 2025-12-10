using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelioDatabaseConnector.Migrations
{
    /// <inheritdoc />
    public partial class RestMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 701,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/CancelarAuto", "/integracion/autos/availability", "/integracion/autos/hold", "/integracion/autos/book", "/integracion/autos/invoices", "/integracion/autos/search", "/integracion/autos/reserva", "/integracion/autos/usuarios/externo", "http://cuencautosinte.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 702,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/CancelarAuto", "/integracion/autos/availability", "/integracion/autos/hold", "/integracion/autos/book", "/integracion/autos/invoices", "/integracion/autos/search", "/integracion/autos/reserva", "/integracion/autos/usuarios/externo", "http://restintegracin.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 703,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/CancelarAuto", "/integracion/autos/availability", "/integracion/autos/hold", "/integracion/autos/book", "/integracion/autos/invoices", "/integracion/autos/search", "/integracion/autos/reserva", "/integracion/autos/usuarios/externo", "http://integracionrest.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 704,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/CancelarAuto", "/integracion/autos/availability", "/integracion/autos/hold", "/integracion/autos/book", "/integracion/autos/invoices", "/integracion/autos/search", "/integracion/autos/reserva", "/integracion/autos/usuarios/externo", "http://autocarent.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 705,
                column: "CancelarReservaEndpoint",
                value: "v1/CancelarAuto");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 706,
                column: "CancelarReservaEndpoint",
                value: "/CancelarAuto");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 801,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 802,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 803,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 901,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 902,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 903,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 904,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 905,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 906,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 907,
                column: "CancelarReservaEndpoint",
                value: "/cancelar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 701,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://cuencautosinte.runasp.net/api/v1/integracion/autos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 702,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://restintegracin.runasp.net/api/v1/integracion/autos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 703,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://integracionrest.runasp.net/api/v1/integracion/autos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 704,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://autocarent.runasp.net/api/v1/integracion/autos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 705,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 706,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 801,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 802,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 803,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 901,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 902,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 903,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 904,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 905,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 906,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 907,
                column: "CancelarReservaEndpoint",
                value: "/cancel");
        }
    }
}
