using System;
using System.ServiceModel;
using TravelioREST.Habitaciones;
using TravelioSOAP.Habitaciones.BuscarDatosReserva;
using TravelioSOAP.Habitaciones.BuscarHabitaciones;
using TravelioSOAP.Habitaciones.CrearPrerreserva;
using TravelioSOAP.Habitaciones.CrearUsuarioExterno;
using TravelioSOAP.Habitaciones.EmitirFacturaHotel;
using TravelioSOAP.Habitaciones.ReservarHabitacion;
using TravelioSOAP.Habitaciones.ValidarDisponibilidad;
using static TravelioAPIConnector.Global;

namespace TravelioAPIConnector.Habitaciones;

#pragma warning disable CS0162
public static class Connector
{
    public static async Task<Habitacion[]> BuscarHabitacionesAsync(
        string uri,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? tipoHabitacion = null,
        int? capacidad = null,
        decimal? precioMin = null,
        decimal? precioMax = null,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var habitacionesRest = await HabitacionesGetter.GetHabitacionesAsync(uri, fechaInicio, fechaFin, tipoHabitacion, capacidad, precioMin, precioMax);
            return Array.ConvertAll(habitacionesRest, h =>
                new Habitacion(
                    h.idHabitacion ?? string.Empty,
                    h.nombreHabitacion ?? string.Empty,
                    h.tipoHabitacion ?? string.Empty,
                    h.nombreHotel ?? string.Empty,
                    h.nombreCiudad ?? string.Empty,
                    h.nombrePais ?? string.Empty,
                    h.capacidad,
                    h.precioNormal,
                    h.precioActual,
                    h.precioVigente,
                    h.amenidades ?? string.Empty,
                    (h.imagenes ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries)));
        }

        var soapClient = new BuscarHabitacionesWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.buscarHabitacionesAsync(fechaInicio, fechaFin, tipoHabitacion, capacidad, precioMin, precioMax);
        var habitacionesDto = response.Body.buscarHabitacionesResult ?? [];

