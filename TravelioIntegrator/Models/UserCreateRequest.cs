using TravelioDatabaseConnector.Enums;

namespace TravelioIntegrator.Models;

public record struct UserCreateRequest(
    string Correo,
    string Nombre,
    string Apellido,
    DateOnly FechaNacimiento,
    string TipoIdentificacion,
    string DocumentoIdentidad,
    string PasswordPlano,
    string? Pais = null,
    string? Telefono = null,
    RolUsuario? Rol = null);
