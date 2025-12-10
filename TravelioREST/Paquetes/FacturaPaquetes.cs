using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public class FacturaRequest
{
    public string idReserva { get; set; }
    public string correo { get; set; }
    public string nombre { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
    public decimal valor { get; set; }
}

public class FacturaResponse
{
    public string idFactura { get; set; }
    public string idReserva { get; set; }
    public string correo { get; set; }
    public string nombreCompleto { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
    public decimal valorPagado { get; set; }
    public DateTime fechaEmision { get; set; }
    public string uriFactura { get; set; }
    public string mensaje { get; set; }
}

public static class FacturaPaquetes
{
    public static async Task<FacturaResponse> GenerarFacturaAsync(
        string baseUri,
        FacturaRequest facturaRequest)
    {
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, facturaRequest);
        response.EnsureSuccessStatusCode();
        var facturaResponse = await response.Content.ReadFromJsonAsync<FacturaResponse>();
        return facturaResponse ?? throw new InvalidOperationException();
    }
}
