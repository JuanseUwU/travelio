using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingMvcDotNet.Controllers;

public class VuelosController : Controller
{
    private readonly IVuelosService _vuelosService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public VuelosController(IVuelosService vuelosService)
    {
        _vuelosService = vuelosService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? origen,
        string? destino,
        DateTime? fechaSalida,
        string? tipoCabina,
        int pasajeros = 1,
        decimal? precioMin = null,
        decimal? precioMax = null)
    {
        var filtros = new VuelosSearchViewModel
        {
            Origen = origen,
            Destino = destino,
            FechaSalida = fechaSalida,
            TipoCabina = tipoCabina,
            Pasajeros = pasajeros,
            PrecioMin = precioMin,
            PrecioMax = precioMax
        };

        var resultado = await _vuelosService.BuscarVuelosAsync(filtros);
        return View(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int servicioId, string idVuelo, int pasajeros = 1)
    {
        var vuelo = await _vuelosService.ObtenerVueloAsync(servicioId, idVuelo);
        
        if (vuelo == null)
            return NotFound();

        vuelo.NumeroPasajeros = pasajeros;
        return View(vuelo);
    }

    [HttpPost]
    public async Task<IActionResult> AgregarAlCarrito(
        int servicioId,
        string idVuelo,
        int pasajeros)
    {
        var vuelo = await _vuelosService.ObtenerVueloAsync(servicioId, idVuelo);
        
        if (vuelo == null)
            return Json(new { success = false, message = "Vuelo no encontrado" });

        var disponible = await _vuelosService.VerificarDisponibilidadAsync(servicioId, idVuelo, pasajeros);

        if (!disponible)
            return Json(new { success = false, message = "No hay suficientes asientos disponibles" });

        var precioTotal = vuelo.PrecioActual * pasajeros;

        var cart = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY) 
            ?? new List<CartItemViewModel>();

        var existente = cart.FirstOrDefault(x => 
            x.Tipo == "FLIGHT" && 
            x.IdProducto == idVuelo && 
            x.ServicioId == servicioId);

        if (existente != null)
        {
            return Json(new { success = false, message = "Este vuelo ya está en tu carrito" });
        }

        cart.Add(new CartItemViewModel
        {
            Tipo = "FLIGHT",
            IdProducto = idVuelo,
            ServicioId = servicioId,
            Titulo = $"{vuelo.Origen} ? {vuelo.Destino}",
            Detalle = $"{vuelo.NombreAerolinea} | {vuelo.TipoCabina} | {pasajeros} pasajero(s)",
            ImagenUrl = null,
            PrecioOriginal = vuelo.PrecioNormal * pasajeros,
            PrecioFinal = precioTotal,
            PrecioUnitario = vuelo.PrecioActual,
            Cantidad = 1,
            FechaInicio = vuelo.Fecha,
            FechaFin = vuelo.Fecha,
            NumeroPersonas = pasajeros,
            UnidadPrecio = $"({pasajeros} pasajeros)"
        });

        HttpContext.Session.Set(CART_SESSION_KEY, cart);

        return Json(new { 
            success = true, 
            message = "Vuelo agregado al carrito",
            totalCount = cart.Sum(x => x.Cantidad)
        });
    }

    [HttpPost]
    public async Task<IActionResult> VerificarDisponibilidad(int servicioId, string idVuelo, int pasajeros)
    {
        var disponible = await _vuelosService.VerificarDisponibilidadAsync(servicioId, idVuelo, pasajeros);
        return Json(new { disponible });
    }
}
