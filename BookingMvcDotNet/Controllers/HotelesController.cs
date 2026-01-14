using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TravelioIntegrator.Models.Carrito;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Controllers;

/// <summary>
/// Controlador para el módulo de Hoteles/Habitaciones.
/// </summary>
public class HotelesController : Controller
{
    private readonly IHotelesService _hotelesService;
    private readonly TravelioIntegrationService _integrationService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public HotelesController(IHotelesService hotelesService, TravelioIntegrationService integrationService)
    {
        _hotelesService = hotelesService;
        _integrationService = integrationService;
    }

    /// <summary>
    /// Página principal de búsqueda de habitaciones.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? ciudad,
        string? tipoHabitacion,
        int? capacidad,
        decimal? precioMin,
        decimal? precioMax,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        int numeroHuespedes = 2)
    {
        var filtros = new HabitacionesSearchViewModel
        {
            Ciudad = ciudad,
            TipoHabitacion = tipoHabitacion,
            Capacidad = capacidad,
            PrecioMin = precioMin,
            PrecioMax = precioMax,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            NumeroHuespedes = numeroHuespedes
        };

        var resultado = await _hotelesService.BuscarHabitacionesAsync(filtros);
        return View(resultado);
    }

    /// <summary>
    /// Página de detalle de una habitación.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detalle(
        int servicioId, 
        string idHabitacion,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        int numeroHuespedes = 2)
    {
        var habitacion = await _hotelesService.ObtenerHabitacionAsync(servicioId, idHabitacion);
        
        if (habitacion == null)
            return NotFound();

        habitacion.FechaInicio = fechaInicio;
        habitacion.FechaFin = fechaFin;
        habitacion.NumeroHuespedes = numeroHuespedes;

        return View(habitacion);
    }

    /// <summary>
    /// API para agregar una habitación al carrito.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AgregarAlCarrito(
        int servicioId,
        string idHabitacion,
        DateTime fechaInicio,
        DateTime fechaFin,
        int numeroHuespedes)
    {
        var habitacion = await _hotelesService.ObtenerHabitacionAsync(servicioId, idHabitacion);
        
        if (habitacion == null)
            return Json(new { success = false, message = "Habitación no encontrada" });

        // Verificar disponibilidad
        var disponible = await _hotelesService.VerificarDisponibilidadAsync(
            servicioId, idHabitacion, fechaInicio, fechaFin);

        if (!disponible)
            return Json(new { success = false, message = "La habitación no está disponible para las fechas seleccionadas" });

        // Calcular precio total
        var noches = Math.Max(1, (fechaFin - fechaInicio).Days);
        var precioTotal = habitacion.PrecioActual * noches;

        // Obtener carrito actual
        var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY) 
            ?? new List<CartItemViewModel>();

        // Verificar si ya existe esta habitacion en el carrito
        var existente = cart.FirstOrDefault(x => 
            x.Tipo == "HOTEL" && 
            x.IdProducto == idHabitacion && 
            x.ServicioId == servicioId);

        if (existente != null)
        {
            return Json(new { success = false, message = "Esta habitacion ya esta en tu carrito" });
        }

        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        var clienteIdValue = clienteId.GetValueOrDefault();
        var guardarEnDb = clienteIdValue > 0;
        if (guardarEnDb)
        {
            var existentesDb = await _integrationService.ObtenerCarritoHabitacionesAsync(clienteIdValue);
            if (existentesDb?.Any(x =>
                    x.ServicioId == servicioId &&
                    x.IdHabitacionProveedor == idHabitacion &&
                    x.FechaInicio.Date == fechaInicio.Date &&
                    x.FechaFin.Date == fechaFin.Date &&
                    x.NumeroHuespedes == numeroHuespedes) == true)
            {
                return Json(new { success = false, message = "Esta habitacion ya esta en tu carrito" });
            }
        }

        var nuevo = new CartItemViewModel
        {
            Tipo = "HOTEL",
            IdProducto = idHabitacion,
            ServicioId = servicioId,
            Titulo = $"{habitacion.Hotel} - {habitacion.NombreHabitacion}",
            Detalle = $"{habitacion.Ciudad}, {habitacion.Pais} | {habitacion.TipoHabitacion} | {numeroHuespedes} huespedes",
            ImagenUrl = habitacion.ImagenPrincipal,
            PrecioOriginal = habitacion.PrecioNormal * noches,
            PrecioFinal = precioTotal,
            PrecioUnitario = habitacion.PrecioActual,
            Cantidad = 1,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            NumeroPersonas = numeroHuespedes,
            UnidadPrecio = $"({noches} noches)"
        };

        if (guardarEnDb)
        {
            var imagenes = habitacion.Imagenes.Length > 0 ? string.Join('|', habitacion.Imagenes) : null;
            var request = new HabitacionCarritoRequest(
                clienteIdValue,
                servicioId,
                idHabitacion,
                habitacion.NombreHabitacion,
                habitacion.TipoHabitacion,
                habitacion.Hotel,
                habitacion.Ciudad,
                habitacion.Pais,
                habitacion.Capacidad,
                habitacion.PrecioNormal,
                habitacion.PrecioActual,
                habitacion.PrecioActual,
                habitacion.Amenidades,
                imagenes,
                fechaInicio,
                fechaFin,
                numeroHuespedes);

            var dbItem = await _integrationService.AgregarHabitacionACarritoAsync(request);
            if (dbItem == null)
            {
                return Json(new { success = false, message = "No se pudo guardar la habitacion en tu carrito" });
            }

            nuevo.CarritoItemId = dbItem.Id;
        }

        // Agregar al carrito
        cart.Add(nuevo);

        HttpContext.Session.Set(CART_SESSION_KEY, cart);

        return Json(new { 
            success = true, 
            message = "Habitación agregada al carrito",
            totalCount = cart.Sum(x => x.Cantidad)
        });
    }

    /// <summary>
    /// API para verificar disponibilidad de una habitación.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> VerificarDisponibilidad(
        int servicioId,
        string idHabitacion,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        var disponible = await _hotelesService.VerificarDisponibilidadAsync(
            servicioId, idHabitacion, fechaInicio, fechaFin);

        return Json(new { disponible });
    }
}
