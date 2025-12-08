using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingMvcDotNet.Controllers;

public class PaquetesController : Controller
{
    private readonly IPaquetesService _paquetesService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public PaquetesController(IPaquetesService paquetesService)
    {
        _paquetesService = paquetesService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? ciudad,
        DateTime? fechaInicio,
        string? tipoActividad,
        decimal? precioMax,
        int personas = 1)
    {
        var filtros = new PaquetesSearchViewModel
        {
            Ciudad = ciudad,
            FechaInicio = fechaInicio,
            TipoActividad = tipoActividad,
            PrecioMax = precioMax,
            Personas = personas
        };

        var resultado = await _paquetesService.BuscarPaquetesAsync(filtros);
        return View(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int servicioId, string idPaquete, DateTime? fechaInicio, int personas = 1)
    {
        var paquete = await _paquetesService.ObtenerPaqueteAsync(servicioId, idPaquete);
        
        if (paquete == null)
            return NotFound();

        paquete.FechaInicio = fechaInicio ?? DateTime.Today.AddDays(7);
        paquete.NumeroPersonas = personas;
        return View(paquete);
    }

    [HttpPost]
    public async Task<IActionResult> AgregarAlCarrito(
        int servicioId,
        string idPaquete,
        DateTime fechaInicio,
        int personas)
    {
        var paquete = await _paquetesService.ObtenerPaqueteAsync(servicioId, idPaquete);
        
        if (paquete == null)
            return Json(new { success = false, message = "Paquete no encontrado" });

        var disponible = await _paquetesService.VerificarDisponibilidadAsync(servicioId, idPaquete, fechaInicio, personas);

        if (!disponible)
            return Json(new { success = false, message = "Paquete no disponible para esa fecha" });

        var precioTotal = paquete.PrecioActual * personas;
        var fechaFin = fechaInicio.AddDays(paquete.Duracion);

        var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY) 
            ?? new List<CartItemViewModel>();

        var existente = cart.FirstOrDefault(x => 
            x.Tipo == "PACKAGE" && 
            x.IdProducto == idPaquete && 
            x.ServicioId == servicioId);

        if (existente != null)
        {
            return Json(new { success = false, message = "Este paquete ya está en tu carrito" });
        }

        cart.Add(new CartItemViewModel
        {
            Tipo = "PACKAGE",
            IdProducto = idPaquete,
            ServicioId = servicioId,
            Titulo = paquete.Nombre,
            Detalle = $"{paquete.Ciudad}, {paquete.Pais} | {paquete.TipoActividad} | {paquete.Duracion} días",
            ImagenUrl = paquete.ImagenUrl,
            PrecioOriginal = paquete.PrecioNormal * personas,
            PrecioFinal = precioTotal,
            PrecioUnitario = paquete.PrecioActual,
            Cantidad = 1,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            NumeroPersonas = personas,
            UnidadPrecio = $"({personas} personas)"
        });

        HttpContext.Session.Set(CART_SESSION_KEY, cart);

        return Json(new { 
            success = true, 
            message = "Paquete agregado al carrito",
            totalCount = cart.Sum(x => x.Cantidad)
        });
    }
}
