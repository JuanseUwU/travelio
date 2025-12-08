using System;
using System.Net.Http;
using System.Net.Http.Json;           // Para PostAsJsonAsync y ReadFromJsonAsync
using System.Threading.Tasks;
using BookingMvcDotNet.Models;
using Microsoft.Extensions.Logging;

namespace BookingMvcDotNet.Services
{
    public class BookingService : IBookingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BookingService> _logger;

        public BookingService(HttpClient httpClient, ILogger<BookingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        // =====================================================
        // 1. HOME
        // =====================================================
        public async Task<ServiceResponse<ResultsViewModel>> GetHomeAsync()
        {
            try
            {
                // Ajusta el endpoint a lo que tengas del lado SOAP / API
                // Ejemplo: GET /api/home
                var response = await _httpClient.GetAsync("api/home");
                return await ProcessResponse<ResultsViewModel>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetHomeAsync");
                return new ServiceResponse<ResultsViewModel>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Error de conexión."
                };
            }
        }

        // =====================================================
        // 2. BUSCAR
        // =====================================================
        public async Task<ServiceResponse<ResultsViewModel>> BuscarAsync(
            string? q,
            string? tipo,
            DateTime? checkIn,
            DateTime? checkOut)
        {
            try
            {
                var query = System.Web.HttpUtility.ParseQueryString(string.Empty);

                if (!string.IsNullOrEmpty(q)) query["q"] = q;
                if (!string.IsNullOrEmpty(tipo)) query["tipo"] = tipo;
                if (checkIn.HasValue) query["checkIn"] = checkIn.Value.ToString("yyyy-MM-dd");
                if (checkOut.HasValue) query["checkOut"] = checkOut.Value.ToString("yyyy-MM-dd");

                // Ejemplo: GET /api/search?...
                var response = await _httpClient.GetAsync($"api/search?{query}");
                return await ProcessResponse<ResultsViewModel>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en BuscarAsync");
                return new ServiceResponse<ResultsViewModel>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }

        // =====================================================
        // 3. DETALLE
        // =====================================================
        public async Task<ServiceResponse<ServiceDetailViewModel>> ObtenerDetalleAsync(
            string tipo,
            string titulo)
        {
            try
            {
                // Ejemplo: GET /api/services/{tipo}/{titulo}
                var response = await _httpClient.GetAsync($"api/services/{tipo}/{titulo}");
                return await ProcessResponse<ServiceDetailViewModel>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ObtenerDetalleAsync");
                return new ServiceResponse<ServiceDetailViewModel>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Error interno."
                };
            }
        }

        // =====================================================
        // 4. LOGIN (POST)
        // =====================================================
        public async Task<ServiceResponse<UserViewModel>> LoginAsync(LoginViewModel model)
        {
            try
            {
                // Ejemplo: POST /api/auth/login
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);
                return await ProcessResponse<UserViewModel>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoginAsync");
                return new ServiceResponse<UserViewModel>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Error de conexión."
                };
            }
        }

        // =====================================================
        // 5. REGISTER (POST)
        // =====================================================
        public async Task<ServiceResponse<bool>> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                // Ejemplo: POST /api/auth/register
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", model);

                return new ServiceResponse<bool>
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Data = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RegisterAsync");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Error de conexión."
                };
            }
        }

        // =====================================================
        // 6. CREAR ORDEN (POST)
        // =====================================================
        public async Task<ServiceResponse<bool>> CreateOrderAsync(OrderViewModel order)
        {
            try
            {
                // Ejemplo: POST /api/orders
                var response = await _httpClient.PostAsJsonAsync("api/orders", order);

                return new ServiceResponse<bool>
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Data = response.IsSuccessStatusCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateOrderAsync");
                return new ServiceResponse<bool>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Error enviando orden."
                };
            }
        }

        // =====================================================
        // HELPER GENÉRICO
        // =====================================================
        private async Task<ServiceResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
        {
            var result = new ServiceResponse<T>
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    result.Data = default!;
                }
                else
                {
                    try
                    {
                        var data = await response.Content.ReadFromJsonAsync<T>();
                        result.Data = data!;
                    }
                    catch
                    {
                        // Error de deserialización: se deja Data con su valor por defecto.
                        result.Data = default!;
                    }
                }
            }
            else
            {
                result.Message = "Error en la solicitud.";
            }

            return result;
        }
    }
}
