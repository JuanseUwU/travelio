using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Autos;

public class NuevoClienteRequest
{
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public string Pais { get; set; }
}

public class NuevoClienteResponse
{
    public int IdUsuario { get; set; }
    public string BookingUserId { get; set; }
    public string Nombre { get; set; }
    public string Email { get; set; }
    public string Telefono { get; set; }
    public string Pais { get; set; }
    public string Estado { get; set; }
    public _LinksNuevoClienteResponse[] _links { get; set; }
}

public class _LinksNuevoClienteResponse
{
    public string Rel { get; set; }
    public string Href { get; set; }
    public string Method { get; set; }
}


public static class ExternalClientCreator
{
    public static async Task<NuevoClienteResponse> CrearClienteExternoAsync(string url,
        string nombre,
        string apellido,
        string email,
        string? telefono,
        string? pais)
    {
        var request = new NuevoClienteRequest()
        {
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Telefono = telefono,
            Pais = pais
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(url, request);
        response.EnsureSuccessStatusCode();
        var cliente = await response.Content.ReadFromJsonAsync<NuevoClienteResponse>();
        return cliente ?? throw new InvalidOperationException("No se pudo crear el cliente externo.");
    }
}
