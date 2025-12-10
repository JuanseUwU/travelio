using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using static TravelioREST.Global;

namespace TravelioREST.Aerolinea;

public sealed class HoldRequest
{
    public int idVuelo { get; set; }
    public required string[] seats { get; set; }
    public int duracionHoldSegundos { get; set; }
}


public sealed class HoldResponse
{
    public int idVuelo { get; set; }
    public required string holdId { get; set; }
    public required string expiraEn { get; set; }
}


public static class HoldCreator
{
    public static async Task<HoldResponse> CreateHoldAsync(string url, int idVuelo, int seats, int duracionHoldSegundos = 600)
    {
        throw new NotImplementedException("La creación de holds ha sido deshabilitada temporalmente.");

        //var request = new HoldRequest
        //{
        //    idVuelo = idVuelo,
        //    seats = seats,
        //    duracionHoldSegundos = duracionHoldSegundos
        //};
        //var response = await CachedHttpClient.PostAsJsonAsync(url, request);
        //var hold = await response.Content.ReadFromJsonAsync<HoldResponse>();
        //return hold ?? throw new InvalidOperationException();
    }
}
