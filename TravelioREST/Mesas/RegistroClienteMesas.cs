using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

public class RegistroClienteRequest
{
    public string nombre { get; set; }
    public string apellido { get; set; }
    public string email { get; set; }
    public string tipo_identificacion { get; set; } = "Cedula";
    public string identificacion { get; set; }
}

public class ClienteCreadoResponse
{
    public int id { get; set; }
    public string mensaje { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
    public string email { get; set; }
    public string tipo_identificacion { get; set; }
    public string identificacion { get; set; }
    public _LinksClienteCreado _links { get; set; }
}

public class _LinksClienteCreado
{
    public SelfClienteCreado self { get; set; }
    public ListarClienteCreado listar { get; set; }
    public ObtenerClienteCreado obtener { get; set; }
}

public class SelfClienteCreado
{
    public string href { get; set; }
    public string method { get; set; }
}

public class ListarClienteCreado
{
    public string href { get; set; }
    public string method { get; set; }
}

public class ObtenerClienteCreado
{
    public string href { get; set; }
    public string method { get; set; }
}

public static class RegistroClienteMesas
{
    public static async Task<ClienteCreadoResponse> CrearClienteAsync(string uri, 
        string nombre,
        string apellido,
        string correo,
        string identificacion,
        string tipoIdentificacion = "Cedula")
    {
        var clienteRequest = new RegistroClienteRequest
        {
            nombre = nombre,
            apellido = apellido,
            email = correo,
            tipo_identificacion = tipoIdentificacion,
            identificacion = identificacion
        };

        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(uri, clienteRequest);
        response.EnsureSuccessStatusCode();
        var clienteCreado = await response.Content.ReadFromJsonAsync<ClienteCreadoResponse>();
        return clienteCreado ?? throw new InvalidOperationException();
    }
}
