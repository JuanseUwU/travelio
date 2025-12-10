using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

public class MesaHoldRequest
{
    public string id_mesa { get; set; }
    public DateTime fecha { get; set; }
    public int numero_clientes { get; set; }
    public int duracionHoldSegundos { get; set; }
}

public class MesaHoldResponse
{
    public string id_hold { get; set; }
    public string mensaje { get; set; }
}

public static class MesaHold
{
    public static async Task<MesaHoldResponse> CrearMesaHoldAsync(string uri,
        string id_mesa,
        DateTime fecha,
        int numero_clientes,
        int duracionHoldSegundos = 300)
    {
        var request = new MesaHoldRequest
        {
            id_mesa = id_mesa,
            fecha = fecha,
            numero_clientes = numero_clientes,
            duracionHoldSegundos = duracionHoldSegundos
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(uri, request);
        var mesaHold = await response.Content.ReadFromJsonAsync<MesaHoldResponse>();
        return mesaHold ?? throw new InvalidOperationException();
    }
}
