using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelioDatabaseConnector.Migrations
{
    /// <inheritdoc />
    public partial class MicroservicesUpdate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 501,
                column: "UriBase",
                value: "http://withflyintegration.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 502,
                column: "UriBase",
                value: "http://apintegraciona.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 503,
                column: "UriBase",
                value: "http://skaywardairintegracion.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 504,
                column: "UriBase",
                value: "https://skyandesintegration.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 601,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/disponibilidad", "/prereserva", "/reservas/confirmar", "/facturas", "/habitaciones", "/reservas", "/usuarios/externos", "https://apigateways-yefo.onrender.com/api/integracion" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 602,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/disponibilidad", "/prereserva", "/reservas/confirmar", "/facturas", "/habitaciones", "/reservas", "/usuarios/externos", "https://apigateway-hyaw.onrender.com/api/integracion" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 603,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint" },
                values: new object[] { "/reservas/cancelar", "/disponibilidad", "/prereserva", "/reservas/confirmar", "/facturas", "/habitaciones", "/reservas", "/usuarios/externos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 604,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint" },
                values: new object[] { "/reservas/cancelar", "/disponibilidad", "/prereserva", "/reservas/confirmar", "/facturas", "/habitaciones", "/reservas", "/usuarios/externos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 605,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint" },
                values: new object[] { "/reservas/cancelar", "/disponibilidad", "/prereserva", "/reservas/confirmar", "/facturas", "/habitaciones", "/reservas", "/usuarios/externos" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 606,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/disponibilidad", "/prereserva", "/reservas/confirmar", "/facturas", "/habitaciones", "/reservas", "/usuarios/externos", "https://apigateway-hyaw.onrender.com/api/integracion" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 701,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/prereserva/auto", "/integracion/autos/reservas", "https://cuencaintegracion.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 702,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/prereserva/auto", "/integracion/autos/reservas", "https://integracionbooking.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 703,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/prereserva/auto", "/integracion/autos/reservas", "http://easycarmicroint.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 704,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/prereserva/auto", "/integracion/autos/reservas", "https://apigatewaybooking.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 705,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/CancelarAuto", "/integracion/autos/availability", "/prereserva/auto", "/integracion/autos/book", "/integracion/autos/invoices", "/integracion/autos/search", "/integracion/autos/reservas", "/integracion/autos/usuarios/externo", "https://guayaquilintegracion.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 706,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/prereserva/auto", "/integracion/autos/reservas", "https://gatewaybooking.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 801,
                column: "UriBase",
                value: "https://worldagencyint.runasp.net/api/v2/paquetes");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 803,
                column: "UriBase",
                value: "http://paquetesturisticosweb.runasp.net/api/v2/paquetes");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 901,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "http://microcangrejitosfelices.runasp.net/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 902,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "http://microdragonrojo.runasp.net/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 903,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "https://apigateway-production1.up.railway.app/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 904,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "http://microsanctum.runasp.net/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 905,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "http://microsaborandino.runasp.net/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 906,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "http://microbarsinson.runasp.net/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 907,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/reservas/cancelar", "/reservas/disponibilidad", "/reservas/hold", "/reservas/confirmar", "/facturas/emitir", "/mesas/buscar", "/usuarios/registrar", "http://micro7mares.runasp.net/api" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 501,
                column: "UriBase",
                value: "http://aerolineaintrest.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 502,
                column: "UriBase",
                value: "http://astrawings.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 503,
                column: "UriBase",
                value: "http://skaywardair.runasp.net/api/v1/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 504,
                column: "UriBase",
                value: "http://skyandes.runasp.net/api/integracion/aerolinea");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 601,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://hotelcampestrerest.runasp.net/api/v1/hoteles" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 602,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://restallpahousenyc.runasp.net/api/v1/hoteles" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 603,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 604,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 605,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 606,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancel", "/availability", "/hold", "/book", "/invoices", "/search", "/reserva", "/usuarios/externo", "http://aureacuenrest.runasp.net/api/v1/hoteles" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 701,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/integracion/autos/hold", "/integracion/autos/reserva", "http://cuencautosinte.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 702,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/integracion/autos/hold", "/integracion/autos/reserva", "http://restintegracin.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 703,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/integracion/autos/hold", "/integracion/autos/reserva", "http://integracionrest.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 704,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/integracion/autos/hold", "/integracion/autos/reserva", "http://autocarent.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 705,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "ObtenerReservaEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "v1/CancelarAuto", "/v2/integracion/autos/availability", "/v1/integracion/autos/hold", "/v1/integracion/autos/book", "/v1/integracion/autos/invoices", "/v2/integracion/autos/search", "/v2/prereserva/auto", "/v1/integracion/autos/usuarios/externo", "http://restinte.runasp.net/api" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 706,
                columns: new[] { "CrearPrerreservaEndpoint", "ObtenerReservaEndpoint", "UriBase" },
                values: new object[] { "/integracion/autos/hold", "/prereserva/auto", "http://urbandriveinterest.runasp.net/api/v1" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 801,
                column: "UriBase",
                value: "https://worldagencybk.runasp.net/api/v2/paquetes");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 803,
                column: "UriBase",
                value: "http://paquetesturisticosweb-bk.runasp.net/api/v2/paquetes");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 901,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://cangrejitosfelices.runasp.net/api/v1/integracion/restaurantes" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 902,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://dragonrojobus.runasp.net/api/v1/integracion/restaurantes" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 903,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://cafesanjuan.runasp.net/api/integracion/restaurantes" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 904,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://sanctum.runasp.net/api/v1/integracion/restaurantes" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 905,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://saborandino.runasp.net/api/v1/integracion/restaurantes" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 906,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://ingtegracion-bar-sinson.runasp.net/api/v1/integracion/restaurantes" });

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 907,
                columns: new[] { "CancelarReservaEndpoint", "ConfirmarProductoEndpoint", "CrearPrerreservaEndpoint", "CrearReservaEndpoint", "GenerarFacturaEndpoint", "ObtenerProductosEndpoint", "RegistrarClienteEndpoint", "UriBase" },
                values: new object[] { "/cancelar", "/availability", "/hold", "/book", "/invoices", "/search", "/usuarios", "http://7maresback.runasp.net/api/v1/integracion/restaurantes" });
        }
    }
}
