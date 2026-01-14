using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace TravelioREST.Autos;

//public class AutosResponse
//{
//    public Datum[] Data { get; set; }
//    public LinkAutosList[] Links { get; set; }
//}

//public class Datum
//{
//    public string IdAuto { get; set; }
//    public string Tipo { get; set; }
//    public int Capacidad { get; set; }
//    public decimal PrecioNormal { get; set; }
//    public decimal? PrecioActual { get; set; }
//    public string UriImagen { get; set; }
//    public string Ciudad { get; set; }
//    public string Pais { get; set; }
//}

//public class LinkAutosList
//{
//    public string Rel { get; set; }
//    public string Href { get; set; }
//    public string Method { get; set; }
//}

public class AutosResponse
{
    public DatumAutosResponse[] Data { get; set; }
}

public class DatumAutosResponse
{
    public string IdAuto { get; set; }
    public string Tipo { get; set; }
    public int Capacidad { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal? PrecioActual { get; set; }
    public string UriImagen { get; set; }
    public string Ciudad { get; set; }
    public string Pais { get; set; }
}


public static class AutosGetter
{
    public static async Task<AutosResponse> GetAutosAsync(string uri,
        string? categoria = null,
        string? transmision = null,
        int? capacidad = null,
        decimal? precioMin = null,
        decimal? precioMax = null,
        string? sort = null,
        string? ciudad = null,
        string? pais = null)
    {
        var uriBuilder = new UriBuilder(uri);

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrEmpty(categoria))
            query["categoria"] = categoria;

        if (!string.IsNullOrEmpty(transmision))
            query["transmision"] = transmision;

        if (capacidad is not null)
            query["capacidad"] = capacidad.ToString();

        if (precioMin is not null)
            query["precio_min"] = precioMin.ToString();

        if (precioMax is not null)
            query["precio_max"] = precioMax.ToString();

        if (!string.IsNullOrEmpty(sort))
            query["sort"] = sort;

        if (!string.IsNullOrEmpty(ciudad))
            query["ciudad"] = ciudad;

        if (!string.IsNullOrEmpty(pais))
            query["pais"] = pais;

        uriBuilder.Query = query.ToString();

        var result = await Global.CachedHttpClient.GetFromJsonAsync<AutosResponse>(uriBuilder.ToString());
        return result ?? throw new InvalidOperationException();
    }
}
