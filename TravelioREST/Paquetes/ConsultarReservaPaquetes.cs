using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

//public class ConsultarReservaPaquetesResponse
//{
//    public string id_reserva { get; set; }
//    public string id_paquete { get; set; }
//    public string correo { get; set; }
//    public DateTime fecha_inicio { get; set; }
//    public int duracion { get; set; }
//    public string tipo_actividad { get; set; }
//    public object[] turistas { get; set; }
//    public decimal valor_pagado { get; set; }
//    public string uri_factura { get; set; }
//    public _LinksConsulta[] _links { get; set; }
//}

//public class _LinksConsulta
//{
//    public string href { get; set; }
//    public string rel { get; set; }
//    public string method { get; set; }
//}

public class ConsultarReservaPaquetesResponse
{
    public string id_reserva { get; set; }
    public DataConsultarReservaPaquetesResponse data { get; set; }
    public _LinksConsultarReservaPaquetesResponse[] _links { get; set; }
}

public class DataConsultarReservaPaquetesResponse
{
    public int id { get; set; }
    public string codigo { get; set; }
    public int usuarioId { get; set; }
    public int clienteId { get; set; }
    public ClienteConsultarReservaPaquetesResponse cliente { get; set; }
    public object? promocionId { get; set; }
    public object? promocion { get; set; }
    public decimal subtotal { get; set; }
    public decimal descuento { get; set; }
    public decimal impuestos { get; set; }
    public decimal total { get; set; }
    public int estadoId { get; set; }
    public string estadoNombre { get; set; }
    public string notas { get; set; }
    public DateTime createdAt { get; set; }
    public ReservadetalleConsultarReservaPaquetesResponse[] reservaDetalles { get; set; }
}

public class ClienteConsultarReservaPaquetesResponse
{
    public int id { get; set; }
    public int bookingUserId { get; set; }
    public string nombres { get; set; }
    public string apellidos { get; set; }
    public string email { get; set; }
    public string telefono { get; set; }
}

public class ReservadetalleConsultarReservaPaquetesResponse
{
    public int id { get; set; }
    public int servicioId { get; set; }
    public int cantidad { get; set; }
    public decimal precioUnitario { get; set; }
    public decimal subtotal { get; set; }
    public string fechaInicio { get; set; }
    public string fechaFin { get; set; }
}

public class _LinksConsultarReservaPaquetesResponse
{
    public string href { get; set; }
    public string rel { get; set; }
    public string method { get; set; }
}

// https://worldagencybk.runasp.net/api/v2/paquetes/665/reserva

public static class ConsultarReservaPaquetes
{
    public static async Task<ConsultarReservaPaquetesResponse> ConsultarReservaAsync(
        string baseUri,
        string idReserva)
    {
        var httpClient = Global.CachedHttpClient;
        if (!baseUri.Contains("{id}"))
            throw new ArgumentException("The baseUri must contain the {id} placeholder.", nameof(baseUri));
        var uri = baseUri.Replace("{id}", Uri.UnescapeDataString(idReserva));
        var response = await httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var reservaResponse = await response.Content.ReadFromJsonAsync<ConsultarReservaPaquetesResponse>();
        return reservaResponse ?? throw new InvalidOperationException();
    }
}
