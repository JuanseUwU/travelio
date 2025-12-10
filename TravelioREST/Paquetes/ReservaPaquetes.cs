using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public class ReservaRequest
{
    public string idPaquete { get; set; }
    public string idHold { get; set; }
    public string correo { get; set; }
    public string metodoPago { get; set; } = "TRANSFERENCIA";
    public TuristaReserva[] turistas { get; set; }
    public string paymentStatus { get; set; } = "CONFIRMADO";
}

public class TuristaReserva
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
}

public class ReservaResponse
{
    public string id_reserva { get; set; }
    public string id_paquete { get; set; }
    public string id_hold { get; set; }
    public string correo { get; set; }
    public string payment_status { get; set; }
    public TuristaReservaResponse[] turistas { get; set; }
    public _LinksReserva[] _links { get; set; }
}

public class TuristaReservaResponse
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
}

public class _LinksReserva
{
    public string href { get; set; }
    public string rel { get; set; }
    public string method { get; set; }
}

public static class ReservaPaquetes
{
    public static async Task<ReservaResponse> CrearReservaAsync(
        string baseUri,
        ReservaRequest reservaRequest)
    {
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, reservaRequest);
        response.EnsureSuccessStatusCode();
        var reservaResponse = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        return reservaResponse ?? throw new InvalidOperationException();
    }
}
