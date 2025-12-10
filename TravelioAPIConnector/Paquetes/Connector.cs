using System;
using System.Linq;
using System.ServiceModel;
using TravelioREST.Paquetes;
using TravelioSOAP.Paquetes;
using static TravelioAPIConnector.Global;

namespace TravelioAPIConnector.Paquetes;

public static class Connector
{
    public static async Task<Paquete[]> BuscarPaquetesAsync(
        string uri,
        string? ciudad = null,
        DateTime? fechaInicio = null,
        string? tipoActividad = null,
        decimal? precioMax = null,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var response = await PaquetesList.ObtenerPaquetesAsync(
                uri,
                ciudad,
                null,
                fechaInicio,
                null,
                tipoActividad,
                null,
                null,
                precioMax,
                null,
                null,
                null);

            var paquetes = response.datos ?? Array.Empty<DatoPaquetes>();
            return paquetes.Select(dto => new Paquete(
                dto.idPaquete ?? string.Empty,
                dto.nombre ?? string.Empty,
                dto.ciudad ?? string.Empty,
                dto.pais ?? string.Empty,
                dto.tipoActividad ?? string.Empty,
                dto.capacidad,
                dto.precioNormal,
                dto.precioActual,
                dto.imagenUrl ?? string.Empty,
                dto.duracion)).ToArray();
        }

        var soapClient = new PaquetesServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var responseSoap = await soapClient.BuscarPaquetesAsync(ciudad, fechaInicio?.ToString("yyyy-MM-dd"), tipoActividad, precioMax);
        var paquetesSoap = responseSoap?.BuscarPaquetesResult ?? [];

