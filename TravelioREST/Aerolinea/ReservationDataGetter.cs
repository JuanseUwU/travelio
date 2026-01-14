using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

//public class ReservaDataResponse
//{
//    public int IdReserva { get; set; }
//    public string Aerolinea { get; set; }
//    public string Origen { get; set; }
//    public string Destino { get; set; }
//    public string TipoCabina { get; set; }
//    public DateTime Fecha { get; set; }
//    public PasajeroReservaInfo[] Pasajeros { get; set; }
//    public int AsientosReservados { get; set; }
//    public _LinksReservaInfo _links { get; set; }
//}

//public class _LinksReservaInfo
//{
//    public string self { get; set; }
//    public string factura { get; set; }
//    public string search { get; set; }
//    public string hold { get; set; }
//    public string book { get; set; }
//}

//public class PasajeroReservaInfo
//{
//    public string NombreCompleto { get; set; }
//    public string Documento { get; set; }
//    public string Nacionalidad { get; set; }
//    public DateTime FechaNacimiento { get; set; }
//    public string Ticket { get; set; }
//    public string Asiento { get; set; }
//    public decimal Precio { get; set; }
//}

public class ReservaDataResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public DataReservaDataResponse data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class DataReservaDataResponse
{
    public string idReserva { get; set; }
    public string origen { get; set; }
    public string destino { get; set; }
    public string correo { get; set; }
    public DateTime fecha { get; set; }
    public string tipoCabina { get; set; }
    public PasajeroReservaDataResponse[] pasajeros { get; set; }
    public string nombreAerolinea { get; set; }
    public int asientosReservados { get; set; }
    public decimal valorPagado { get; set; }
    public string uriFactura { get; set; }
}

public class PasajeroReservaDataResponse
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
    public string numeroTicket { get; set; }
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
