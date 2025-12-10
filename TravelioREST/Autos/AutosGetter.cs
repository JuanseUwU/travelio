using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Autos;

public class AutosResponse
{
    public Datum[] Data { get; set; }
    public LinkAutosList[] Links { get; set; }
}

public class Datum
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

public class LinkAutosList
{
    public string Rel { get; set; }
    public string Href { get; set; }
    public string Method { get; set; }
}


public static class AutosGetter
{
    public static async Task<AutosResponse> GetAutosAsync(string uri)
    {
        var result = await Global.CachedHttpClient.GetFromJsonAsync<AutosResponse>(uri);
        return result ?? throw new InvalidOperationException();
    }
}
