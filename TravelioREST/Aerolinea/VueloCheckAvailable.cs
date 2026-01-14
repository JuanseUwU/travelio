using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using static TravelioREST.Global;

namespace TravelioREST.Aerolinea;

//public class DisponibilidadRequest
//{
//    public int IdVuelo { get; set; }
//    public int Pasajeros { get; set; }
//}

//public class DisponibilidadResponse
//{
//    public bool disponible { get; set; }
//    public _LinksDisponibilidad _links { get; set; }
//}

//public class _LinksDisponibilidad
//{
//    public string self { get; set; }
//    public string hold { get; set; }
//}

public class DisponibilidadRequest
{
    public string idVuelo { get; set; }
    public int pasajeros { get; set; }
}

public class DisponibilidadResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public DataDisponibilidadResponse data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class DataDisponibilidadResponse
{
    public bool disponible { get; set; }
    public string mensaje { get; set; }
    public int asientosDisponibles { get; set; }
}

public static class VueloCheckAvailable
{
    public static async Task<bool> GetDisponibilidadAsync(string url, string idVuelo, int numPasajeros)
    {
        var request = new DisponibilidadRequest
        {
            idVuelo = idVuelo,
            pasajeros = numPasajeros
        };
        var response = await CachedHttpClient.PostAsJsonAsync(url, request);
        var disponibilidad = await response.Content.ReadFromJsonAsync<DisponibilidadResponse>();
        return disponibilidad?.data?.disponible ?? false;
    }
}
