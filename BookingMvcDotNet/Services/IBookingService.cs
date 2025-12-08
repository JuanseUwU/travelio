using System;
using System.Threading.Tasks;
using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services
{
    /// <summary>
    /// Contrato del servicio de booking para el proyecto SOAP.
    /// Se expone la misma API lógica que en el proyecto REST.
    /// </summary>
    public interface IBookingService
    {
        // =================================
        // MÉTODOS DE LECTURA (GET)
        // =================================

        /// <summary>
        /// Obtiene datos para la página de inicio (servicios recomendados).
        /// </summary>
        Task<ServiceResponse<ResultsViewModel>> GetHomeAsync();

        /// <summary>
        /// Búsqueda general de servicios (hoteles, autos, vuelos, restaurantes).
        /// </summary>
        Task<ServiceResponse<ResultsViewModel>> BuscarAsync(
            string? q,
            string? tipo,
            DateTime? checkIn,
            DateTime? checkOut);

        /// <summary>
        /// Obtiene el detalle de un servicio concreto por tipo y título.
        /// </summary>
        Task<ServiceResponse<ServiceDetailViewModel>> ObtenerDetalleAsync(
            string tipo,
            string titulo);

        // =================================
        // MÉTODOS DE ESCRITURA (POST)
        // =================================

        /// <summary>
        /// Envía credenciales a la API para obtener el usuario autenticado.
        /// </summary>
        Task<ServiceResponse<UserViewModel>> LoginAsync(LoginViewModel model);

        /// <summary>
        /// Envía datos de registro de usuario a la API.
        /// </summary>
        Task<ServiceResponse<bool>> RegisterAsync(RegisterViewModel model);

        /// <summary>
        /// Envía la orden de compra finalizada a la API.
        /// </summary>
        Task<ServiceResponse<bool>> CreateOrderAsync(OrderViewModel order);
    }
}
