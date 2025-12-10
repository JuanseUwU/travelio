using System;
using System.Collections.Generic;
using System.Text;
using TravelioAPIConnector;
using TravelioAPIConnector.Autos;

namespace TravelioTestConsoleApp.Autos;

internal static class ConnectionTest
{
    public static async Task TestBasicGetConnection()
    {
        Console.WriteLine($"Probando {(Global.IsREST ? "REST" : "SOAP")}");
        const string restGetCarsUri = @"http://cuencautosinte.runasp.net/api/v2/integracion/autos/search";
        const string soapGetCarsUri = @"http://cuencautosrenta.runasp.net/WS_BuscarAutos.asmx?WSDL";
        var getCarsUri = Global.IsREST ? restGetCarsUri : soapGetCarsUri;
        //var autos = await Connector.GetVehiculosAsync(getCarsUri, categoria: "SUV", capacidad: 5, precioMax: 100);
        var autos = await Connector.GetVehiculosAsync(getCarsUri);
        if (autos.Length == 0)
        {
            Console.WriteLine("No se encontraron vehículos.");
            return;
        }
        foreach (var a in autos) Console.WriteLine(a);

        Console.WriteLine("Comprobación de disponibilidad:");

        const string restCheckAvailabilityUri = @"http://cuencautosinte.runasp.net/api/v2/integracion/autos/availability";
        const string soapCheckAvailabilityUri = @"http://cuencautosrenta.runasp.net/WS_DisponibilidadAutos.asmx?WSDL";

        var checkAvailabilityUri = Global.IsREST ? restCheckAvailabilityUri : soapCheckAvailabilityUri;

        var firstCarId = autos[^3].IdAuto;

        var isAvailable = await Connector.VerificarDisponibilidadAutoAsync(checkAvailabilityUri, firstCarId, DateTime.Now.AddDays(14).AddYears(1), DateTime.Now.AddDays(19).AddYears(1));
        Console.WriteLine($"El vehículo con Id '{firstCarId}' {(isAvailable ? "está" : "no está")} disponible.");
        var invalidIsAvailable = await Connector.VerificarDisponibilidadAutoAsync(checkAvailabilityUri, "32312", DateTime.Now.AddDays(1).AddYears(1), DateTime.Now.AddDays(5).AddYears(1));
        Console.WriteLine($"El vehículo con Id '32312' {(invalidIsAvailable ? "está" : "no está")} disponible.");

        if (!isAvailable)
            return;

        Console.WriteLine("Creando prerreserva:");

        const string restCreateHoldUri = @"http://cuencautosinte.runasp.net/api/v2/prereserva/auto";
        const string soapCreateHoldUri = @"http://cuencautosrenta.runasp.net/WS_PreReserva.asmx?WSDL";

        var createHoldUri = Global.IsREST ? restCreateHoldUri : soapCreateHoldUri;

        var (holdId, holdExpiration) = await Connector.CrearPrerreservaAsync(createHoldUri, firstCarId, DateTime.Now.AddDays(14).AddYears(1), DateTime.Now.AddDays(19).AddYears(1), 180);

        Console.WriteLine($"Prerreserva creada: '{holdId}', que expira el {holdExpiration}");

        const string restCreateExternalClientUri = @"http://cuencautosinte.runasp.net/api/v1/integracion/autos/usuarios/externo";
        const string soapCreateExternalClientUri = @"http://cuencautosrenta.runasp.net/WS_UsuarioExterno.asmx?WSDL";

        var createExternalClientUri = Global.IsREST ? restCreateExternalClientUri : soapCreateExternalClientUri;

        var externalClientId = await Connector.CrearClienteExternoAsync(createExternalClientUri, "Benito", "Camelanuel", "bcamelanuel@correo.com");

        Console.WriteLine($"Cliente externo creado con Id: {externalClientId}");

        const string restCreateReservationUri = @"http://cuencautosinte.runasp.net/api/v1/integracion/autos/book";
        const string soapCreateReservationUri = @"http://cuencautosrenta.runasp.net/WS_ReservarAutos.asmx?WSDL";

        var createReservationUri = Global.IsREST ? restCreateReservationUri : soapCreateReservationUri;

        var reservationId = await Connector.CrearReservaAsync(createReservationUri, firstCarId, holdId, "Benito", "Camelanuel", "DNI", "2314567890", "bcamelanuel@correo.com", DateTime.Now.AddDays(14).AddYears(1), DateTime.Now.AddDays(19).AddYears(1));

        Console.WriteLine($"Reserva creada con Id: {reservationId}");

        const string restGenerateInvoiceUri = @"http://cuencautosinte.runasp.net/api/v1/integracion/autos/invoices";
        const string soapGenerateInvoiceUri = @"http://cuencautosrenta.runasp.net/WS_FacturaIntegracion.asmx?WSDL";

        var generateInvoiceUri = Global.IsREST ? restGenerateInvoiceUri : soapGenerateInvoiceUri;
        var invoiceUri = await Connector.GenerarFacturaAsync(generateInvoiceUri, reservationId, 200, 12, 212, (nombre: "Juan Pérez", tipoDocumento: "Cédula", documento: "1234567890", correo: "jpere@correo.com"));

        Console.WriteLine($"Factura generada. URL de la factura: {invoiceUri}");

        const string restGetReservationDataUri = @"http://cuencautosinte.runasp.net/api/v1/integracion/autos/reservas";
        const string soapGetReservationDataUri = @"http://cuencautosrenta.runasp.net/WS_BuscarDatos.asmx?WSDL";

        var getReservationDataUri = Global.IsREST ? restGetReservationDataUri : soapGetReservationDataUri;

        var reservationData = await Connector.ObtenerDatosReservaAsync(getReservationDataUri, reservationId);
        Console.WriteLine($"Datos de la reserva Id {reservationId}: {reservationData}");
    }
}
