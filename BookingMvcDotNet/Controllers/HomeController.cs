using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;

namespace BookingMvcDotNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IAuthService _authService;
        private readonly ICheckoutService _checkoutService;
        private readonly TravelioDbContext _dbContext;
        private const string CART_SESSION_KEY = "MyCartSession";

        public HomeController(IBookingService bookingService, IAuthService authService, ICheckoutService checkoutService, TravelioDbContext dbContext)
        {
            _bookingService = bookingService;
            _authService = authService;
            _checkoutService = checkoutService;
            _dbContext = dbContext;
        }

        // ============================
        //  VISTAS PÚBLICAS
        // ============================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var resp = await _bookingService.GetHomeAsync();
            return View(resp.Data ?? new ResultsViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Results(
            string? q,
            string? tipo,
            DateTime? checkIn,
            DateTime? checkOut,
            decimal? minPrice,
            decimal? maxPrice,
            double? minRating,
            string? sortBy)
        {
            var resp = await _bookingService.BuscarAsync(q, tipo, checkIn, checkOut);

            var vm = new ResultsViewModel
            {
                Query = q,
                Tipo = tipo,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Items = new List<ResultItemViewModel>()
            };

            if (resp.Success && resp.Data?.Items != null)
            {
                IEnumerable<ResultItemViewModel> filteredItems = resp.Data.Items;

                if (minPrice.HasValue)
                    filteredItems = filteredItems.Where(x => x.Precio >= minPrice.Value);
                if (maxPrice.HasValue)
                    filteredItems = filteredItems.Where(x => x.Precio <= maxPrice.Value);
                if (minRating.HasValue)
                    filteredItems = filteredItems.Where(x => x.Rating >= minRating.Value);

                switch (sortBy)
                {
                    case "price_asc":
                        filteredItems = filteredItems.OrderBy(x => x.Precio);
                        break;
                    case "price_desc":
                        filteredItems = filteredItems.OrderByDescending(x => x.Precio);
                        break;
                    case "rating_desc":
                        filteredItems = filteredItems.OrderByDescending(x => x.Rating);
                        break;
                }

                vm.Items = filteredItems.ToList();
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Detalles(string tipo, string titulo)
        {
            var resp = await _bookingService.ObtenerDetalleAsync(tipo, titulo);

            if (!resp.Success || resp.Data == null)
                return View("Error", new ErrorViewModel { RequestId = "404 No encontrado" });

            return View(resp.Data);
        }

        // ============================
        //  AUTH (LOGIN / REGISTER) - Usando TravelioDb
        // ============================

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (exito, mensaje, cliente) = await _authService.LoginAsync(model.Email, model.Password);

            if (!exito || cliente == null)
            {
                ModelState.AddModelError(string.Empty, mensaje);
                return View(model);
            }

            // Guardar datos en sesión
            HttpContext.Session.SetInt32("ClienteId", cliente.Id);
            HttpContext.Session.SetString("UserEmail", cliente.CorreoElectronico);
            HttpContext.Session.SetString("UserName", $"{cliente.Nombre} {cliente.Apellido}");
            HttpContext.Session.SetString("IsAdmin", "False");

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (exito, mensaje, _) = await _authService.RegistrarAsync(model);

            if (!exito)
            {
                ModelState.AddModelError("Email", mensaje);
                return View(model);
            }

            TempData["SuccessMessage"] = "¡Registro exitoso! Ahora puedes iniciar sesión.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        // ============================
        //  PERFIL Y CARRITO
        // ============================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (!clienteId.HasValue) return RedirectToAction("Login");

            var cliente = await _authService.ObtenerClientePorIdAsync(clienteId.Value);
            if (cliente == null) return RedirectToAction("Login");

            // Obtener compras del cliente con sus reservas
            var compras = await _dbContext.Compras
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .Where(c => c.ClienteId == clienteId.Value)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();

            var orders = compras.Select(c => new OrderViewModel
            {
                OrderId = c.Id.ToString(),
                Date = c.FechaCompra,
                Total = c.ValorPagado,
                FacturaTravelioUrl = Url.Action("FacturaTravelio", "Home", new { compraId = c.Id }),
                Items = c.ReservasCompra.Select(rc => {
                    var servicio = rc.Reserva?.Servicio;
                    var tipoServicio = servicio?.TipoServicio.ToString() ?? "Servicio";
                    return new OrderItemViewModel
                    {
                        Tipo = tipoServicio switch
                        {
                            "RentaVehiculos" => "CAR",
                            "Hotel" => "HOTEL",
                            "Aerolinea" => "FLIGHT",
                            _ => "PACKAGE"
                        },
                        Titulo = servicio?.Nombre ?? "Servicio",
                        Cantidad = 1,
                        PrecioUnitario = c.ValorPagado / Math.Max(1, c.ReservasCompra.Count) / 1.12m,
                        CodigoReserva = rc.Reserva?.CodigoReserva ?? "",
                        FacturaProveedorUrl = rc.Reserva?.FacturaUrl
                    };
                }).ToList()
            }).ToList();

            var user = new UserViewModel
            {
                Email = cliente.CorreoElectronico,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                Orders = orders
            };

            return View(user);
        }

        [HttpGet]
        public IActionResult Carrito()
        {
            var cartItems =
                HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            return View(new CartViewModel { Items = cartItems });
        }

        /// <summary>
        /// Vista de checkout con formulario de pago.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (!clienteId.HasValue)
                return RedirectToAction("Login");

            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            if (!cartItems.Any())
                return RedirectToAction("Carrito");

            var cliente = await _authService.ObtenerClientePorIdAsync(clienteId.Value);
            if (cliente == null)
                return RedirectToAction("Login");

            var model = new CheckoutViewModel
            {
                Items = cartItems,
                // Pre-llenar datos del cliente
                NombreCompleto = $"{cliente.Nombre} {cliente.Apellido}",
                TipoDocumento = cliente.TipoIdentificacion,
                NumeroDocumento = cliente.DocumentoIdentidad,
                Correo = cliente.CorreoElectronico
            };

            return View(model);
        }

        /// <summary>
        /// Procesa el pago usando la API del banco y crea las reservas.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcesarPago(CheckoutViewModel model)
        {
            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (!clienteId.HasValue)
                return Json(new { success = false, message = "Debes iniciar sesión." });

            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            if (!cartItems.Any())
                return Json(new { success = false, message = "Carrito vacío." });

            // Validar número de cuenta bancaria
            if (!int.TryParse(model.NumeroCuentaBancaria, out var cuentaBancaria))
                return Json(new { success = false, message = "Número de cuenta bancaria inválido." });

            var datosFacturacion = new DatosFacturacion
            {
                NombreCompleto = model.NombreCompleto,
                TipoDocumento = model.TipoDocumento,
                NumeroDocumento = model.NumeroDocumento,
                Correo = model.Correo
            };

            var resultado = await _checkoutService.ProcesarCheckoutAsync(
                clienteId.Value,
                cuentaBancaria,
                cartItems,
                datosFacturacion
            );

            if (resultado.Exitoso)
            {
                HttpContext.Session.Remove(CART_SESSION_KEY);
                
                // Guardar resultado en TempData para mostrar en la página de confirmación
                TempData["CheckoutExitoso"] = true;
                TempData["CheckoutMensaje"] = resultado.Mensaje;
                TempData["CompraId"] = resultado.CompraId;
                TempData["TotalPagado"] = resultado.TotalPagado.ToString("C");
                
                // Serializar las reservas para mostrar en la confirmación
                var reservasJson = System.Text.Json.JsonSerializer.Serialize(resultado.Reservas);
                TempData["Reservas"] = reservasJson;

                return Json(new { 
                    success = true, 
                    message = resultado.Mensaje,
                    url = Url.Action("ConfirmacionCompra")
                });
            }

            return Json(new { success = false, message = resultado.Mensaje });
        }

        /// <summary>
        /// Página de confirmación después de una compra exitosa.
        /// </summary>
        [HttpGet]
        public IActionResult ConfirmacionCompra()
        {
            if (TempData["CheckoutExitoso"] == null)
                return RedirectToAction("Index");

            ViewBag.Mensaje = TempData["CheckoutMensaje"];
            ViewBag.CompraId = TempData["CompraId"];
            ViewBag.TotalPagado = TempData["TotalPagado"];
            
            // Deserializar las reservas
            var reservasJson = TempData["Reservas"] as string;
            List<ReservaResult>? reservas = null;
            if (!string.IsNullOrEmpty(reservasJson))
            {
                reservas = System.Text.Json.JsonSerializer.Deserialize<List<ReservaResult>>(reservasJson);
            }
            ViewBag.Reservas = reservas ?? new List<ReservaResult>();

            return View();
        }

        /// <summary>
        /// Genera la factura de Travelio para una compra.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FacturaTravelio(int compraId)
        {
            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (!clienteId.HasValue)
                return RedirectToAction("Login");

            // Obtener la compra con sus reservas
            var compra = await _dbContext.Compras
                .Include(c => c.Cliente)
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .FirstOrDefaultAsync(c => c.Id == compraId && c.ClienteId == clienteId.Value);

            if (compra == null)
                return NotFound("Compra no encontrada");

            var cliente = compra.Cliente;

            var factura = new FacturaTravelioViewModel
            {
                CompraId = compraId,
                NumeroFactura = $"TRV-{compra.FechaCompra.Year}-{compraId:D6}",
                FechaEmision = compra.FechaCompra,
                ClienteNombre = $"{cliente.Nombre} {cliente.Apellido}",
                ClienteDocumento = cliente.DocumentoIdentidad,
                ClienteTipoDocumento = cliente.TipoIdentificacion,
                ClienteCorreo = cliente.CorreoElectronico,
                MetodoPago = "Transferencia Bancaria",
                EstadoPago = "Pagado",
                Total = compra.ValorPagado
            };

            // Crear items de la factura basados en las reservas
            factura.Items = compra.ReservasCompra.Select(rc => {
                var servicio = rc.Reserva?.Servicio;
                var tipoServicio = servicio?.TipoServicio.ToString() ?? "Servicio";
                var tipo = tipoServicio switch
                {
                    "RentaVehiculos" => "CAR",
                    "Hotel" => "HOTEL",
                    "Aerolinea" => "FLIGHT",
                    _ => "PACKAGE"
                };

                return new FacturaItemViewModel
                {
                    Descripcion = servicio?.Nombre ?? "Servicio de viaje",
                    Tipo = tipo,
                    CodigoReserva = rc.Reserva?.CodigoReserva ?? "-",
                    Cantidad = 1,
                    PrecioUnitario = 0 // Se calculará del total
                };
            }).ToList();

            // Si no hay items, crear uno genérico
            if (!factura.Items.Any())
            {
                factura.Items.Add(new FacturaItemViewModel
                {
                    Descripcion = "Servicios de viaje",
                    Tipo = "PACKAGE",
                    CodigoReserva = compraId.ToString(),
                    Cantidad = 1,
                    PrecioUnitario = compra.ValorPagado / 1.12m // Quitar IVA
                });
            }

            // Calcular totales (el Total ya viene con IVA incluido)
            factura.PorcentajeIVA = 12m;
            factura.Subtotal = compra.ValorPagado / 1.12m;
            factura.IVA = compra.ValorPagado - factura.Subtotal;

            // Distribuir el subtotal entre los items
            if (factura.Items.Any())
            {
                var precioUnitario = factura.Subtotal / factura.Items.Count;
                foreach (var item in factura.Items)
                {
                    item.PrecioUnitario = precioUnitario;
                }
            }

            return View(factura);
        }

        // ============================
        //  ADMIN
        // ============================

        [HttpGet]
        public IActionResult Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return RedirectToAction("Index");

            // De momento, config dummy (la vista Admin.cshtml usa ViewBag.*)
            ViewBag.IvaPercent = 0;
            ViewBag.PromoDiscount = 0;
            ViewBag.PromoCategories = Array.Empty<string>();

            // Más adelante podrás rellenar con usuarios reales de la API
            return View(new List<UserViewModel>());
        }

        [HttpPost]
        public IActionResult UpdateConfig(int iva, int discount, string categories)
        {
            // Aquí luego podrás llamar a una API de configuración.
            return Ok();
        }

        // ============================
        //  API DEL CARRITO (AJAX)
        // ============================

        [HttpPost]
        public async Task<IActionResult> AgregarAlCarritoApi(string tipo, string titulo)
        {
            var resp = await _bookingService.ObtenerDetalleAsync(tipo, titulo);

            if (!resp.Success || resp.Data == null)
                return NotFound(new { success = false, message = "Producto no encontrado en API" });

            var producto = resp.Data;

            var cart =
                HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            var item = cart.FirstOrDefault(x => x.Tipo == producto.Tipo && x.Titulo == producto.Titulo);

            if (item != null)
            {
                item.Cantidad++;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    Tipo = producto.Tipo,
                    Titulo = producto.Titulo,
                    Detalle = producto.Ciudad,
                    PrecioOriginal = producto.Precio,
                    PrecioFinal = producto.Precio,
                    PrecioUnitario = producto.Precio,
                    Cantidad = 1
                });
            }

            HttpContext.Session.Set(CART_SESSION_KEY, cart);
            return Ok(new { success = true, message = "Añadido", totalCount = cart.Sum(x => x.Cantidad) });
        }

        [HttpPost]
        public IActionResult EliminarDelCarritoApi(string titulo)
        {
            var cart =
                HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            var item = cart.FirstOrDefault(x => x.Titulo == titulo);

            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set(CART_SESSION_KEY, cart);
                return Ok(new { success = true, newTotal = cart.Sum(x => x.Cantidad) });
            }

            return NotFound(new { success = false });
        }

        [HttpPost]
        public IActionResult ActualizarCantidadApi(string titulo, int cantidad)
        {
            var cart =
                HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            var item = cart.FirstOrDefault(x => x.Titulo == titulo);

            if (item != null)
            {
                if (cantidad < 1) cantidad = 1;
                item.Cantidad = cantidad;
                HttpContext.Session.Set(CART_SESSION_KEY, cart);
                return Ok(new { success = true });
            }

            return NotFound();
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart =
                HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            return Ok(new { count = cart.Sum(x => x.Cantidad) });
        }

        // ============================
        //  VISTAS ESTÁTICAS
        // ============================
        [HttpGet] public IActionResult Modules() => View();
        [HttpGet] public IActionResult Hoteles() => View();
        [HttpGet] public IActionResult Autos() => View();
        [HttpGet] public IActionResult Vuelos() => View();
        [HttpGet] public IActionResult Restaurantes() => View();
        [HttpGet] public IActionResult Paquetes() => View();
        [HttpGet] public IActionResult Privacy() => View();
        public IActionResult Error() => View(new ErrorViewModel());
    }
}
