using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Autos;

public class CancelarReservaResponse
{
    public decimal ValorPagado { get; set; }
    public bool Exito { get; set; }
}

public static class CancelarReservaAutos
{
    public static async Task<CancelarReservaResponse> CancelarReservaAsync(
        string baseUri,
        string idReserva)
    {
        var httpClient = Global.CachedHttpClient;
        var requestUri = $"{baseUri}/{idReserva}";
        var response = await httpClient.PostAsync(requestUri, null);
        response.EnsureSuccessStatusCode();
        var cancelarReservaResponse = await response.Content.ReadFromJsonAsync<CancelarReservaResponse>();
        return cancelarReservaResponse ?? throw new InvalidOperationException();
    }
}
