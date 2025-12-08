using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Models;
using TravelioDatabaseConnector.Services;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementación del servicio de autenticación usando TravelioDbContext.
/// </summary>
public class AuthService(TravelioDbContext dbContext, ILogger<AuthService> logger) : IAuthService
{
    public async Task<(bool exito, string mensaje, Cliente? cliente)> RegistrarAsync(RegisterViewModel model)
    {
        try
        {
            // Verificar si el correo ya existe
            var existente = await dbContext.Clientes
                .FirstOrDefaultAsync(c => c.CorreoElectronico == model.Email);

            if (existente != null)
            {
                logger.LogWarning("Intento de registro con correo existente: {Email}", model.Email);
                return (false, "Ya existe una cuenta con este correo electrónico.", null);
            }

            // Crear nuevo cliente
            var cliente = new Cliente
            {
                CorreoElectronico = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                TipoIdentificacion = model.TipoIdentificacion,
                DocumentoIdentidad = model.DocumentoIdentidad,
                FechaNacimiento = DateOnly.FromDateTime(model.FechaNacimiento),
                Telefono = model.Telefono,
                Pais = model.Pais
            };

            // Establecer contraseña hasheada
            ClientePasswordService.EstablecerPassword(cliente, model.Password);

            dbContext.Clientes.Add(cliente);
            await dbContext.SaveChangesAsync();

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
            var cliente = await dbContext.Clientes
                .FirstOrDefaultAsync(c => c.CorreoElectronico == email);

            if (cliente == null)
            {
                logger.LogWarning("Intento de login con correo no existente: {Email}", email);
                return (false, "Correo o contraseña incorrectos.", null);
            }

            // Verificar contraseña
            var passwordValido = ClientePasswordService.EsPasswordValido(cliente, password);

            if (!passwordValido)
            {
                logger.LogWarning("Contraseña incorrecta para: {Email}", email);
                return (false, "Correo o contraseña incorrectos.", null);
            }

            logger.LogInformation("Login exitoso: {Email} (ID: {Id})", cliente.CorreoElectronico, cliente.Id);

            return (true, "Login exitoso.", cliente);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al iniciar sesión para {Email}", email);
            return (false, "Error al iniciar sesión. Intente nuevamente.", null);
        }
    }

    public async Task<Cliente?> ObtenerClientePorIdAsync(int clienteId)
    {
        try
        {
            return await dbContext.Clientes.FindAsync(clienteId);
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
            return await dbContext.Clientes
                .FirstOrDefaultAsync(c => c.CorreoElectronico == email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al obtener cliente por email {Email}", email);
            return null;
        }
    }
}
