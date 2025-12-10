using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace TravelioREST.Mesas;

public class MesasListResponse
{
    public string mensaje { get; set; }
    public int total { get; set; }
    public MesaFromList[] mesas { get; set; }
    public _LinksMesasList _links { get; set; }
}

public class _LinksMesasList
{
    public SelfMesasList self { get; set; }
    public CreateholdMesasList createHold { get; set; }
    public ReservarMesasList reservar { get; set; }
}

public class SelfMesasList
{
    public string href { get; set; }
}

public class CreateholdMesasList
{
    public string href { get; set; }
    public string method { get; set; }
}

public class ReservarMesasList
{
    public string href { get; set; }
    public string method { get; set; }
}

public class MesaFromList
{
    public int IdMesa { get; set; }
    public int NumeroMesa { get; set; }
    public string TipoMesa { get; set; }
    public int Capacidad { get; set; }
    public decimal Precio { get; set; }
    public string ImagenURL { get; set; }
    public string Estado { get; set; }
    public string Restaurante { get; set; }
}

// http://cangrejitosfelices.runasp.net/api/v1/integracion/restaurantes/search?capacidad=1&tipoMesa=Exterior&estado=Disponible

public static class MesasList
{
    public static async Task<MesaFromList[]> GetMesasListAsync(string baseUri,
        int? capacidad = null,
        string? tipoMesa = null,
        string? estado = null)
    {
        var uriBuilder = new UriBuilder(baseUri);

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (capacidad.HasValue)
            query["capacidad"] = capacidad.ToString();

        if (!string.IsNullOrEmpty(tipoMesa))
            query["tipoMesa"] = Uri.EscapeDataString(tipoMesa);

        if (!string.IsNullOrEmpty(estado))
            query["estado"] = Uri.EscapeDataString(estado);

        uriBuilder.Query = query.ToString();

        var url = uriBuilder.ToString();
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var mesasListResponse = await response.Content.ReadFromJsonAsync<MesasListResponse>();
        return mesasListResponse?.mesas ?? throw new InvalidOperationException();
    }
}
