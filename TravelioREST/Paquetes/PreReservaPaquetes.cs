using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public class HoldRequest
{
    public string idPaquete { get; set; }
    public string bookingUserId { get; set; }
    public string correo { get; set; }
    public DateTime fechaInicio { get; set; }
    public TuristaHold[] turistas { get; set; }
    public int duracionHoldSegundos { get; set; }
}

public class TuristaHold
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
}

public class HoldResponse
{
    public string id_hold { get; set; }
    public DateTime fechaExpiracion { get; set; }
    public LinksHold[] _links { get; set; }
}

public class LinksHold
{
    public string href { get; set; }
    public string rel { get; set; }
    public string method { get; set; }
}


public static class PreReservaPaquetes
{
    public static async Task<HoldResponse> CrearPreReservaAsync(
        string baseUri,
        HoldRequest holdRequest)
    {
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, holdRequest);
        response.EnsureSuccessStatusCode();
        var holdResponse = await response.Content.ReadFromJsonAsync<HoldResponse>();
        return holdResponse ?? throw new InvalidOperationException();
    }
}
