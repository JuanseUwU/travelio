using BookingMvcDotNet.Models;
using TravelioDatabaseConnector.Models;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Servicio de autenticación que usa la base de datos TravelioDb.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registra un nuevo cliente en la base de datos.
    /// </summary>
    Task<(bool exito, string mensaje, Cliente? cliente)> RegistrarAsync(RegisterViewModel model);

    /// <summary>
    /// Inicia sesión verificando las credenciales.
    /// </summary>
    Task<(bool exito, string mensaje, Cliente? cliente)> LoginAsync(string email, string password);

    /// <summary>
    /// Obtiene un cliente por su ID.
    /// </summary>
    Task<Cliente?> ObtenerClientePorIdAsync(int clienteId);

    /// <summary>
    /// Obtiene un cliente por su correo electrónico.
    /// </summary>
    Task<Cliente?> ObtenerClientePorEmailAsync(string email);
}
