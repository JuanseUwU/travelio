using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

public interface IVuelosService
{
    Task<VuelosSearchViewModel> BuscarVuelosAsync(VuelosSearchViewModel filtros);
    Task<VueloDetalleViewModel?> ObtenerVueloAsync(int servicioId, string idVuelo);
    Task<bool> VerificarDisponibilidadAsync(int servicioId, string idVuelo, int pasajeros);
}
