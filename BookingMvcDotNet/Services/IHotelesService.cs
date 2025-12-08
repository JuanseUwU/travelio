using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Servicio para gestionar habitaciones de hoteles usando TravelioAPIConnector.
/// </summary>
public interface IHotelesService
{
    /// <summary>
    /// Busca habitaciones en todos los proveedores activos.
    /// </summary>
    Task<HabitacionesSearchViewModel> BuscarHabitacionesAsync(HabitacionesSearchViewModel filtros);

    /// <summary>
    /// Obtiene los detalles de una habitación específica.
    /// </summary>
    Task<HabitacionDetalleViewModel?> ObtenerHabitacionAsync(int servicioId, string idHabitacion);

    /// <summary>
    /// Verifica la disponibilidad de una habitación en un rango de fechas.
    /// </summary>
    Task<bool> VerificarDisponibilidadAsync(int servicioId, string idHabitacion, DateTime fechaInicio, DateTime fechaFin);

    /// <summary>
    /// Crea una prerreserva (hold) para una habitación.
    /// </summary>
    Task<(bool exito, string mensaje, string? holdId)> CrearPrerreservaAsync(
        int servicioId, 
        string idHabitacion, 
        DateTime fechaInicio, 
        DateTime fechaFin,
        int numeroHuespedes,
        decimal precioActual);
}