        return Array.ConvertAll(habitacionesDto, MapHabitacion);
    }

    public static async Task<bool> ValidarDisponibilidadAsync(string uri, string idHabitacion, DateTime fechaInicio, DateTime fechaFin, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var disponibilidad = await RoomCheckAvailable.CheckAvailabilityAsync(uri, idHabitacion, fechaInicio, fechaFin);
            return disponibilidad.disponible;
        }

        var soapClient = new ValidarDisponibilidadWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.validarDisponibilidadHabitacionAsync(idHabitacion, fechaInicio, fechaFin);
        return response.Body.validarDisponibilidadHabitacionResult.Disponible;
    }

    public static async Task<string> CrearPrerreservaAsync(
        string uri,
        string idHabitacion,
        DateTime fechaInicio,
        DateTime fechaFin,
        int numeroHuespedes,
        int? duracionHoldSegundos = null,
        decimal? precioActual = null,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var hold = await HoldCreator.CrearHoldAsync(uri, idHabitacion, fechaInicio, fechaFin, numeroHuespedes, duracionHoldSegundos, precioActual);
            return hold.idHold;
        }

        var soapClient = new CrearPreReservaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.crearPreReservaHabitacionAsync(idHabitacion, fechaInicio, fechaFin, numeroHuespedes, duracionHoldSegundos, precioActual);
        var prerreserva = response.Body.crearPreReservaHabitacionResult;
        return prerreserva?.IdHold ?? throw new InvalidOperationException("No se pudo crear la prerreserva de habitacion.");
    }

    public static async Task<int> CrearUsuarioExternoAsync(string uri, string correo, string nombre, string apellido, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var cliente = await ClienteExternoCreator.CrearClienteExternoAsync(uri, correo, nombre, apellido);
            return cliente.id;
        }

        var soapClient = new CrearUsuarioExternoWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.crearUsuarioExternoAsync(correo, nombre, apellido);
        return response.Body.crearUsuarioExternoResult?.Id ?? throw new InvalidOperationException("No se pudo crear el usuario externo.");
    }

    public static async Task<int> CrearReservaAsync(
        string uri,
        string idHabitacion,
        string idHold,
        string nombre,
        string apellido,
        string correo,
        string tipoDocumento,
        string documento,
        DateTime fechaInicio,
        DateTime fechaFin,
        int numeroHuespedes,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var reserva = await BookRoom.ReservarHabitacionAsync(uri, idHabitacion, idHold, nombre, apellido, correo, tipoDocumento, documento, fechaInicio, fechaFin, numeroHuespedes);
            return reserva.idReserva;
        }

        var soapClient = new ReservarHabitacionWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.reservarHabitacionAsync(idHabitacion, idHold, nombre, apellido, correo, tipoDocumento, documento, fechaInicio, fechaFin, numeroHuespedes);
        return response.Body.reservarHabitacionResult?.IdReserva ?? throw new InvalidOperationException("No se pudo crear la reserva de habitacion.");
    }

    public static async Task<string> EmitirFacturaAsync(
        string uri,
        int idReserva,
        string nombre,
        string apellido,
        string tipoDocumento,
        string documento,
        string correo,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var invoice = await InvoiceGenerator.GenerarFacturaAsync(uri, idReserva, nombre, apellido, tipoDocumento, documento, correo);
            return invoice.urlPdf ?? throw new InvalidOperationException("No se pudo emitir la factura.");
        }

        var soapClient = new EmitirFacturaHotelWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.emitirFacturaHotelAsync(idReserva, nombre, apellido, tipoDocumento, documento, correo);
        return response.Body.emitirFacturaHotelResult?.UrlPdf ?? throw new InvalidOperationException("No se pudo emitir la factura.");
    }

    public static async Task<Reserva> ObtenerDatosReservaAsync(string uri, int idReserva, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var datos = await BookedRoomInfo.GetBookedRoomInfoAsync(uri, idReserva);
            return new Reserva(
                datos.idReserva,
                datos.costoTotal,
                datos.fechaRegistro,
                datos.inicio,
                datos.fin,
                datos.estadoGeneral ?? string.Empty,
                datos.nombre ?? string.Empty,
                datos.apellido ?? string.Empty,
                datos.correo ?? string.Empty,
                datos.idHabitacion ?? string.Empty,
                datos.nombreHabitacion ?? string.Empty,
                datos.tipoHabitacion ?? string.Empty,
                datos.hotel ?? string.Empty,
                datos.ciudad ?? string.Empty,
                datos.pais ?? string.Empty,
                datos.capacidadReserva,
                datos.costoCalculado,
                datos.descuento,
                datos.impuestos,
                datos.idHold ?? string.Empty,
                datos.amenidades ?? string.Empty,
                datos.imagenes?.Split('|') ?? Array.Empty<string>(),
                datos.urlPdf ?? string.Empty);
        }
        else
        {
            var soapClient = new BuscarDatosReservaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var response = await soapClient.buscarDatosReservaAsync(idReserva);
            var datos = response.Body.buscarDatosReservaResult ?? throw new InvalidOperationException("No se pudieron obtener los datos de la reserva.");

            return new Reserva(
                datos.IdReserva,
                datos.CostoTotal ?? 0m,
                datos.FechaRegistro ?? DateTime.MinValue,
                datos.Inicio ?? DateTime.MinValue,
                datos.Fin ?? DateTime.MinValue,
                datos.EstadoGeneral ?? string.Empty,
                datos.Nombre ?? string.Empty,
                datos.Apellido ?? string.Empty,
                datos.Correo ?? string.Empty,
                datos.IdHabitacion ?? string.Empty,
                datos.NombreHabitacion ?? string.Empty,
                datos.TipoHabitacion ?? string.Empty,
                datos.Hotel ?? string.Empty,
                datos.Ciudad ?? string.Empty,
                datos.Pais ?? string.Empty,
                datos.CapacidadReserva ?? 0,
                datos.CostoCalculado ?? 0m,
                datos.Descuento ?? 0m,
                datos.Impuestos ?? 0m,
                datos.IdHold ?? string.Empty,
                datos.Amenidades ?? string.Empty,
                datos.Imagenes?.Split('|') ?? [],
                datos.UrlPdf ?? string.Empty);
        }
    }

    public static async Task<(bool exito, decimal montoDevuelto)> CancelarReservaAsync(string uri, string idReserva, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            if (!int.TryParse(idReserva, out var reservaId))
            {
                throw new FormatException($"El id de reserva {idReserva} no es un entero v\u00e1lido para la API REST de habitaciones.");
            }

            var resultado = await CancelReservation.CancelarReservaAsync(uri, reservaId);
            return (resultado.success, resultado.montoPagado);
        }

        throw new NotSupportedException("La cancelaci\u00f3n SOAP para habitaciones no est\u00e1 soportada.");
    }

    private static Habitacion MapHabitacion(HabitacionListItemDto dto)
    {
        var precioActual = dto.PrecioActual ?? dto.PrecioVigente ?? dto.PrecioNormal ?? 0m;
        var precioVigente = dto.PrecioVigente ?? dto.PrecioActual ?? dto.PrecioNormal ?? 0m;

        return new Habitacion(
            dto.IdHabitacion ?? string.Empty,
            dto.NombreHabitacion ?? string.Empty,
            dto.TipoHabitacion ?? string.Empty,
            dto.NombreHotel ?? string.Empty,
            dto.NombreCiudad ?? string.Empty,
            dto.NombrePais ?? string.Empty,
            dto.Capacidad ?? 0,
            dto.PrecioNormal ?? 0m,
            precioActual,
            precioVigente,
            dto.Amenidades ?? string.Empty,
            dto.Imagenes?.Split('|') ?? []);
    }
}
#pragma warning restore CS0162