        return Array.ConvertAll(paquetesSoap, dto => new Paquete(
            dto.IdPaquete ?? string.Empty,
            dto.Nombre ?? string.Empty,
            dto.Ciudad ?? string.Empty,
            dto.Pais ?? string.Empty,
            dto.TipoActividad ?? string.Empty,
            dto.Capacidad ?? 0,
            dto.PrecioNormal,
            dto.PrecioActual,
            dto.ImagenUrl ?? string.Empty,
            dto.Duracion ?? 0));
    }

    public static async Task<bool> ValidarDisponibilidadAsync(string uri, string idPaquete, DateTime fechaInicio, int personas, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            return await VerificarDisponibilidadPaquetes.VerificarDisponibilidadAsync(uri, idPaquete, fechaInicio, personas);
        }

        var soapClient = new PaquetesServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        return await soapClient.ValidarDisponibilidadAsync(idPaquete, fechaInicio.ToString("yyyy-MM-dd"), personas);
    }

    public static async Task<(string holdId, DateTime expira)> CrearHoldAsync(
        string uri,
        string idPaquete,
        string bookingUserId,
        DateTime fechaInicio,
        int personas,
        int duracionSegundos = 300,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var turistasHold = Enumerable.Range(1, Math.Max(1, personas)).Select(i => new TuristaHold
            {
                nombre = $"Turista{i}",
                apellido = $"Paquete{i}",
                fechaNacimiento = DateTime.UtcNow.Date,
                tipoIdentificacion = "Documento",
                identificacion = $"{bookingUserId}-{i}"
            }).ToArray();

            var holdRequest = new HoldRequest
            {
                idPaquete = idPaquete,
                bookingUserId = bookingUserId,
                correo = $"cliente-{bookingUserId}@travelio.local",
                fechaInicio = fechaInicio,
                turistas = turistasHold,
                duracionHoldSegundos = duracionSegundos
            };

            var hold = await PreReservaPaquetes.CrearPreReservaAsync(uri, holdRequest);
            return (hold.id_hold, hold.fechaExpiracion);
        }

        var soapClient = new PaquetesServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.CrearHoldAsync(idPaquete, bookingUserId, fechaInicio.ToString("yyyy-MM-dd"), personas, duracionSegundos);
        return (response.HoldId, response.Expira);
    }

    public static async Task<string> CrearUsuarioExternoAsync(string uri, string bookingUserId, string nombre, string apellido, string correo, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var usuario = await CrearUsuarioPaquetes.CrearUsuarioAsync(uri, correo, nombre, apellido);
            return usuario.idUsuario ?? string.Empty;
        }

        var soapClient = new PaquetesServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.CrearUsuarioExternoAsync(bookingUserId, nombre, apellido, correo);
        return response?.IdUsuario ?? throw new InvalidOperationException("No se pudo crear el usuario externo.");
    }

    public static async Task<Reserva> CrearReservaAsync(
        string uri,
        string idPaquete,
        string holdId,
        string bookingUserId,
        string metodoPago,
        (string nombre, string apellido, DateTime? fechaNacimiento, string tipoIdentificacion, string identificacion)[] turistas,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var reservaRequest = new ReservaRequest
            {
                idPaquete = idPaquete,
                idHold = holdId,
                correo = $"reserva-{bookingUserId}@travelio.local",
                metodoPago = metodoPago,
                turistas = turistas.Select(t => new TuristaReserva
                {
                    nombre = t.nombre,
                    apellido = t.apellido,
                    fechaNacimiento = t.fechaNacimiento ?? DateTime.UtcNow.Date,
                    tipoIdentificacion = t.tipoIdentificacion,
                    identificacion = t.identificacion
                }).ToArray()
            };

            var reservaRest = await ReservaPaquetes.CrearReservaAsync(uri, reservaRequest);
            return new Reserva(
                reservaRest.id_reserva ?? string.Empty,
                reservaRest.id_reserva ?? string.Empty,
                0,
                0,
                0m,
                DateTime.UtcNow,
                reservaRest.payment_status ?? string.Empty);
        }

        var soapClient = new PaquetesServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));

        var turistasSoap = Array.ConvertAll(turistas, t => new TuristaSoap
        {
            Nombre = t.nombre,
            Apellido = t.apellido,
            FechaNacimiento = t.fechaNacimiento,
            TipoIdentificacion = t.tipoIdentificacion,
            Identificacion = t.identificacion
        });

        var reserva = await soapClient.ReservarPaqueteAsync(idPaquete, holdId, bookingUserId, metodoPago, turistasSoap);

        return reserva is null
            ? throw new InvalidOperationException("No se pudo crear la reserva del paquete.")
            : new Reserva(
                reserva.IdReserva ?? string.Empty,
                reserva.CodigoReserva ?? string.Empty,
                reserva.ClienteId ?? 0,
                reserva.UsuarioId ?? 0,
                reserva.Total,
                reserva.FechaCreacion,
                reserva.Estado ?? string.Empty);
    }

    public static async Task<string> EmitirFacturaAsync(string uri, string reservaId, decimal subtotal, decimal iva, decimal total, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var facturaRequest = new FacturaRequest
            {
                idReserva = reservaId,
                correo = "factura@travelio.local",
                nombre = "Cliente Travelio",
                tipoIdentificacion = "NA",
                identificacion = "NA",
                valor = total
            };

            var factura = await FacturaPaquetes.GenerarFacturaAsync(uri, facturaRequest);
            return factura.uriFactura ?? throw new InvalidOperationException("No se pudo emitir la factura del paquete (REST).");
        }

        var soapClient = new PaquetesServiceSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var facturaSoap = await soapClient.EmitirFacturaAsync(reservaId, subtotal, iva, total);
        return facturaSoap?.UriFactura ?? throw new InvalidOperationException("No se pudo emitir la factura del paquete.");
    }

    public static async Task<(bool exito, decimal valorDevuelto)> CancelarReservaAsync(string uri, string idReserva)
    {
        var cancelacion = await CancelarReservaPaquetes.CancelarReservaAsync(uri, idReserva);
        return (cancelacion.exito, cancelacion.valor_pasado);
    }
}
