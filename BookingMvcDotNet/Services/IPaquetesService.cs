using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

public interface IPaquetesService
{
    Task<PaquetesSearchViewModel> BuscarPaquetesAsync(PaquetesSearchViewModel filtros);
    Task<PaqueteDetalleViewModel?> ObtenerPaqueteAsync(int servicioId, string idPaquete);
    Task<bool> VerificarDisponibilidadAsync(int servicioId, string idPaquete, DateTime fechaInicio, int personas);
}
