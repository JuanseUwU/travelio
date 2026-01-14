using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Web;
using static TravelioREST.Global;

namespace TravelioREST.Aerolinea;

//public class ConsultaVuelosResponse
//{
//    public int count { get; set; }
//    public FlightConsulta[] flights { get; set; }
//    public _LinksConsultaVuelos _links { get; set; }
//}

//public class _LinksConsultaVuelos
//{
//    public string self { get; set; }
//    public string hold { get; set; }
//    public string availability { get; set; }
//    public string book { get; set; }
//}

//public class FlightConsulta
//{
//    public int IdVuelo { get; set; }
//    public string Origen { get; set; }
//    public string Destino { get; set; }
//    public DateTime FechaSalida { get; set; }
//    public DateTime FechaLlegada { get; set; }
//    public string TipoCabina { get; set; }
//    public int Pasajeros { get; set; }
//    public string NombreAerolinea { get; set; }
//    public decimal PrecioNormal { get; set; }
//    public decimal PrecioActual { get; set; }
//    public string Moneda { get; set; }
//    public int CapacidadDisponible { get; set; }
//}

public class ConsultaVuelosResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public FlightConsulta[] data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class FlightConsulta
{
    public string idVuelo { get; set; }
    public string origen { get; set; }
    public string destino { get; set; }
    public DateTime fecha { get; set; }
    public string tipoCabina { get; set; }
    public int capacidadPasajeros { get; set; }
    public string nombreAerolinea { get; set; }
    public int capacidadActual { get; set; }
    public decimal precioNormal { get; set; }
    public decimal precioActual { get; set; }
}


public static class VuelosGetter
{
    public static async Task<FlightConsulta[]> GetVuelosAsync(string baseUri,
        string? from = null,
        string? to = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? cabin = null,
        int? pasajeros = null,
        decimal? precio_min = null,
        decimal? precio_max = null)
    {
        var uriBuilder = new UriBuilder(baseUri);

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrEmpty(from))
            query["from"] = Uri.EscapeDataString(from);

        if (!string.IsNullOrEmpty(to))
            query["to"] = Uri.EscapeDataString(to);

        if (dateFrom.HasValue)
            query["date_from"] = dateFrom.ToString();

        if (dateTo.HasValue)
            query["date_to"] = dateTo.ToString();

        if (!string.IsNullOrEmpty(cabin))
            query["cabin"] = Uri.EscapeDataString(cabin);

        if (pasajeros.HasValue)
            query["pasajeros"] = pasajeros.ToString();

        if (precio_min.HasValue)
            query["precio_min"] = precio_min.ToString();

        if (precio_max.HasValue)
            query["precio_max"] = precio_max.ToString();

        uriBuilder.Query = query.ToString();

        var url = uriBuilder.ToString();

        var vuelos = await CachedHttpClient.GetFromJsonAsync<ConsultaVuelosResponse>(url);
        return vuelos?.data ?? throw new InvalidOperationException();
    }
}