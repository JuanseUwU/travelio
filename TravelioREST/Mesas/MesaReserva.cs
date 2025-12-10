using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

public class MesaReservaRequest
{
    public string id_mesa { get; set; }
    public string id_hold { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
    public string correo { get; set; }
    public string tipo_identificacion { get; set; } = "Cedula";
    public string identificacion { get; set; }
    public DateTime fecha { get; set; }
    public int personas { get; set; }
}

public class MesaReservaResponse
{
    public string idReserva { get; set; }
    public decimal valor_pagado { get; set; }
    public string uri_factura { get; set; }
    public string mensaje { get; set; }
}

public static class MesaReserva
{
    public static async Task<MesaReservaResponse> CrearMesaReservaAsync(string uri,
        string id_mesa,
        string id_hold,
        string nombre,
        string apellido,
        string correo,
        string identificacion,
        DateTime fecha,
        int personas,
        string tipo_identificacion = "Cedula")
    {
        var request = new MesaReservaRequest
        {
            id_mesa = id_mesa,
            id_hold = id_hold,
            nombre = nombre,
            apellido = apellido,
            correo = correo,
            tipo_identificacion = tipo_identificacion,
            identificacion = identificacion,
            fecha = fecha,
            personas = personas
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(uri, request);
        var mesaReserva = await response.Content.ReadFromJsonAsync<MesaReservaResponse>();
        return mesaReserva ?? throw new InvalidOperationException();
    }
}
