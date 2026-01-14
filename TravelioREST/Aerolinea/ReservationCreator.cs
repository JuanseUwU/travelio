using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

//public sealed class ReservaRequest
//{
//    public int idVuelo { get; set; }
//    public required string holdId { get; set; }
//    public required string correo { get; set; }
//    public required Pasajero[] pasajeros { get; set; }
//}

//public sealed class Pasajero
//{
//    public required string nombre { get; set; }
//    public required string apellido { get; set; }
//    public required string identificacion { get; set; }
//}


//public class ReservaResponse
//{
//    public int IdReserva { get; set; }
//    public required string CodigoReserva { get; set; }
//    public required string Estado { get; set; }
//}

public class ReservaRequest
{
    public string idVuelo { get; set; }
    public string idHold { get; set; }
    public PasajeroReservaRequest[] pasajeros { get; set; }
    public string correo { get; set; }
}

public class PasajeroReservaRequest
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
}

public class ReservaResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public DataReservaResponse data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class DataReservaResponse
{
    public bool success { get; set; }
    public string idReserva { get; set; }
    public string codigoReserva { get; set; }
    public decimal total { get; set; }
    public string mensaje { get; set; }
}

public static class ReservationCreator
{
    public static async Task<ReservaResponse> CreateReservationAsync(string uri,
        string idVuelo,
        string idHold,
        string correo,
        (string nombre, string apellido, string tipoIdentificacion, string identificacion, DateTime fechaNacimiento)[] pasajeros)
    {
        var reservaRequest = new ReservaRequest
        {
            idVuelo = idVuelo,
            idHold = idHold,
            correo = correo,
            pasajeros = pasajeros.Select(p => new PasajeroReservaRequest
            {
                nombre = p.nombre,
                apellido = p.apellido,
                tipoIdentificacion = p.tipoIdentificacion,
                identificacion = p.identificacion,
                fechaNacimiento = p.fechaNacimiento
            }).ToArray()
        };

        var response = await Global.CachedHttpClient.PostAsJsonAsync(uri, reservaRequest);
        response.EnsureSuccessStatusCode();
        var reservaResponse = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        return reservaResponse ?? throw new InvalidOperationException();
    }
}
