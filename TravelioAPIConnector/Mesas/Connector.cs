using System;
using System.ServiceModel;
using TravelioREST.Mesas;
using TravelioSOAP.Mesas.Busqueda;
using TravelioSOAP.Mesas.Disponibilidad;
using TravelioSOAP.Mesas.Factura;
using TravelioSOAP.Mesas.Reserva;
using TravelioSOAP.Mesas.Usuario;
using static TravelioAPIConnector.Global;

namespace TravelioAPIConnector.Mesas;

public static class Connector
{
    public static async Task<Mesa[]> BuscarMesasAsync(string uri, int? capacidad = null, string? tipoMesa = null, string? estado = null, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var mesasRest = await MesasList.GetMesasListAsync(uri, capacidad, tipoMesa, estado);
            return Array.ConvertAll(mesasRest, m => new Mesa(
                m.IdMesa,
                0,
                m.NumeroMesa,
                m.TipoMesa ?? string.Empty,
                m.Capacidad,
                m.Precio,
                m.ImagenURL ?? string.Empty,
                m.Estado ?? string.Empty));
        }

        var soapClient = new BusBusquedaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.BuscarMesasAsync(capacidad, tipoMesa, estado);
        var mesas = response?.BuscarMesasResponse?.BuscarMesasResult?.Mesas ?? [];

