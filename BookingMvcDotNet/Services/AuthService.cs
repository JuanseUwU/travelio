using BookingMvcDotNet.Models;
using TravelioDatabaseConnector.Models;
using TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class AuthService(TravelioIntegrationService integrationService, ILogger<AuthService> logger) : IAuthService
{
    public async Task<(bool exito, string mensaje, Cliente? cliente)> RegistrarAsync(RegisterViewModel model)
    {
        try
        {
            var existente = await integrationService.ObtenerClientePorEmailAsync(model.Email, logger);
            if (existente is not null)
            {
                logger.LogWarning("Intento de registro con correo existente: {Email}", model.Email);
                return (false, "Ya existe una cuenta con este correo electronico.", null);
            }

            var request = new UserCreateRequest(
                Correo: model.Email,
                Nombre: model.Nombre,
                Apellido: model.Apellido,
                FechaNacimiento: DateOnly.FromDateTime(model.FechaNacimiento),
                TipoIdentificacion: model.TipoIdentificacion,
                DocumentoIdentidad: model.DocumentoIdentidad,
                PasswordPlano: model.Password,
                Pais: model.Pais,
                Telefono: model.Telefono);

            var cliente = await integrationService.CrearUsuarioAsync(request, logger);
            if (cliente is null)
            {
                return (false, "Error al registrar. Intente nuevamente.", null);
            }

            logger.LogInformation("Nuevo cliente registrado: {Email} (ID: {Id})", cliente.CorreoElectronico, cliente.Id);

            return (true, "Registro exitoso.", cliente);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al registrar cliente {Email}", model.Email);
            return (false, "Error al registrar. Intente nuevamente.", null);
        }
    }

    public async Task<(bool exito, string mensaje, Cliente? cliente)> LoginAsync(string email, string password)
    {
        try
        {
            var cliente = await integrationService.IniciarSesionAsync(email, password, logger);
            if (cliente is null)
            {
                logger.LogWarning("Intento de login invalido: {Email}", email);
                return (false, "Correo o contrasena incorrectos.", null);
            }

            logger.LogInformation("Login exitoso: {Email} (ID: {Id})", cliente.CorreoElectronico, cliente.Id);

            return (true, "Login exitoso.", cliente);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al iniciar sesion para {Email}", email);
            return (false, "Error al iniciar sesion. Intente nuevamente.", null);
        }
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int clienteId)
    {
        try
        {
            return await integrationService.ObtenerClientePorIdAsync(clienteId, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string email)
    {
        try
        {
            return await integrationService.ObtenerClientePorEmailAsync(email, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener cliente por email {Email}", email);
            return null;
        }
    }
}
