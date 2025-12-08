using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

public interface IRestaurantesService
{
    Task<MesasSearchViewModel> BuscarMesasAsync(MesasSearchViewModel filtros);
    Task<MesaDetalleViewModel?> ObtenerMesaAsync(int servicioId, int idMesa);
    Task<bool> VerificarDisponibilidadAsync(int servicioId, int idMesa, DateTime fecha, int personas);
}
