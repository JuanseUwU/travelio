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
        // null = unknown, true = available, false = not available
        private bool? _remoteAvailable = null;

        public BookingService(HttpClient httpClient, ILogger<BookingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private async Task<bool> IsRemoteAvailableAsync()
        {
            if (_remoteAvailable.HasValue) return _remoteAvailable.Value;

            if (_httpClient.BaseAddress == null)
            {
                _remoteAvailable = false;
                return false;
            }

            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
                var ping = new HttpRequestMessage(HttpMethod.Get, "");
                var resp = await _httpClient.SendAsync(ping, cts.Token);
                _remoteAvailable = resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Remote booking API not available at {BaseAddress}", _httpClient.BaseAddress);
                _remoteAvailable = false;
            }

            return _remoteAvailable.Value;
        }

        // =====================================================
        // 1. HOME
        // =====================================================
        public async Task<ServiceResponse<ResultsViewModel>> GetHomeAsync()
        {
            try
            {
                // Si la API remota no está disponible, devolvemos datos mock locales para evitar errores en la UI.
                if (!await IsRemoteAvailableAsync())
                {
                    var mock = new ResultsViewModel
                    {
                        Query = null,
                        Items = new System.Collections.Generic.List<ResultItemViewModel>
                        {
                            new ResultItemViewModel { Tipo = "HOTEL", Titulo = "Hotel Campestre", Ciudad = "Cuenca", Precio = 45, Rating = 4.6, UnidadPrecio = "por noche" },
                            new ResultItemViewModel { Tipo = "CAR", Titulo = "Kia Sportage SUV", Ciudad = "Cuenca", Precio = 20, Rating = 4.4, UnidadPrecio = "/día" },
                            new ResultItemViewModel { Tipo = "FLIGHT", Titulo = "Withfly - Quito", Ciudad = "Quito", Precio = 120, Rating = 4.7, UnidadPrecio = "por tramo" }
                        }
                    };

                    return new ServiceResponse<ResultsViewModel>
                    {
                        Success = true,
                        StatusCode = 200,
                        Data = mock
                    };
                }

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

                // Si la API remota no está disponible, devolvemos un resultado vacío en lugar de lanzar.
                if (!await IsRemoteAvailableAsync())
                {
                    return new ServiceResponse<ResultsViewModel>
                    {
                        Success = true,
                        StatusCode = 200,
                        Data = new ResultsViewModel { Query = q, Tipo = tipo, CheckIn = checkIn, CheckOut = checkOut, Items = new System.Collections.Generic.List<ResultItemViewModel>() }
                    };
                }

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
