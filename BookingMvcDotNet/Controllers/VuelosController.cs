using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TravelioIntegrator.Models.Carrito;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Controllers;

public class VuelosController : Controller
{
    private readonly IVuelosService _vuelosService;
    private readonly TravelioIntegrationService _integrationService;
    private const string CART_SESSION_KEY = "MyCartSession";

    public VuelosController(IVuelosService vuelosService, TravelioIntegrationService integrationService)
    {
        _vuelosService = vuelosService;
        _integrationService = integrationService;
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
            return Json(new { success = false, message = "Este vuelo ya esta en tu carrito" });
        }

        var clienteId = HttpContext.Session.GetInt32("ClienteId");
        var clienteIdValue = clienteId.GetValueOrDefault();
        var guardarEnDb = clienteIdValue > 0;
        if (guardarEnDb)
        {
            var existentesDb = await _integrationService.ObtenerCarritoVuelosAsync(clienteIdValue);
            if (existentesDb?.Any(x =>
                    x.ServicioId == servicioId &&
                    x.IdVueloProveedor == idVuelo &&
                    x.FechaVuelo.Date == vuelo.Fecha.Date &&
                    x.CantidadPasajeros == pasajeros &&
                    x.TipoCabina == vuelo.TipoCabina) == true)
            {
                return Json(new { success = false, message = "Este vuelo ya esta en tu carrito" });
            }
        }

        var nuevo = new CartItemViewModel
        {
            Tipo = "FLIGHT",
            IdProducto = idVuelo,
            ServicioId = servicioId,
            Titulo = $"{vuelo.Origen} - {vuelo.Destino}",
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
        };

        if (guardarEnDb)
        {
            var request = new AerolineaCarritoRequest(
                clienteIdValue,
                servicioId,
                idVuelo,
                vuelo.Origen,
                vuelo.Destino,
                vuelo.Fecha,
                vuelo.TipoCabina,
                vuelo.NombreAerolinea,
                vuelo.PrecioNormal,
                vuelo.PrecioActual,
                vuelo.DescuentoPorcentaje,
                pasajeros,
                Array.Empty<AerolineaPasajeroRequest>());

            var dbItem = await _integrationService.AgregarVueloACarritoAsync(request);
            if (dbItem == null)
            {
                return Json(new { success = false, message = "No se pudo guardar el vuelo en tu carrito" });
            }

            nuevo.CarritoItemId = dbItem.Id;
        }

        cart.Add(nuevo);

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
