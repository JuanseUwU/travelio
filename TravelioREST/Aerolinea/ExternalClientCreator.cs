using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

//public class ClienteRequestData
//{
//    public string Nombre { get; set; }
//    public string Apellido { get; set; }
//    public string Correo { get; set; }
//}

//public class ClienteResponse
//{
//    public int IdUsuario { get; set; }
//    public string NombreCompleto { get; set; }
//    public string Email { get; set; }
//    public _LinksClienteCreator _links { get; set; }
//}

//public class _LinksClienteCreator
//{
//    public string self { get; set; }
//}

public class ClienteRequestData
{
    public string correo { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
    public DateTime fechaNacimiento { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
}

public class ClienteResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public DataClienteResponse data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class DataClienteResponse
{
    public bool success { get; set; }
    public int idUsuario { get; set; }
    public bool esNuevo { get; set; }
    public string mensaje { get; set; }
}

public static class ExternalClientCreator
{
    public static async Task<ClienteResponse> CreateExternalClientAsync(string uri,
        string correo,
        string nombre,
        string apellido,
        DateTime fechaNacimiento,
        string tipoIdentificacion,
        string identificacion)
    {
        var request = new ClienteRequestData
        {
            correo = correo,
            nombre = nombre,
            apellido = apellido,
            fechaNacimiento = fechaNacimiento,
            tipoIdentificacion = tipoIdentificacion,
            identificacion = identificacion
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        var client = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        return client ?? throw new InvalidOperationException();
    }
}
