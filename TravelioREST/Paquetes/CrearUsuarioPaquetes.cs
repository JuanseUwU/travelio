using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Paquetes;

public class UserRequest
{
    public string correo { get; set; }
    public string nombre { get; set; }
    public string apellido { get; set; }
}

public class UserResponse
{
    public string idUsuario { get; set; }
    public string correo { get; set; }
    public bool exitoso { get; set; }
    public string mensaje { get; set; }
}

public static class CrearUsuarioPaquetes
{
    public static async Task<UserResponse> CrearUsuarioAsync(
        string baseUri,
        string correo,
        string nombre,
        string apellido)
    {
        var userRequest = new UserRequest
        {
            correo = correo,
            nombre = nombre,
            apellido = apellido
        };
        var httpClient = Global.CachedHttpClient;
        var response = await httpClient.PostAsJsonAsync(baseUri, userRequest);
        response.EnsureSuccessStatusCode();
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        return userResponse ?? throw new InvalidOperationException();
    }
}
