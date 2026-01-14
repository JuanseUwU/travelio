using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TravelioIntegrator.Models.Carrito;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Controllers;

/// <summary>
/// Controlador para el módulo de renta de autos.
/// Conecta con 6 proveedores SOAP/REST: Cuenca Wheels, LojitaGO, EasyCar, Auto Car Rent, RentaAutosGYE, UrbanDrive NY
/// </summary>
public class AutosController(IAutosService autosService, TravelioIntegrationService integrationService, ILogger<AutosController> logger) : Controller
{
    private const string CART_SESSION_KEY = "MyCartSession";

    /// <summary>
    /// Página principal de búsqueda de autos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(AutosSearchViewModel filtros)
    {
        var resultado = await autosService.BuscarAutosAsync(filtros);
        return View(resultado);
    }

    /// <summary>
    /// Detalle de un auto específico.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detalle(int servicioId, string idAuto, DateTime? fechaInicio, DateTime? fechaFin)
    {
        var auto = await autosService.ObtenerAutoAsync(servicioId, idAuto);

        if (auto == null)
            return RedirectToAction("Index");

        auto.FechaInicio = fechaInicio ?? DateTime.Today.AddDays(1);
        auto.FechaFin = fechaFin ?? DateTime.Today.AddDays(4);

        // Verificar disponibilidad si hay fechas
        if (fechaInicio.HasValue && fechaFin.HasValue)
        {
            auto.Disponible = await autosService.VerificarDisponibilidadAsync(
                servicioId, idAuto, fechaInicio.Value, fechaFin.Value);
        }

        return View(auto);
    }

    /// <summary>
    /// Verificar disponibilidad de un auto (AJAX).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> VerificarDisponibilidad(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var disponible = await autosService.VerificarDisponibilidadAsync(servicioId, idAuto, fechaInicio, fechaFin);
            return Json(new { success = true, disponible });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad");
            return Json(new { success = false, message = "Error verificando disponibilidad" });
        }
    }

    /// <summary>
    /// Agregar auto al carrito (AJAX).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AgregarAlCarrito(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var auto = await autosService.ObtenerAutoAsync(servicioId, idAuto);
            if (auto == null)
                return Json(new { success = false, message = "Vehículo no encontrado" });

            // Verificar disponibilidad
            var disponible = await autosService.VerificarDisponibilidadAsync(servicioId, idAuto, fechaInicio, fechaFin);
            if (!disponible)
                return Json(new { success = false, message = "El vehículo no está disponible en esas fechas" });

            var dias = Math.Max(1, (int)(fechaFin - fechaInicio).TotalDays);

            var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY) ?? [];

            // Verificar si ya existe en el carrito (por IdProducto y ServicioId)
            var existente = cart.FirstOrDefault(x => 
                x.IdProducto == idAuto && x.ServicioId == servicioId && x.Tipo == "CAR");
            
            if (existente != null)
            {
                return Json(new { success = false, message = "Este vehiculo ya esta en tu carrito" });
            }

            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            var clienteIdValue = clienteId.GetValueOrDefault();
            var guardarEnDb = clienteIdValue > 0;
            if (guardarEnDb)
            {
                var existentesDb = await integrationService.ObtenerCarritoAutosAsync(clienteIdValue);
                if (existentesDb?.Any(x =>
                        x.ServicioId == servicioId &&
                        x.IdAutoProveedor == idAuto &&
                        x.FechaInicio.Date == fechaInicio.Date &&
                        x.FechaFin.Date == fechaFin.Date) == true)
                {
                    return Json(new { success = false, message = "Este vehiculo ya esta en tu carrito" });
                }
            }

            var nuevo = new CartItemViewModel
            {
                Tipo = "CAR",
                ServicioId = servicioId,
                IdProducto = idAuto,
                Titulo = $"{auto.Tipo} - {auto.NombreProveedor}",
                Detalle = $"{auto.Ciudad}, {auto.Pais} | {fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy} ({dias} dias)",
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                ImagenUrl = auto.UriImagen,
                PrecioOriginal = auto.PrecioNormalPorDia * dias,
                PrecioFinal = auto.PrecioActualPorDia * dias,
                PrecioUnitario = auto.PrecioActualPorDia,
                UnidadPrecio = "por dia",
                Cantidad = 1
            };

            if (guardarEnDb)
            {
                var request = new AutoCarritoRequest(
                    clienteIdValue,
                    servicioId,
                    idAuto,
                    auto.Tipo,
                    null,
                    null,
                    auto.CapacidadPasajeros,
                    auto.PrecioNormalPorDia,
                    auto.PrecioActualPorDia,
                    auto.DescuentoPorcentaje,
                    string.IsNullOrWhiteSpace(auto.UriImagen) ? null : auto.UriImagen,
                    auto.Ciudad,
                    auto.Pais,
                    fechaInicio,
                    fechaFin);

                var dbItem = await integrationService.AgregarAutoACarritoAsync(request, logger);
                if (dbItem == null)
                {
                    return Json(new { success = false, message = "No se pudo guardar el vehiculo en tu carrito" });
                }

                nuevo.CarritoItemId = dbItem.Id;
            }

            cart.Add(nuevo);

            HttpContext.Session.Set(CART_SESSION_KEY, cart);

            return Json(new
            {
                success = true,
                message = "Vehículo agregado al carrito",
                totalCount = cart.Sum(x => x.Cantidad)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error agregando auto al carrito");
            return Json(new { success = false, message = "Error al agregar al carrito" });
        }
    }
}
