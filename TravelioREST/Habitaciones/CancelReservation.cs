using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Habitaciones;

public sealed class CancellationResponse
{
    public bool success { get; set; }
    public decimal montoPagado { get; set; }
}

public static class CancelReservation
{
    public static async Task<CancellationResponse> CancelarReservaAsync(
        string baseUri,
        int reservationId)
    {
        var httpClient = Global.CachedHttpClient;
        //if (!baseUri.EndsWith("/cancel"))
        //    throw new ArgumentException("La baseUri debe terminar con /cancel", nameof(baseUri));
        var requestUri = $"{baseUri}?idReserva={reservationId}";
        var response = await httpClient.DeleteAsync(requestUri);
        response.EnsureSuccessStatusCode();
        var cancellationResponse = await response.Content.ReadFromJsonAsync<CancellationResponse>();
        return cancellationResponse ?? throw new InvalidOperationException();
    }
}
