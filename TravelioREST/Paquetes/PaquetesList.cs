using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace TravelioREST.Paquetes;

public class PaquetesListResponse
{
    public DatoPaquetes[] datos { get; set; }
    public PaginacionPaquetes paginacion { get; set; }
}

public class PaginacionPaquetes
{
    public int paginaActual { get; set; }
    public int limite { get; set; }
    public int totalPaginas { get; set; }
    public int totalElementos { get; set; }
}

public class DatoPaquetes
{
    public string idPaquete { get; set; }
    public string nombre { get; set; }
    public string ciudad { get; set; }
    public string pais { get; set; }
    public string tipoActividad { get; set; }
    public int capacidad { get; set; }
    public decimal precioNormal { get; set; }
    public decimal precioActual { get; set; }
    public string imagenUrl { get; set; }
    public int duracion { get; set; }
    public _LinksListarPaquetes[] _links { get; set; }
}

public class _LinksListarPaquetes
{
    public string href { get; set; }
    public string rel { get; set; }
    public string method { get; set; }
}

// https://worldagencybk.runasp.net/api/v2/paquetes?pais=Puerto%20Rico

public static class PaquetesList
{
    public static async Task<PaquetesListResponse> ObtenerPaquetesAsync(
        string baseUri,
        string? ciudad = null,
        string? pais = null,
        DateTime? fechaInicio = null,
        int? duracion = null,
        string? tipoActividad = null,
        int? capacidad = null,
        decimal? precioMin = null,
        decimal? precioMax = null,
        string? sort = null,
        int? pagina = null,
        int? limite = null)
    {
        var httpClient = Global.CachedHttpClient;

        var uriBuilder = new UriBuilder(baseUri);

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrEmpty(ciudad))
            query["ciudad"] = Uri.EscapeDataString(ciudad);

        if (!string.IsNullOrEmpty(pais))
            query["pais"] = Uri.EscapeDataString(pais);

        if (fechaInicio.HasValue)
            query["fecha_inicio"] = fechaInicio.ToString();

        if (duracion.HasValue)
            query["duracion"] = duracion.ToString();

        if (!string.IsNullOrEmpty(tipoActividad))
            query["tipo_actividad"] = Uri.EscapeDataString(tipoActividad);

        if (capacidad.HasValue)
            query["capacidad"] = capacidad.ToString();

        if (precioMin.HasValue)
            query["precio_min"] = precioMin.ToString();

        if (precioMax.HasValue)
            query["precio_max"] = precioMax.ToString();

        if (!string.IsNullOrEmpty(sort))
            query["sort"] = Uri.EscapeDataString(sort);

        if (pagina.HasValue)
            query["pagina"] = pagina.ToString();

        if (limite.HasValue)
            query["limite"] = limite.ToString();

        uriBuilder.Query = query.ToString();

        var url = uriBuilder.ToString();
        var response = await httpClient.GetFromJsonAsync<PaquetesListResponse>(url);
        return response ?? throw new InvalidOperationException();
    }
}
