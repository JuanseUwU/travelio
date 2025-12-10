using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

public class CancelarReservaVuelosRequest
{
    public int ReservationId { get; set; }
}

public class CancelarReservaVuelosResponse
{
    public bool Success { get; set; }
    public int ReservationId { get; set; }
    public string Estado { get; set; }
    public decimal TotalPagado { get; set; }
    public _LinksCancelacion _links { get; set; }
}

public class _LinksCancelacion
{
    public string self { get; set; }
    public string reserva { get; set; }
    public string reservas { get; set; }
}

public static class CancelarReservaVuelos
{
    public static async Task<CancelarReservaVuelosResponse> CancelarReservaAsync(
        string baseUri,
        int reservationId)
    {
        var cancelacionRequest = new CancelarReservaVuelosRequest
        {
            ReservationId = reservationId
        };
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, cancelacionRequest);
        response.EnsureSuccessStatusCode();
        var cancelacionResponse = await response.Content.ReadFromJsonAsync<CancelarReservaVuelosResponse>();
        return cancelacionResponse ?? throw new InvalidOperationException();
    }
}
