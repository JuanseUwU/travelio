using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Web;
using static TravelioREST.Global;

namespace TravelioREST.Aerolinea;

public class ConsultaVuelosResponse
{
    public int count { get; set; }
    public FlightConsulta[] flights { get; set; }
    public _LinksConsultaVuelos _links { get; set; }
}

public class _LinksConsultaVuelos
{
    public string self { get; set; }
    public string hold { get; set; }
    public string availability { get; set; }
    public string book { get; set; }
}

public class FlightConsulta
{
    public int IdVuelo { get; set; }
    public string Origen { get; set; }
    public string Destino { get; set; }
    public DateTime FechaSalida { get; set; }
    public DateTime FechaLlegada { get; set; }
    public string TipoCabina { get; set; }
    public int Pasajeros { get; set; }
    public string NombreAerolinea { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    public string Moneda { get; set; }
    public int CapacidadDisponible { get; set; }
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
        string? sort = null,
        string? moneda = null)
    {
        var uriBuilder = new UriBuilder(baseUri);

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrEmpty(from))
            query["from"] = Uri.EscapeDataString(from);

        if (!string.IsNullOrEmpty(to))
            query["to"] = Uri.EscapeDataString(to);

        if (dateFrom.HasValue)
            query["dateFrom"] = dateFrom.ToString();

        if (dateTo.HasValue)
            query["dateTo"] = dateTo.ToString();

        if (!string.IsNullOrEmpty(cabin))
            query["cabin"] = Uri.EscapeDataString(cabin);

        if (pasajeros.HasValue)
            query["pasajeros"] = pasajeros.ToString();

        if (!string.IsNullOrEmpty(sort))
            query["sort"] = Uri.EscapeDataString(sort);

        if (!string.IsNullOrEmpty(moneda))
            query["moneda"] = Uri.EscapeDataString(moneda);

        uriBuilder.Query = query.ToString();

        var url = uriBuilder.ToString();

        var vuelos = await CachedHttpClient.GetFromJsonAsync<ConsultaVuelosResponse>(url);
        return vuelos?.flights ?? throw new InvalidOperationException();
    }
}