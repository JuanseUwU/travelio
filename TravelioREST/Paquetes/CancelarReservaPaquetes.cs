using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public sealed class CancelacionRequest
{
    public required string id_reserva { get; set; }
}

public sealed class CancelacionResponse
{
    public bool exito { get; set; }
    public decimal valor_pasado { get; set; }
}


public static class CancelarReservaPaquetes
{
    public static async Task<CancelacionResponse> CancelarReservaAsync(
        string baseUri,
        string idReserva)
    {
        var cancelacionRequest = new CancelacionRequest
        {
            id_reserva = idReserva
        };
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, cancelacionRequest);
        response.EnsureSuccessStatusCode();
        var cancelacionResponse = await response.Content.ReadFromJsonAsync<CancelacionResponse>();
        return cancelacionResponse ?? throw new InvalidOperationException();
    }
}
