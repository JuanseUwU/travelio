using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingMvcDotNet.Controllers;

/// <summary>
/// Controlador para el módulo de Hoteles/Habitaciones.
/// </summary>
public class HotelesController : Controller
{
    private readonly IHotelesService _hotelesService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public HotelesController(IHotelesService hotelesService)
    {
        _hotelesService = hotelesService;
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

        // Verificar si ya existe esta habitación en el carrito
        var existente = cart.FirstOrDefault(x => 
            x.Tipo == "HOTEL" && 
            x.IdProducto == idHabitacion && 
            x.ServicioId == servicioId);

        if (existente != null)
        {
            return Json(new { success = false, message = "Esta habitación ya está en tu carrito" });
        }

        // Agregar al carrito
        cart.Add(new CartItemViewModel
        {
            Tipo = "HOTEL",
            IdProducto = idHabitacion,
            ServicioId = servicioId,
            Titulo = $"{habitacion.Hotel} - {habitacion.NombreHabitacion}",
            Detalle = $"{habitacion.Ciudad}, {habitacion.Pais} | {habitacion.TipoHabitacion} | {numeroHuespedes} huéspedes",
            ImagenUrl = habitacion.ImagenPrincipal,
            PrecioOriginal = habitacion.PrecioNormal * noches,
            PrecioFinal = precioTotal,
            PrecioUnitario = habitacion.PrecioActual,
            Cantidad = 1,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            NumeroPersonas = numeroHuespedes,
            UnidadPrecio = $"({noches} noches)"
        });

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
