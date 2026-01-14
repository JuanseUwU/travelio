using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

public class MesasDisponibilidadRequest
{
    public int id_mesa { get; set; }
    public DateTime fecha { get; set; }
    public int numeroPersonas { get; set; }
}

public class MesasDisponibilidadResponse
{
    public int idMesa { get; set; }
    public DateTime fecha { get; set; }
    public bool disponible { get; set; }
    public string? mensaje { get; set; }
}

public static class VerificarDisponibilidadMesas
{
    public static async Task<bool> VerificarAsync(string url, int idMesa, DateTime fecha, int numeroPersonas)
    {
        var request = new MesasDisponibilidadRequest
        {
            id_mesa = idMesa,
            fecha = fecha,
            numeroPersonas = numeroPersonas
        };

        var response = await Global.CachedHttpClient.PostAsJsonAsync(url, request);
        response.EnsureSuccessStatusCode();
        var disponibilidadResponse = await response.Content.ReadFromJsonAsync<MesasDisponibilidadResponse>();
        return disponibilidadResponse?.disponible ?? false;
    }
}
