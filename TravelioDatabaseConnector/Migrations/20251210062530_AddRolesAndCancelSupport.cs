using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelioDatabaseConnector.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndCancelSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activa",
                table: "Reservas",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionAgencia",
                table: "Reservas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPagadoNegocio",
                table: "Reservas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CancelarReservaEndpoint",
                table: "DetallesServicio",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Clientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Cliente");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 1,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 2,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 3,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 4,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 5,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 101,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 102,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 103,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 104,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 105,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 106,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 201,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 202,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 203,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 204,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 205,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 206,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 301,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 302,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 303,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 401,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 402,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 403,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 404,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 405,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 406,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 407,
                column: "CancelarReservaEndpoint",
                value: null);

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 501,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 502,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 503,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 504,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 505,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 601,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 602,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 603,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 604,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 605,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 606,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 701,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 702,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 703,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

            migrationBuilder.UpdateData(
                table: "DetallesServicio",
                keyColumn: "Id",
                keyValue: 704,
                column: "CancelarReservaEndpoint",
                value: "/cancel");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activa",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ComisionAgencia",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "ValorPagadoNegocio",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "CancelarReservaEndpoint",
                table: "DetallesServicio");

            migrationBuilder.DropColumn(
                name: "Rol",
                table: "Clientes");
        }
    }
}
