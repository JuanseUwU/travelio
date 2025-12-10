using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;


public sealed class ReservaRequest
{
    public int idVuelo { get; set; }
    public required string holdId { get; set; }
    public required string correo { get; set; }
    public required Pasajero[] pasajeros { get; set; }
}

public sealed class Pasajero
{
    public required string nombre { get; set; }
    public required string apellido { get; set; }
    public required string identificacion { get; set; }
}


public class ReservaResponse
{
    public int IdReserva { get; set; }
    public required string CodigoReserva { get; set; }
    public required string Estado { get; set; }
}



public static class ReservationCreator
{
    public static async Task<ReservaResponse> CreateReservationAsync(string url,
        int idVuelo,
        string holdId,
        string correo,
        (string nombre, string apellido, string tipoIdentificacion, string identificacion)[] pasajeros)
    {
        throw new NotImplementedException("La creación de reservas ha sido deshabilitada temporalmente.");

        //var reserva = new ReservaRequest
        //{
        //    idVuelo = idVuelo,
        //    holdId = holdId,
        //    correo = correo,
        //    pasajeros = Array.ConvertAll(pasajeros, p => new Pasajero
        //    {
        //        nombre = p.nombre,
        //        apellido = p.apellido,
        //        identificacion = p.identificacion
        //    })
        //};
        //var response = await Global.CachedHttpClient.PostAsJsonAsync(url, reserva);
        //var reservaResponse = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        //return reservaResponse ?? throw new InvalidOperationException();
    }
}
