using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TravelioIntegrator.Models.Carrito;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Controllers;

public class RestaurantesController : Controller
{
    private readonly IRestaurantesService _restaurantesService;
    private readonly TravelioIntegrationService _integrationService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public RestaurantesController(IRestaurantesService restaurantesService, TravelioIntegrationService integrationService)
    {
        _restaurantesService = restaurantesService;
        _integrationService = integrationService;
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
            return Json(new { success = false, message = "Esta mesa ya esta en tu carrito" });
        }

        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        var clienteIdValue = clienteId.GetValueOrDefault();
        var guardarEnDb = clienteIdValue > 0;
        if (guardarEnDb)
        {
            var existentesDb = await _integrationService.ObtenerCarritoMesasAsync(clienteIdValue);
            if (existentesDb?.Any(x =>
                    x.ServicioId == servicioId &&
                    x.IdMesa == idMesa &&
                    x.FechaReserva.Date == fecha.Date &&
                    x.NumeroPersonas == personas) == true)
            {
                return Json(new { success = false, message = "Esta mesa ya esta en tu carrito" });
            }
        }

        var nuevo = new CartItemViewModel
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
        };

        if (guardarEnDb)
        {
            var request = new MesaCarritoRequest(
                clienteIdValue,
                servicioId,
                idMesa,
                mesa.IdRestaurante,
                mesa.NumeroMesa,
                mesa.TipoMesa,
                mesa.Capacidad,
                mesa.Precio,
                string.IsNullOrWhiteSpace(mesa.ImagenUrl) ? null : mesa.ImagenUrl,
                mesa.Estado,
                fecha,
                personas);

            var dbItem = await _integrationService.AgregarMesaACarritoAsync(request);
            if (dbItem == null)
            {
                return Json(new { success = false, message = "No se pudo guardar la mesa en tu carrito" });
            }

            nuevo.CarritoItemId = dbItem.Id;
        }

        cart.Add(nuevo);

        HttpContext.Session.Set(CART_SESSION_KEY, cart);

        return Json(new { 
            success = true, 
            message = "Mesa agregada al carrito",
            totalCount = cart.Sum(x => x.Cantidad)
        });
    }
}