        return Array.ConvertAll(mesas, static m => new Mesa(
            m.IdMesa,
            m.IdRestaurante,
            m.NumeroMesa,
            m.TipoMesa ?? string.Empty,
            m.Capacidad,
            m.Precio,
            m.ImagenURL ?? string.Empty,
            m.Estado ?? string.Empty));
    }

    public static async Task<bool> ValidarDisponibilidadAsync(string uri, int idMesa, DateTime fecha, int numeroPersonas, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            return await VerificarDisponibilidadMesas.VerificarAsync(uri, idMesa, numeroPersonas);
        }

        var soapClient = new BusDisponibilidadWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.ValidarDisponibilidadMesaAsync(idMesa, fecha.ToString(), numeroPersonas);
        return response?.Body.ValidarDisponibilidadMesaResponse?.ValidarDisponibilidadResult?.Disponible ?? false;
    }

    public static async Task<(string holdId, DateTime expira)> CrearPreReservaAsync(
        string uri,
        int idMesa,
        DateTime fecha,
        int personas,
        int duracionHoldSegundos = 300,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var hold = await MesaHold.CrearMesaHoldAsync(uri, idMesa.ToString(), fecha, personas, duracionHoldSegundos);
            return (hold.id_hold, DateTime.UtcNow.AddSeconds(duracionHoldSegundos));
        }

        var soapClient = new BusReservaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.CrearPreReservaAsync(idMesa.ToString(), fecha.ToString(), personas, duracionHoldSegundos);
        var pre = response?.CrearPreReservaResult ?? throw new InvalidOperationException("No se pudo crear la prerreserva.");
        return (pre.IdHold ?? string.Empty, DateTime.TryParse(pre.FechaReserva, out var parsed) ? parsed : DateTime.MinValue);
    }

    public static async Task<int> CrearUsuarioAsync(string uri, string nombre, string apellido, string email, string tipoIdentificacion, string identificacion, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var cliente = await RegistroClienteMesas.CrearClienteAsync(uri, nombre, apellido, email, identificacion, tipoIdentificacion);
            return cliente.id;
        }

        var soapClient = new BusUsuarioWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.CrearUsuarioAsync(nombre, apellido, email, tipoIdentificacion, identificacion);
        return response?.CrearUsuarioResult?.IdUsuario ?? throw new InvalidOperationException("No se pudo crear el usuario.");
    }

    public static async Task<Reserva> ConfirmarReservaAsync(
        string uri,
        int idMesa,
        string holdId,
        string nombre,
        string apellido,
        string correo,
        string tipoIdentificacion,
        string identificacion,
        DateTime fecha,
        int personas,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var reserva = await MesaReserva.CrearMesaReservaAsync(uri, idMesa.ToString(), holdId, nombre, apellido, correo, identificacion, fecha, personas, tipoIdentificacion);
            return new Reserva(
                reserva.mensaje ?? string.Empty,
                reserva.idReserva ?? string.Empty,
                idMesa,
                fecha,
                personas,
                string.Empty,
                string.Empty,
                nombre,
                apellido,
                correo,
                reserva.valor_pagado,
                reserva.uri_factura ?? string.Empty);
        }

        var soapClient = new BusReservaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.ConfirmarReservaAsync(
            idMesa.ToString(),
            holdId,
            nombre,
            apellido,
            correo,
            tipoIdentificacion,
            identificacion,
            fecha.ToString(),
            personas);

        var reservaSoap = response?.ConfirmarReservaResult ?? throw new InvalidOperationException("No se pudo confirmar la reserva.");

        return new Reserva(
            reservaSoap.Mensaje ?? string.Empty,
            reservaSoap.IdReserva ?? string.Empty,
            reservaSoap.IdMesa,
            DateTime.TryParse(reservaSoap.FechaReserva, out var fechaReserva) ? fechaReserva : DateTime.MinValue,
            reservaSoap.NumeroPersonas,
            string.Empty,
            string.Empty,
            reservaSoap.NombreCliente ?? string.Empty,
            reservaSoap.ApellidoCliente ?? string.Empty,
            reservaSoap.Correo ?? string.Empty,
            reservaSoap.ValorPagado,
            reservaSoap.UriFactura ?? string.Empty);
    }

    public static async Task<Reserva> BuscarReservaAsync(string uri, int idReserva, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var datos = await MesaReservaConsulta.ConsultarMesaReservaAsync(uri, idReserva.ToString());
            return new Reserva(
                string.Empty,
                idReserva.ToString(),
                int.TryParse(datos.numero_mesa, out var idMesa) ? idMesa : 0,
                datos.fecha,
                datos.numero_personas,
                string.Empty,
                datos.tipo ?? string.Empty,
                datos.nombre ?? string.Empty,
                datos.apellido ?? string.Empty,
                datos.correo ?? string.Empty,
                datos.valor_pagado,
                datos.uri_factura ?? string.Empty);
        }

        var soapClient = new BusReservaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var response = await soapClient.BuscarReservaAsync(idReserva.ToString());
        var datosSoap = response?.BuscarReservaResult ?? throw new InvalidOperationException("No se pudieron obtener los datos de la reserva.");

        return new Reserva(
            datosSoap.Mensaje ?? string.Empty,
            datosSoap.IdReserva.ToString(),
            datosSoap.IdMesa,
            datosSoap.Fecha,
            datosSoap.NumeroPersonas,
            datosSoap.Estado ?? string.Empty,
            datosSoap.TipoMesa ?? string.Empty,
            datosSoap.NombreCliente ?? string.Empty,
            datosSoap.ApellidoCliente ?? string.Empty,
            datosSoap.Correo ?? string.Empty,
            datosSoap.ValorPagado,
            datosSoap.UriFactura ?? string.Empty);
    }

    public static async Task<string> GenerarFacturaAsync(
        string uri,
        string idReserva,
        string correo,
        string nombre,
        string tipoIdentificacion,
        string identificacion,
        decimal valor,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var factura = await MesaFactura.CrearMesaFacturaAsync(uri, idReserva, correo, nombre, identificacion, valor, tipoIdentificacion);
            return factura.uri_factura ?? throw new InvalidOperationException("No se pudo generar la factura.");
        }

        var soapClient = new BusFacturaWSSoapClient(GetBinding(uri), new EndpointAddress(uri));
        var facturaSoap = await soapClient.GenerarFacturaBusAsync(idReserva, correo, nombre, tipoIdentificacion, identificacion, valor);
        return facturaSoap?.uri_factura ?? throw new InvalidOperationException("No se pudo generar la factura.");
    }

    public static async Task<(bool exito, decimal valorPagado)> CancelarReservaAsync(string uri, string idReserva)
    {
        var cancelar = await MesaCancelarReserva.CancelarMesaReservaAsync(uri, idReserva);
        return (cancelar.exito, cancelar.valor_pagado);
    }
}
