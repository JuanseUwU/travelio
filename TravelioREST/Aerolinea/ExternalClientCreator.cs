using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

public class ClienteRequestData
{
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Correo { get; set; }
}

public class ClienteResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; }
    public string Email { get; set; }
    public _LinksClienteCreator _links { get; set; }
}

public class _LinksClienteCreator
{
    public string self { get; set; }
}


public static class ExternalClientCreator
{
    public static async Task<ClienteResponse> CreateExternalClientAsync(string url, string nombre, string apellido, string correo)
    {
        var request = new ClienteRequestData
        {
            Nombre = nombre,
            Apellido = apellido,
            Correo = correo
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(url, request);
        var client = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        return client ?? throw new InvalidOperationException();
    }
}
