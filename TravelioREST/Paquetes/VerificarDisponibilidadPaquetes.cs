using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public class VerificacionRequest
{
    public string idPaquete { get; set; }
    public DateTime fechaInicio { get; set; }
    public int personas { get; set; }
}

public class VerificacionResponse
{
    public bool disponible { get; set; }
    public string idPaquete { get; set; }
    public DateTime fechaInicio { get; set; }
    public int personas { get; set; }
    public string mensaje { get; set; }
}

public static class VerificarDisponibilidadPaquetes
{
    public static async Task<bool> VerificarDisponibilidadAsync(
        string baseUri,
        string idPaquete,
        DateTime fechaInicio,
        int numeroPersonas)
    {
        var verificacionRequest = new VerificacionRequest
        {
            idPaquete = idPaquete,
            fechaInicio = fechaInicio,
            personas = numeroPersonas
        };
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, verificacionRequest);
        response.EnsureSuccessStatusCode();
        var verificacionResponse = await response.Content.ReadFromJsonAsync<VerificacionResponse>();
        return verificacionResponse?.disponible ?? throw new InvalidOperationException();
    }
}
