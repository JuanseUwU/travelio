using System;
using System.Linq;
using System.ServiceModel;
using TravelioREST.Aerolinea;
using TravelioSOAP.Aerolinea;
using static TravelioAPIConnector.Global;

namespace TravelioAPIConnector.Aerolinea;

public static class Connector
{
    public static async Task<Vuelo[]> GetVuelosAsync(
        string uri,
        string? origen = null,
        string? destino = null,
        DateTime? fechaDespegue = null,
        DateTime? fechaLlegada = null,
        string? tipoCabina = null,
        int? pasajeros = null,
        decimal? precioMin = null,
        decimal? precioMax = null,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var vuelosRest = await VuelosGetter.GetVuelosAsync(uri, origen, destino, fechaDespegue, fechaLlegada, tipoCabina, pasajeros);

            return Array.ConvertAll(vuelosRest, v => new Vuelo(
                v.IdVuelo.ToString(),
                v.Origen ?? string.Empty,
                v.Destino ?? string.Empty,
                v.FechaSalida,
                v.TipoCabina ?? string.Empty,
                v.NombreAerolinea ?? string.Empty,
                v.Pasajeros,
                v.CapacidadDisponible,
                v.PrecioNormal,
                v.PrecioActual,
                v.PrecioNormal == 0 ? 0 : (1 - (v.PrecioActual / v.PrecioNormal)) * 100m));
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await client.buscarVuelosAsync(origen, destino, fechaDespegue, fechaLlegada, tipoCabina, pasajeros, precioMin, precioMax);
        var vuelos = response?.buscarVuelosResult ?? [];

        return Array.ConvertAll(vuelos, static v => new Vuelo(
            v.IdVuelo ?? string.Empty,
            v.Origen ?? string.Empty,
            v.Destino ?? string.Empty,
            v.Fecha,
            v.TipoCabina ?? string.Empty,
            v.NombreAerolinea ?? string.Empty,
            v.CapacidadPasajeros,
            v.CapacidadActual,
            v.PrecioNormal,
            v.PrecioActual,
            v.PrecioNormal == 0 ? 0 : (1 - (v.PrecioActual / v.PrecioNormal)) * 100m));
    }

    public static async Task<bool> VerificarDisponibilidadVueloAsync(string uri, string idVuelo, int pasajeros, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            if (!int.TryParse(idVuelo, out var vueloId))
            {
                throw new FormatException($"El id de vuelo {idVuelo} no es un entero v\u00e1lido para la API REST.");
            }

            return await VueloCheckAvailable.GetDisponibilidadAsync(uri, vueloId, pasajeros);
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        return await client.validarDisponibilidadVueloAsync(idVuelo, pasajeros);
    }

    public static async Task<(string holdId, DateTime expira)> CrearPrerreservaVueloAsync(
        string uri,
        string idVuelo,
        (string nombre, string apellido, string tipoIdentificacion, string identificacion, DateTime fechaNacimiento)[] pasajeros,
        int duracionHold = 300,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            if (!int.TryParse(idVuelo, out var vueloId))
            {
                throw new FormatException($"El id de vuelo {idVuelo} no es un entero v\u00e1lido para la API REST.");
            }

            var hold = await HoldCreator.CreateHoldAsync(uri, vueloId, pasajeros.Length, duracionHold);
            var expira = DateTime.TryParse(hold.expiraEn, out var parsedExpira)
                ? parsedExpira
                : DateTime.UtcNow.AddSeconds(duracionHold);
            return (hold.holdId, expira);
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var pasajerosDto = Array.ConvertAll(pasajeros, p => new PasajeroDTO_Integracion
        {
            Nombre = p.nombre,
            Apellido = p.apellido,
            TipoIdentificacion = p.tipoIdentificacion,
            Identificacion = p.identificacion,
            FechaNacimiento = p.fechaNacimiento
        });

        var response = await client.crearPreReservaVueloDetalleAsync(idVuelo, pasajerosDto, duracionHold);
        var pre = response ?? throw new InvalidOperationException("No se pudo crear la prerreserva.");
        return (pre.IdHold ?? string.Empty, pre.ExpiresAt);
    }

