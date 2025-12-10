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

        /// <summary>
        /// Devuelve recomendaciones de otros servicios en la misma ciudad (excluye tipo/título opcionalmente).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RecomendacionesCiudad(string ciudad, string? excludeTipo, string? excludeTitulo, int limit = 6)
        {
            if (string.IsNullOrWhiteSpace(ciudad))
                return Json(new { success = false, items = Array.Empty<object>() });

            try
            {
                var resp = await _bookingService.BuscarAsync(ciudad, null, null, null);
                if (!resp.Success || resp.Data?.Items == null)
                    return Json(new { success = true, items = Array.Empty<object>() });

                var items = resp.Data.Items
                    .Where(i => string.Equals(i.Ciudad, ciudad, StringComparison.OrdinalIgnoreCase))
                    .Where(i => !(i.Tipo == excludeTipo && i.Titulo == excludeTitulo))
                    .GroupBy(i => new { i.Tipo, i.Titulo })
                    .Select(g => g.First())
                    .Where(i => i.Tipo != excludeTipo)
                    .Take(limit)
                    .Select(i => new { tipo = i.Tipo, titulo = i.Titulo, ciudad = i.Ciudad, precio = i.Precio, rating = i.Rating })
                    .ToList();

                return Json(new { success = true, items });
            }
            catch
            {
                return Json(new { success = false, items = Array.Empty<object>() });
            }
        }

        /// <summary>
        /// Devuelve una lista de ciudades disponibles basadas en los items retornados por la API de búsqueda.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CiudadesDisponibles()
        {
            try
            {
                var resp = await _bookingService.BuscarAsync(null, null, null, null);
                if (!resp.Success || resp.Data?.Items == null)
                    return Json(new { success = true, cities = Array.Empty<string>() });

                var cities = resp.Data.Items
                    .Select(i => i.Ciudad)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(100)
                    .ToList();

                return Json(new { success = true, cities });
            }
            catch
            {
                return Json(new { success = false, cities = Array.Empty<string>() });
            }
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

        // Credenciales de administrador (hardcodeadas)
        private const string ADMIN_EMAIL = "admin@admin.com";
        private const string ADMIN_PASSWORD = "Admin123!";

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Verificar si es el administrador
            if (model.Email.Equals(ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase) && model.Password == ADMIN_PASSWORD)
            {
                HttpContext.Session.SetInt32("ClienteId", -1); // ID especial para admin
                HttpContext.Session.SetString("UserEmail", ADMIN_EMAIL);
                HttpContext.Session.SetString("UserName", "Administrador");
                HttpContext.Session.SetString("IsAdmin", "True");
                return RedirectToAction("Admin");
            }

            var (exito, mensaje, cliente) = await _authService.LoginAsync(model.Email, model.Password);

            if (!exito || cliente == null)
            {
                ModelState.AddModelError(string.Empty, mensaje);
                return View(model);
            }

            // Guardar datos en sesion
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
                    var reservaActiva = rc.Reserva?.Activa ?? true;
                    return new OrderItemViewModel
                    {
                        Tipo = tipoServicio switch
                        {
                            "RentaVehiculos" => "CAR",
                            "Hotel" => "HOTEL",
                            "Aerolinea" => "FLIGHT",
                            "Restaurante" => "RESTAURANT",
                            "PaquetesTuristicos" => "PACKAGE",
                            _ => "PACKAGE"
                        },
                        Titulo = servicio?.Nombre ?? "Servicio",
                        Cantidad = 1,
                        PrecioUnitario = c.ValorPagado / Math.Max(1, c.ReservasCompra.Count) / 1.12m,
                        CodigoReserva = rc.Reserva?.CodigoReserva ?? "",
                        FacturaProveedorUrl = rc.Reserva?.FacturaUrl,
                        ReservaId = rc.Reserva?.Id ?? 0,
                        ServicioId = rc.Reserva?.ServicioId ?? 0,
                        Activa = reservaActiva,
                        PuedeCancelar = reservaActiva
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

        /// <summary>
        /// Cancela una reserva y reembolsa al cliente (solo REST).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReservaApi(int reservaId, int cuentaBancaria)
        {
            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (!clienteId.HasValue)
                return Json(new { success = false, message = "Debes iniciar sesion." });

            var resultado = await _checkoutService.CancelarReservaAsync(reservaId, clienteId.Value, cuentaBancaria);

            return Json(new { 
                success = resultado.Exitoso, 
                message = resultado.Mensaje,
                montoReembolsado = resultado.MontoReembolsado
            });
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
            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "True";
            
            if (!clienteId.HasValue)
                return RedirectToAction("Login");

            // Obtener la compra con sus reservas
            // Admin puede ver cualquier factura, usuario solo las suyas
            var query = _dbContext.Compras
                .Include(c => c.Cliente)
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .Where(c => c.Id == compraId);

            if (!isAdmin)
            {
                query = query.Where(c => c.ClienteId == clienteId.Value);
            }

            var compra = await query.FirstOrDefaultAsync();

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
        public async Task<IActionResult> Admin()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return RedirectToAction("Login");

            var model = new AdminDashboardViewModel();

            // Obtener clientes
            var clientes = await _dbContext.Clientes.ToListAsync();
            model.TotalClientes = clientes.Count;

            // Obtener compras con reservas
            var compras = await _dbContext.Compras
                .Include(c => c.Cliente)
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();

            model.TotalCompras = compras.Count;
            model.IngresosTotales = compras.Sum(c => c.ValorPagado);
            model.ComprasHoy = compras.Count(c => c.FechaCompra.Date == DateTime.Today);
            model.IngresosHoy = compras.Where(c => c.FechaCompra.Date == DateTime.Today).Sum(c => c.ValorPagado);

            // Obtener reservas
            var reservas = await _dbContext.Reservas
                .Include(r => r.Servicio)
                .Include(r => r.ReservasCompra)
                    .ThenInclude(rc => rc.Compra)
                        .ThenInclude(c => c.Cliente)
                .ToListAsync();

            model.TotalReservas = reservas.Count;
            model.ReservasActivas = reservas.Count(r => r.Activa);
            model.ReservasCanceladas = reservas.Count(r => !r.Activa);
            model.ComisionesTotales = reservas.Sum(r => r.ComisionAgencia);

            // Mapear clientes
            model.Clientes = clientes.Select(c => {
                var comprasCliente = compras.Where(co => co.ClienteId == c.Id).ToList();
                return new ClienteAdminViewModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Apellido = c.Apellido,
                    Email = c.CorreoElectronico,
                    Documento = c.DocumentoIdentidad,
                    FechaRegistro = DateTime.Now, // Si tienes fecha de registro
                    TotalCompras = comprasCliente.Count,
                    TotalGastado = comprasCliente.Sum(co => co.ValorPagado)
                };
            }).OrderByDescending(c => c.TotalGastado).ToList();

            // Mapear ultimas compras
            model.UltimasCompras = compras.Take(20).Select(c => new CompraAdminViewModel
            {
                Id = c.Id,
                ClienteNombre = $"{c.Cliente.Nombre} {c.Cliente.Apellido}",
                ClienteEmail = c.Cliente.CorreoElectronico,
                Fecha = c.FechaCompra,
                Total = c.ValorPagado,
                CantidadReservas = c.ReservasCompra.Count,
                Reservas = c.ReservasCompra.Select(rc => new ReservaAdminViewModel
                {
                    Id = rc.Reserva?.Id ?? 0,
                    CodigoReserva = rc.Reserva?.CodigoReserva ?? "",
                    Proveedor = rc.Reserva?.Servicio?.Nombre ?? "",
                    TipoServicio = rc.Reserva?.Servicio?.TipoServicio.ToString() ?? "",
                    Activa = rc.Reserva?.Activa ?? false,
                    FacturaUrl = rc.Reserva?.FacturaUrl
                }).ToList()
            }).ToList();

            // Mapear todas las reservas
            model.Reservas = reservas.Select(r => {
                var compra = r.ReservasCompra.FirstOrDefault()?.Compra;
                return new ReservaAdminViewModel
                {
                    Id = r.Id,
                    CodigoReserva = r.CodigoReserva,
                    Proveedor = r.Servicio?.Nombre ?? "",
                    TipoServicio = r.Servicio?.TipoServicio.ToString() ?? "",
                    Activa = r.Activa,
                    FacturaUrl = r.FacturaUrl,
                    ValorNegocio = r.ValorPagadoNegocio,
                    Comision = r.ComisionAgencia,
                    FechaCompra = compra?.FechaCompra,
                    ClienteNombre = compra?.Cliente != null ? $"{compra.Cliente.Nombre} {compra.Cliente.Apellido}" : ""
                };
            }).OrderByDescending(r => r.FechaCompra).ToList();

            // Obtener estado de proveedores
            var servicios = await _dbContext.Servicios
                .Include(s => s.DetallesServicio)
                .Where(s => s.Activo)
                .ToListAsync();

            model.EstadoProveedores = servicios.Select(s => {
                var detalleRest = s.DetallesServicio.FirstOrDefault(d => d.TipoProtocolo == TravelioDatabaseConnector.Enums.TipoProtocolo.Rest);
                var detalleSoap = s.DetallesServicio.FirstOrDefault(d => d.TipoProtocolo == TravelioDatabaseConnector.Enums.TipoProtocolo.Soap);
                return new ProveedorStatusViewModel
                {
                    ServicioId = s.Id,
                    Nombre = s.Nombre,
                    TipoServicio = s.TipoServicio.ToString(),
                    TieneRest = detalleRest != null,
                    TieneSoap = detalleSoap != null,
                    UrlRest = detalleRest?.UriBase,
                    UrlSoap = detalleSoap?.UriBase,
                    Activo = s.Activo,
                    EstadoRest = detalleRest != null ? "Configurado" : "No disponible",
                    EstadoSoap = detalleSoap != null ? "Configurado" : "No disponible"
                };
            }).ToList();

            return View(model);
        }

        [HttpPost]
        public IActionResult UpdateConfig(int iva, int discount, string categories)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return Unauthorized();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> VerificarProveedorApi(int servicioId)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return Unauthorized();

            try
            {
                var detalles = await _dbContext.DetallesServicio
                    .Include(d => d.Servicio)
                    .Where(d => d.ServicioId == servicioId)
                    .ToListAsync();

                var resultados = new List<object>();

                foreach (var detalle in detalles)
                {
                    var url = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";
                    var protocolo = detalle.TipoProtocolo.ToString();
                    bool disponible = false;
                    string mensaje = "";

                    try
                    {
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                        var response = await httpClient.GetAsync(url);
                        disponible = response.IsSuccessStatusCode;
                        mensaje = disponible ? "OK" : $"Error: {response.StatusCode}";
                    }
                    catch (Exception ex)
                    {
                        mensaje = $"Error: {ex.Message}";
                    }

                    resultados.Add(new { protocolo, url, disponible, mensaje });
                }

                return Json(new { success = true, resultados });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cancela una reserva desde el panel de admin (sin reembolso automatico).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelarReservaAdminApi(int reservaId)
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return Unauthorized();

            try
            {
                var reserva = await _dbContext.Reservas.FindAsync(reservaId);
                if (reserva == null)
                    return Json(new { success = false, message = "Reserva no encontrada" });

                reserva.Activa = false;
                await _dbContext.SaveChangesAsync();

                return Json(new { success = true, message = "Reserva marcada como cancelada" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene estadisticas para el dashboard admin via AJAX.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AdminEstadisticasApi()
        {
            if (HttpContext.Session.GetString("IsAdmin") != "True")
                return Unauthorized();

            var comprasHoy = await _dbContext.Compras
                .Where(c => c.FechaCompra.Date == DateTime.Today)
                .SumAsync(c => c.ValorPagado);

            var comprasSemana = await _dbContext.Compras
                .Where(c => c.FechaCompra >= DateTime.Today.AddDays(-7))
                .SumAsync(c => c.ValorPagado);

            var comprasMes = await _dbContext.Compras
                .Where(c => c.FechaCompra >= DateTime.Today.AddMonths(-1))
                .SumAsync(c => c.ValorPagado);

            return Json(new { 
                hoy = comprasHoy, 
                semana = comprasSemana, 
                mes = comprasMes 
            });
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

            // Crear nuevo item basado en el detalle retornado
            var nuevo = new CartItemViewModel
            {
                Tipo = producto.Tipo,
                Titulo = producto.Titulo,
                Detalle = producto.Ciudad,
                PrecioOriginal = producto.Precio,
                PrecioFinal = producto.Precio,
                PrecioUnitario = producto.Precio,
                Cantidad = 1
            };

            // Añadir o incrementar
            var existente = cart.FirstOrDefault(x => x.Tipo == nuevo.Tipo && x.Titulo == nuevo.Titulo);
            if (existente != null)
            {
                existente.Cantidad++;
            }
            else
            {
                cart.Add(nuevo);
            }

            // Aplicar descuento por combinar productos en la misma ciudad
            // Regla simple: 2 productos en la misma ciudad => 10% cada uno, 3 o más => 15%
            var ciudad = producto.Ciudad ?? string.Empty;
            var grupo = cart.Where(i => i.Detalle == ciudad).ToList();
            int grupoCount = grupo.Count;
            int discountPct = 0;
            if (grupoCount >= 2) discountPct = grupoCount == 2 ? 10 : 15;

            if (discountPct > 0)
            {
                foreach (var it in grupo)
                {
                    it.PrecioFinal = Decimal.Round(it.PrecioOriginal * (1 - discountPct / 100m), 2);
                }
            }

            HttpContext.Session.Set(CART_SESSION_KEY, cart);

            // Obtener recomendaciones: otros servicios en la misma ciudad (diferente tipo)
            List<object> recomendaciones = new();
            try
            {
                var recResp = await _bookingService.BuscarAsync(ciudad, null, null, null);
                if (recResp.Success && recResp.Data?.Items != null)
                {
                    recomendaciones = recResp.Data.Items
                        .Where(i => i.Ciudad == ciudad && !(i.Tipo == producto.Tipo && i.Titulo == producto.Titulo))
                        .GroupBy(i => new { i.Tipo, i.Titulo })
                        .Select(g => g.First())
                        .Where(i => i.Tipo != producto.Tipo)
                        .Take(3)
                        .Select(i => new { tipo = i.Tipo, titulo = i.Titulo, ciudad = i.Ciudad, precio = i.Precio })
                        .Cast<object>()
                        .ToList();
                }
            }
            catch { }

            return Ok(new { success = true, message = "Añadido", totalCount = cart.Sum(x => x.Cantidad), recommendations = recomendaciones });
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
