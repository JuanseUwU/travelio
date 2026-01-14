using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using static TravelioREST.Global;

namespace TravelioREST.Aerolinea;

//public sealed class HoldRequest
//{
//    public int idVuelo { get; set; }
//    public required string[] seats { get; set; }
//    public int duracionHoldSegundos { get; set; }
//}


//public sealed class HoldResponse
//{
//    public int idVuelo { get; set; }
//    public required string holdId { get; set; }
//    public required string expiraEn { get; set; }
//}

public class HoldRequest
{
    public string idVuelo { get; set; }
    public PasajeroHoldRequest[] pasajeros { get; set; }
    public int duracionHoldSegundos { get; set; }
}

public class PasajeroHoldRequest
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
}

public class HoldResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public DataHoldResponse data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class DataHoldResponse
{
    public bool success { get; set; }
    public string idHold { get; set; }
    public DateTime expiresAt { get; set; }
    public string mensaje { get; set; }
}

public static class HoldCreator
{
    public static async Task<HoldResponse> CreateHoldAsync(
        string uri,
        string idVuelo,
        (string nombre, string apellido, string tipoIdentificacion, string identificacion, DateTime fechaNacimiento)[] pasajeros,
        int duracionHold = 300)
    {
        var request = new HoldRequest
        {
            idVuelo = idVuelo,
            duracionHoldSegundos = duracionHold,
            pasajeros = new PasajeroHoldRequest[pasajeros.Length]
        };

        for (int i = 0; i < pasajeros.Length; i++)
        {
            request.pasajeros[i] = new PasajeroHoldRequest
            {
                nombre = pasajeros[i].nombre,
                apellido = pasajeros[i].apellido,
                tipoIdentificacion = pasajeros[i].tipoIdentificacion,
                identificacion = pasajeros[i].identificacion,
                fechaNacimiento = pasajeros[i].fechaNacimiento
            };
        }

        var response = await CachedHttpClient.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        var holdResponse = await response.Content.ReadFromJsonAsync<HoldResponse>();
        return holdResponse ?? throw new InvalidOperationException();
    }
}