    public static async Task<(string idReserva, string codigoReserva, string mensaje)> CrearReservaAsync(
        string uri,
        string idVuelo,
        string idHold,
        string correo,
        (string nombre, string apellido, string tipoIdentificacion, string identificacion, DateTime fechaNacimiento)[] pasajeros,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            if (!int.TryParse(idVuelo, out var vueloId))
            {
                throw new FormatException($"El id de vuelo {idVuelo} no es un entero v\u00e1lido para la API REST.");
            }

            var pasajerosRest = pasajeros.Select(p => (p.nombre, p.apellido, p.tipoIdentificacion, p.identificacion)).ToArray();
            var reservaRest = await ReservationCreator.CreateReservationAsync(uri, vueloId, idHold, correo, pasajerosRest);
            return (reservaRest.IdReserva.ToString(), reservaRest.CodigoReserva, reservaRest.Estado);
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));

        var pasajerosDto = Array.ConvertAll(pasajeros, static p => new PasajeroDTO_Integracion
        {
            Nombre = p.nombre,
            Apellido = p.apellido,
            TipoIdentificacion = p.tipoIdentificacion,
            Identificacion = p.identificacion,
            FechaNacimiento = p.fechaNacimiento
        });

        var response = await client.reservarVueloDetalleAsync(idVuelo, idHold, pasajerosDto, correo);
        var reserva = response ?? throw new InvalidOperationException("No se pudo crear la reserva.");
        return (reserva.IdReserva ?? string.Empty, reserva.CodigoReserva ?? string.Empty, reserva.Message ?? string.Empty);
    }

    public static async Task<string> GenerarFacturaAsync(
        string uri,
        string idReserva,
        decimal subtotal,
        decimal iva,
        decimal total,
        (string nombre, string tipoDocumento, string documento, string correo) cliente,
        string idTransaccionBanco = "",
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            if (!int.TryParse(idReserva, out var reservaId))
            {
                throw new FormatException($"El id de reserva {idReserva} no es un entero v\u00e1lido para la API REST.");
            }

            var factura = await InvoiceGenerator.GenerateInvoiceAsync(uri, reservaId, subtotal, iva, total, (cliente.nombre, cliente.documento, cliente.correo));
            return factura.UriFactura ?? throw new InvalidOperationException("La API REST de aerol\u00edneas no devolvi\u00f3 el enlace de la factura.");
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var dtoCliente = new ClienteFacturaDTO
        {
            Nombre = cliente.nombre,
            TipoDocumento = cliente.tipoDocumento,
            Documento = cliente.documento,
            Correo = cliente.correo
        };

        var response = await client.emitirFacturaVueloDetalleAsync(idReserva, subtotal, iva, total, dtoCliente, idTransaccionBanco);
        return response?.UriFactura ?? throw new InvalidOperationException("No se pudo emitir la factura.");
    }

    public static async Task<string> CrearClienteExternoAsync(
        string uri,
        string correo,
        string nombre,
        string apellido,
        DateTime fechaNacimiento,
        string tipoIdentificacion,
        string identificacion,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var restClient = await ExternalClientCreator.CreateExternalClientAsync(uri, nombre, apellido, correo);
            return restClient.IdUsuario.ToString();
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await client.crearUsuarioExternoDetalleAsync(correo, nombre, apellido, fechaNacimiento, tipoIdentificacion, identificacion);
        return response?.IdUsuario ?? throw new InvalidOperationException("No se pudo crear el usuario externo.");
    }

    public static async Task<Reserva> GetDatosReservaAsync(string uri, string idReserva, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            if (!int.TryParse(idReserva, out var reservaId))
            {
                throw new FormatException($"El id de reserva {idReserva} no es un entero v\u00e1lido para la API REST.");
            }

            var datos = await ReservationDataGetter.GetReservationDataAsync(uri, reservaId);
            var pasajeros = datos.Pasajeros ?? Array.Empty<PasajeroReservaInfo>();
            return new Reserva(
                datos.IdReserva.ToString(),
                datos.Origen ?? string.Empty,
                datos.Destino ?? string.Empty,
                datos._links?.self ?? string.Empty,
                datos.Fecha,
                datos.TipoCabina ?? string.Empty,
                Array.ConvertAll(pasajeros, p => (p.NombreCompleto ?? string.Empty, string.Empty, string.Empty, p.Documento ?? string.Empty)),
                datos.Aerolinea ?? string.Empty,
                datos.AsientosReservados,
                0m,
                datos._links?.factura ?? string.Empty,
                string.Empty);
        }

        var client = new IntegracionServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var datosSoap = await client.buscarDatosReservaAsync(idReserva) ?? throw new InvalidOperationException("No se pudo obtener la reserva.");

        var pasajerosSoap = datosSoap.Pasajeros ?? Array.Empty<PasajeroDTO_Integracion>();

        return new Reserva(
            datosSoap.IdReserva ?? string.Empty,
            datosSoap.Origen ?? string.Empty,
            datosSoap.Destino ?? string.Empty,
            datosSoap.Correo ?? string.Empty,
            datosSoap.Fecha,
            datosSoap.TipoCabina ?? string.Empty,
            Array.ConvertAll(pasajerosSoap, p => (p.Nombre, p.Apellido, p.TipoIdentificacion, p.Identificacion)),
            datosSoap.NombreAerolinea ?? string.Empty,
            datosSoap.AsientosReservados,
            datosSoap.ValorPagado,
            datosSoap.UriFactura ?? string.Empty,
            datosSoap.Estado ?? string.Empty);
    }

    public static async Task<(bool exito, decimal valorPagado)> CancelarReservaAsync(string uri, string idReserva)
    {
        if (!int.TryParse(idReserva, out var reservaId))
        {
            throw new FormatException($"El id de reserva {idReserva} no es un entero v\u00e1lido para la API REST.");
        }

        var resultado = await CancelarReservaVuelos.CancelarReservaAsync(uri, reservaId);
        return (resultado.Success, resultado.TotalPagado);
    }
}
