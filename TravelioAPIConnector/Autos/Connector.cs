using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Web;
using TravelioREST.Autos;
using TravelioSOAP.Autos.Busqueda;
using TravelioSOAP.Autos.Cliente;
using TravelioSOAP.Autos.DatosReserva;
using TravelioSOAP.Autos.Disponibilidad;
using TravelioSOAP.Autos.Facturacion;
using TravelioSOAP.Autos.Prerreserva;
using TravelioSOAP.Autos.Reserva;
using static TravelioAPIConnector.Global;

namespace TravelioAPIConnector.Autos;

public static class Connector
{
    public static async Task<Vehiculo[]> GetVehiculosAsync(string uri,
        string? categoria = null,
        string? transmision = null,
        int? capacidad = null,
        decimal? precioMin = null,
        decimal? precioMax = null,
        string? sort = null,
        string? ciudad = null,
        string? pais = null,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var uriBuilder = new UriBuilder(uri);

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (!string.IsNullOrEmpty(categoria))
                query["categoria"] = categoria;

            if (!string.IsNullOrEmpty(transmision))
                query["transmision"] = transmision;

            if (capacidad is not null)
                query["capacidad"] = capacidad.ToString();

            if (precioMin is not null)
                query["precio_min"] = precioMin.ToString();

            if (precioMax is not null)
                query["precio_max"] = precioMax.ToString();

            if (!string.IsNullOrEmpty(sort))
                query["sort"] = sort;

            if (!string.IsNullOrEmpty(ciudad))
                query["ciudad"] = ciudad;

            if (!string.IsNullOrEmpty(pais))
                query["pais"] = pais;

            uriBuilder.Query = query.ToString();

            var response = await AutosGetter.GetAutosAsync(uriBuilder.ToString());

            var result = new Vehiculo[response.Data.Length];

            for (int i = 0; i < response.Data.Length; i++)
            {
                var v = response.Data[i];
                result[i] = new Vehiculo
                {
                    IdAuto = v.IdAuto,
                    Tipo = v.Tipo,
                    CapacidadPasajeros = v.Capacidad,
                    PrecioNormalPorDia = v.PrecioNormal,
                    PrecioActualPorDia = v.PrecioActual ?? v.PrecioNormal,
                    DescuentoPorcentaje = (1 - (v.PrecioActual ?? v.PrecioNormal) / v.PrecioNormal) * 100,
                    UriImagen = v.UriImagen,
                    Ciudad = v.Ciudad,
                    Pais = v.Pais
                };
            }

            return result;
        }
        else
        {
            var soapClient = new WS_BuscarAutosSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var response = await soapClient.buscarAutosAsync(categoria, transmision, capacidad, precioMin, precioMax, sort, ciudad, pais);
            var vehiculosResult = new Vehiculo[response.Body.buscarAutosResult.Length];
            for (int i = 0; i < response.Body.buscarAutosResult.Length; i++)
            {
                var v = response.Body.buscarAutosResult[i];
                vehiculosResult[i] = new Vehiculo
                {
                    IdAuto = v.IdAuto,
                    Tipo = v.Tipo,
                    CapacidadPasajeros = v.Capacidad,
                    PrecioNormalPorDia = v.PrecioNormal,
                    PrecioActualPorDia = v.PrecioActual ?? v.PrecioNormal,
                    DescuentoPorcentaje = (1 - (v.PrecioActual ?? v.PrecioNormal) / v.PrecioNormal) * 100,
                    UriImagen = v.UriImagen,
                    Ciudad = v.Ciudad,
                    Pais = v.Pais
                };
            }

            return vehiculosResult;
        }
    }

    public static async Task<bool> VerificarDisponibilidadAutoAsync(string uri, string idAuto, DateTime dateFrom, DateTime dateTo, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            return await VehicleCheckAvailable.GetDisponibilidadAsync(uri, idAuto, dateFrom, dateTo);
        }
        else
        {
            var soapClient = new WS_DisponibilidadAutosSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var response = await soapClient.validarDisponibilidadAutoAsync(idAuto, dateFrom, dateTo);
            return response.Body.validarDisponibilidadAutoResult.Disponible;
        }
    }

    public static async Task<(string holdId, DateTime holdExpiration)> CrearPrerreservaAsync(string uri,
        string idAuto,
        DateTime dateFrom,
        DateTime dateTo,
        int duracionHold = 300,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            // Modificado en V2 (¡¿POR QUÉ?!)
            var uriBuilder = new UriBuilder(uri);

            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query["idVehiculo"] = idAuto;
            query["fechaInicio"] = dateFrom.ToString("o");
            query["fechaFin"] = dateTo.ToString("o");
            query["duracionHoldSegundos"] = duracionHold.ToString();

            uriBuilder.Query = query.ToString();

            var holdResponse = await HoldCreator.CrearPrerreservaAsync(uriBuilder.ToString(), idAuto, dateFrom, dateTo, duracionHold);
            //var holdResponse = await HoldCreator.CrearPrerreservaAsync(uri, idAuto, dateFrom, dateTo, duracionHold);
            //return (holdResponse.datos.IdHold, holdResponse.datos.FechaExpiracion);
            return (holdResponse.id_hold.ToString(), holdResponse.expiracion);
        }
        else
        {
            var soapClient = new WS_PreReservaSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var dto = new PreReservaAutoRequestDto()
            {
                IdVehiculo = idAuto,
                FechaInicio = dateFrom,
                FechaFin = dateTo,
                DuracionHoldSegundos = duracionHold
            };

            var response = await soapClient.CrearPreReservaAutoAsync(dto);
            return (response.IdHold, response.FechaExpiracion);
        }
    }

    public static async Task<int> CrearClienteExternoAsync(string uri, string nombre, string apellido, string correo, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var response = await ExternalClientCreator.CrearClienteExternoAsync(uri, nombre, apellido, correo, null, null);
            return response.IdUsuario;
        }
        else
        {
            var soapClient = new WS_UsuarioExternoSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var dto = new UsuarioExternoDto()
            {
                Nombre = nombre,
                Apellido = apellido,
                Email = correo
            };

            var response = await soapClient.ProcesarUsuarioExternoAsync(dto);

            return response.Body.ProcesarUsuarioExternoResult.IdUsuario;
        }
    }

    public static async Task<int> CrearReservaAsync(string uri,
        string idAuto,
        string idHold,
        string nombre,
        string apellido,
        string tipoIdentificacion,
        string identificacion,
        string correo,
        DateTime fechaInicio,
        DateTime fechaFin,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var response = await AutosReservaCreador.CrearReservaAsync(uri, idAuto, idHold, nombre, apellido, tipoIdentificacion, identificacion, correo, fechaInicio, fechaFin);
            return response.datos.id_reserva;
        }
        else
        {
            var soapClient = new WS_ReservarAutosSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var response = await soapClient.ReservarAutoAsync(idAuto, idHold, nombre, apellido, tipoIdentificacion, identificacion, correo, fechaInicio, fechaFin);
            return response.Body.ReservarAutoResult.IdReserva;
        }
    }

    public static async Task<string> GenerarFacturaAsync(string uri,
        int reservaId,
        decimal subtotal,
        decimal iva,
        decimal total,
        (string nombre, string tipoDocumento, string documento, string correo) cliente,
        bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            return await InvoiceGenerator.GenerarFacturaAsync(uri, reservaId, subtotal, iva, total, cliente.nombre, cliente.tipoDocumento, cliente.documento, cliente.correo);
        }
        else
        {
            var soapClient = new WS_FacturaIntegracionSoapClient(GetBinding(uri), new EndpointAddress(uri));

            var response = await soapClient.EmitirFacturaAsync(reservaId.ToString(), cliente.correo, cliente.nombre, cliente.tipoDocumento, cliente.documento, total);
            return response.Body.EmitirFacturaResult.UrlFactura;
        }
    }

    public static async Task<Reserva> ObtenerDatosReservaAsync(string uri, int reservaId, bool forceSoap = false)
    {
        if (IsREST && !forceSoap)
        {
            var datos = await AutosReservaObtenerDatos.GetReservaAsync(uri, reservaId);
            return new Reserva
            {
                NumeroMatricula = datos.numero_matricula,
                Correo = datos.correo,
                FechaInicio = datos.fecha_inicio,
                FechaFin = datos.fecha_fin,
                Categoria = datos.categoria,
                Transmision = datos.transmision,
                ValorPagado = datos.valor_pagado,
                

            };
        }
        else
        {
            var soapClient = new WS_BuscarDatosSoapClient(GetBinding(uri), new EndpointAddress(uri));
            var response = await soapClient.BuscarDatosReservaAsync(reservaId);
            var datos = response.Body.BuscarDatosReservaResult;
            return new Reserva
            {
                NumeroMatricula = datos.numero_matricula,
                Correo = datos.correo,
                FechaInicio = datos.fecha_inicio,
                FechaFin = datos.fecha_fin,
                Categoria = datos.categoria,
                Transmision = datos.transmision,
                ValorPagado = datos.valor_pagado,
                UriFactura = datos.uri_factura
            };
        }
    }

    public static async Task<(bool exito, decimal valorPagado)> CancelarReservaAsync(string uri, string idReserva)
    {
        var resultado = await CancelarReservaAutos.CancelarReservaAsync(uri, idReserva);
        return (resultado.Exito, resultado.ValorPagado);
    }
}
