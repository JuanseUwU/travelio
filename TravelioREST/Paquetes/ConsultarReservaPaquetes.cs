using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public class ConsultarReservaPaquetesResponse
{
    public string id_reserva { get; set; }
    public string id_paquete { get; set; }
    public string correo { get; set; }
    public DateTime fecha_inicio { get; set; }
    public int duracion { get; set; }
    public string tipo_actividad { get; set; }
    public object[] turistas { get; set; }
    public decimal valor_pagado { get; set; }
    public string uri_factura { get; set; }
    public _LinksConsulta[] _links { get; set; }
}

public class _LinksConsulta
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
