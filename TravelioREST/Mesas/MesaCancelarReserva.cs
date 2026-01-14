using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

//public class ReservaCancelarRequest
//{
//    public string id_reserva { get; set; }
//}

//public class ReservaCancelarResponse
//{
//    public bool exito { get; set; }
//    public decimal valor_pagado { get; set; }
//}

public class ReservaCancelarResponse
{
    public bool exito { get; set; }
    public decimal valorPagado { get; set; }
}

public static class MesaCancelarReserva
{
    public static async Task<ReservaCancelarResponse> CancelarMesaReservaAsync(string uri,
        string id_reserva)
    {
        //var request = new ReservaCancelarRequest
        //{
        //    id_reserva = id_reserva
        //};
        var fullUri = $"{uri}/{id_reserva}";
        var response = await Global.CachedHttpClient.PutAsync(fullUri, null); // Cambiado de PostAsJsonAsync a PutAsJsonAsync
        response.EnsureSuccessStatusCode();
        var cancelarResponse = await response.Content.ReadFromJsonAsync<ReservaCancelarResponse>();
        return cancelarResponse ?? throw new InvalidOperationException();
    }
}
