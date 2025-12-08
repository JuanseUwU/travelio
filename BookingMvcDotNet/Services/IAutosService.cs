using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Interfaz del servicio de autos que conecta con TravelioAPIConnector.
/// </summary>
public interface IAutosService
{
    /// <summary>
    /// Busca vehículos en todos los proveedores activos según los filtros.
    /// </summary>
    Task<AutosSearchViewModel> BuscarAutosAsync(AutosSearchViewModel filtros);

    /// <summary>
    /// Obtiene el detalle de un auto específico de un proveedor.
    /// </summary>
    Task<AutoDetalleViewModel?> ObtenerAutoAsync(int servicioId, string idAuto);

    /// <summary>
    /// Verifica la disponibilidad de un auto en un rango de fechas.
    /// </summary>
    Task<bool> VerificarDisponibilidadAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Crea una prerreserva (hold) para un auto.
    /// </summary>
    Task<(bool exito, string mensaje)> CrearPrerreservaAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin);
}
