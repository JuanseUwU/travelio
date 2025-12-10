using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Autos;

public class BuscarReservaResponse
{
    public required string numero_matricula { get; set; }
    public required string correo { get; set; }
    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_fin { get; set; }
    public required string categoria { get; set; }
    public required string transmision { get; set; }
    public decimal valor_pagado { get; set; }
    public required string uri_factura { get; set; }
    public required Link[] Links { get; set; }
}


public static class AutosReservaObtenerDatos
{
    public static async Task<BuscarReservaResponse> GetReservaAsync(string uri, int numeroReserva)
    {
        uri = $"{uri}/{numeroReserva}";
        var result = await Global.CachedHttpClient.GetFromJsonAsync<BuscarReservaResponse>(uri);
        return result ?? throw new InvalidOperationException();
    }
}
