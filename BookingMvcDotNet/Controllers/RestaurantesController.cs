using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingMvcDotNet.Controllers;

public class RestaurantesController : Controller
{
    private readonly IRestaurantesService _restaurantesService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public RestaurantesController(IRestaurantesService restaurantesService)
    {
        _restaurantesService = restaurantesService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? capacidad,
        string? tipoMesa,
        DateTime? fecha,
        int numeroPersonas = 2)
    {
        var filtros = new MesasSearchViewModel
        {
            Capacidad = capacidad,
            TipoMesa = tipoMesa,
            Fecha = fecha,
            NumeroPersonas = numeroPersonas
        };

        var resultado = await _restaurantesService.BuscarMesasAsync(filtros);
        return View(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int servicioId, int idMesa, DateTime? fecha, int personas = 2)
    {
        var mesa = await _restaurantesService.ObtenerMesaAsync(servicioId, idMesa);
        
        if (mesa == null)
            return NotFound();

        mesa.FechaReserva = fecha ?? DateTime.Today.AddDays(1);
        mesa.NumeroPersonas = personas;
        return View(mesa);
    }

    [HttpPost]
    public async Task<IActionResult> AgregarAlCarrito(
        int servicioId,
        int idMesa,
        DateTime fecha,
        int personas)
    {
        var mesa = await _restaurantesService.ObtenerMesaAsync(servicioId, idMesa);
        
        if (mesa == null)
            return Json(new { success = false, message = "Mesa no encontrada" });

        var disponible = await _restaurantesService.VerificarDisponibilidadAsync(servicioId, idMesa, fecha, personas);

        if (!disponible)
            return Json(new { success = false, message = "Mesa no disponible para esa fecha" });

        var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY) 
            ?? new List<CartItemViewModel>();

        var existente = cart.FirstOrDefault(x => 
            x.Tipo == "RESTAURANT" && 
            x.IdProducto == idMesa.ToString() && 
            x.ServicioId == servicioId);

        if (existente != null)
        {
            return Json(new { success = false, message = "Esta mesa ya está en tu carrito" });
        }

        cart.Add(new CartItemViewModel
        {
            Tipo = "RESTAURANT",
            IdProducto = idMesa.ToString(),
            ServicioId = servicioId,
            Titulo = $"Mesa {mesa.NumeroMesa} - {mesa.TipoMesa}",
            Detalle = $"{mesa.NombreProveedor} | {personas} personas",
            ImagenUrl = mesa.ImagenUrl,
            PrecioOriginal = mesa.Precio,
            PrecioFinal = mesa.Precio,
            PrecioUnitario = mesa.Precio,
            Cantidad = 1,
            FechaInicio = fecha,
            FechaFin = fecha,
            NumeroPersonas = personas,
            UnidadPrecio = "por reserva"
        });

        HttpContext.Session.Set(CART_SESSION_KEY, cart);

        return Json(new { 
            success = true, 
            message = "Mesa agregada al carrito",
            totalCount = cart.Sum(x => x.Cantidad)
        });
    }
}
