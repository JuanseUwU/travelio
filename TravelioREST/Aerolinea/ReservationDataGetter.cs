using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

public class ReservaDataResponse
{
    public int IdReserva { get; set; }
    public string Aerolinea { get; set; }
    public string Origen { get; set; }
    public string Destino { get; set; }
    public string TipoCabina { get; set; }
    public DateTime Fecha { get; set; }
    public PasajeroReservaInfo[] Pasajeros { get; set; }
    public int AsientosReservados { get; set; }
    public _LinksReservaInfo _links { get; set; }
}

public class _LinksReservaInfo
{
    public string self { get; set; }
    public string factura { get; set; }
    public string search { get; set; }
    public string hold { get; set; }
    public string book { get; set; }
}

public class PasajeroReservaInfo
{
    public string NombreCompleto { get; set; }
    public string Documento { get; set; }
    public string Nacionalidad { get; set; }
    public DateTime FechaNacimiento { get; set; }
    public string Ticket { get; set; }
    public string Asiento { get; set; }
    public decimal Precio { get; set; }
}


public static class ReservationDataGetter
{
    public static async Task<ReservaDataResponse> GetReservationDataAsync(
        string baseUri,
        int reservationId)
    {
        var httpClient = Global.CachedHttpClient;
        var requestUri = $"{baseUri}?idReserva={reservationId}";
        var response = await httpClient.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        var reservaData = await response.Content.ReadFromJsonAsync<ReservaDataResponse>();
        return reservaData ?? throw new InvalidOperationException();
    }
}
