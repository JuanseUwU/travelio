using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingMvcDotNet.Models;
using BookingMvcDotNet.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TravelioDatabaseConnector.Enums;
using TravelioDatabaseConnector.Models;
using TravelioIntegrator.Models.Carrito;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IAuthService _authService;
        private readonly ICheckoutService _checkoutService;
        private readonly TravelioIntegrationService _integrationService;
        private const string CART_SESSION_KEY = "MyCartSession";

        public HomeController(IBookingService bookingService, IAuthService authService, ICheckoutService checkoutService, TravelioIntegrationService integrationService)
        {
            _bookingService = bookingService;
            _authService = authService;
            _checkoutService = checkoutService;
            _integrationService = integrationService;
        }

        /// <summary>
        /// Devuelve recomendaciones de otros servicios en la misma ciudad (excluye tipo/t�tulo opcionalmente).
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
        /// Devuelve una lista de ciudades disponibles basadas en los items retornados por la API de b�squeda.
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
        //  VISTAS P�BLICAS
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

            TempData["SuccessMessage"] = "�Registro exitoso! Ahora puedes iniciar sesi�n.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        private static string MapTipoServicio(TipoServicio tipo) =>
            tipo switch
            {
                TipoServicio.RentaVehiculos => "CAR",
                TipoServicio.Hotel => "HOTEL",
                TipoServicio.Aerolinea => "FLIGHT",
                TipoServicio.Restaurante => "RESTAURANT",
                TipoServicio.PaquetesTuristicos => "PACKAGE",
                _ => "PACKAGE"
            };

        private static TipoServicio? MapTipoServicioFromCart(string? tipo) =>
            tipo switch
            {
                "CAR" => TipoServicio.RentaVehiculos,
                "HOTEL" => TipoServicio.Hotel,
                "FLIGHT" => TipoServicio.Aerolinea,
                "RESTAURANT" => TipoServicio.Restaurante,
                "PACKAGE" => TipoServicio.PaquetesTuristicos,
                _ => null
            };

        private static string BuildCartKey(CartItemViewModel item)
        {
            if (item.ServicioId == 0 && string.IsNullOrWhiteSpace(item.IdProducto))
            {
                return $"misc:{item.Tipo}|{item.Titulo}";
            }

            var fechaInicio = item.FechaInicio?.Date.ToString("yyyy-MM-dd") ?? "";
            var fechaFin = item.FechaFin?.Date.ToString("yyyy-MM-dd") ?? "";
            var personas = item.NumeroPersonas?.ToString() ?? "";
            return $"{item.Tipo}|{item.ServicioId}|{item.IdProducto}|{fechaInicio}|{fechaFin}|{personas}";
        }

        private static string? ObtenerImagenHabitacion(string? imagenes)
        {
            if (string.IsNullOrWhiteSpace(imagenes))
            {
                return null;
            }

            var partes = imagenes.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (partes.Length > 0)
            {
                return partes[0];
            }

            partes = imagenes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return partes.Length > 0 ? partes[0] : null;
        }

        private async Task<List<CartItemViewModel>> ObtenerCarritoActualAsync()
        {
            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            var clienteId = HttpContext.Session.GetInt32("ClienteId");
            if (!clienteId.HasValue)
            {
                return cartItems;
            }

            var servicios = await _integrationService.ObtenerServiciosActivosAsync() ?? new List<Servicio>();
            var serviciosMap = servicios.ToDictionary(s => s.Id, s => s.Nombre);

            var dbItems = new List<CartItemViewModel>();

            var autos = await _integrationService.ObtenerCarritoAutosAsync(clienteId.Value) ?? [];
            foreach (var item in autos)
            {
                var dias = Math.Max(1, (int)(item.FechaFin.Date - item.FechaInicio.Date).TotalDays);
                var proveedor = serviciosMap.TryGetValue(item.ServicioId, out var nombre) ? nombre : "Proveedor";
                dbItems.Add(new CartItemViewModel
                {
                    CarritoItemId = item.Id,
                    Tipo = "CAR",
                    ServicioId = item.ServicioId,
                    IdProducto = item.IdAutoProveedor,
                    Titulo = $"{item.Tipo} - {proveedor}",
                    Detalle = $"{item.Ciudad}, {item.Pais} | {item.FechaInicio:dd/MM/yyyy} - {item.FechaFin:dd/MM/yyyy} ({dias} dias)",
                    ImagenUrl = item.UriImagen,
                    PrecioOriginal = item.PrecioNormalPorDia * dias,
                    PrecioFinal = item.PrecioActualPorDia * dias,
                    PrecioUnitario = item.PrecioActualPorDia,
                    Cantidad = 1,
                    FechaInicio = item.FechaInicio,
                    FechaFin = item.FechaFin,
                    UnidadPrecio = "por dia"
                });
            }

            var habitaciones = await _integrationService.ObtenerCarritoHabitacionesAsync(clienteId.Value) ?? [];
            foreach (var item in habitaciones)
            {
                var noches = Math.Max(1, (item.FechaFin.Date - item.FechaInicio.Date).Days);
                dbItems.Add(new CartItemViewModel
                {
                    CarritoItemId = item.Id,
                    Tipo = "HOTEL",
                    ServicioId = item.ServicioId,
                    IdProducto = item.IdHabitacionProveedor,
                    Titulo = $"{item.Hotel} - {item.NombreHabitacion}",
                    Detalle = $"{item.Ciudad}, {item.Pais} | {item.TipoHabitacion} | {item.NumeroHuespedes} huespedes",
                    ImagenUrl = ObtenerImagenHabitacion(item.Imagenes),
                    PrecioOriginal = item.PrecioNormal * noches,
                    PrecioFinal = item.PrecioActual * noches,
                    PrecioUnitario = item.PrecioActual,
                    Cantidad = 1,
                    FechaInicio = item.FechaInicio,
                    FechaFin = item.FechaFin,
                    NumeroPersonas = item.NumeroHuespedes,
                    UnidadPrecio = $"({noches} noches)"
                });
            }

            var vuelos = await _integrationService.ObtenerCarritoVuelosAsync(clienteId.Value) ?? [];
            foreach (var item in vuelos)
            {
                dbItems.Add(new CartItemViewModel
                {
                    CarritoItemId = item.Id,
                    Tipo = "FLIGHT",
                    ServicioId = item.ServicioId,
                    IdProducto = item.IdVueloProveedor,
                    Titulo = $"{item.Origen} - {item.Destino}",
                    Detalle = $"{item.NombreAerolinea} | {item.TipoCabina} | {item.CantidadPasajeros} pasajero(s)",
                    PrecioOriginal = item.PrecioNormal * item.CantidadPasajeros,
                    PrecioFinal = item.PrecioActual * item.CantidadPasajeros,
                    PrecioUnitario = item.PrecioActual,
                    Cantidad = 1,
                    FechaInicio = item.FechaVuelo,
                    FechaFin = item.FechaVuelo,
                    NumeroPersonas = item.CantidadPasajeros,
                    UnidadPrecio = $"({item.CantidadPasajeros} pasajeros)"
                });
            }

            var paquetes = await _integrationService.ObtenerCarritoPaquetesAsync(clienteId.Value) ?? [];
            foreach (var item in paquetes)
            {
                var fechaFin = item.FechaInicio.AddDays(item.Duracion);
                dbItems.Add(new CartItemViewModel
                {
                    CarritoItemId = item.Id,
                    Tipo = "PACKAGE",
                    ServicioId = item.ServicioId,
                    IdProducto = item.IdPaqueteProveedor,
                    Titulo = item.Nombre,
                    Detalle = $"{item.Ciudad}, {item.Pais} | {item.TipoActividad} | {item.Duracion} dias",
                    ImagenUrl = item.ImagenUrl,
                    PrecioOriginal = item.PrecioNormal * item.Personas,
                    PrecioFinal = item.PrecioActual * item.Personas,
                    PrecioUnitario = item.PrecioActual,
                    Cantidad = 1,
                    FechaInicio = item.FechaInicio,
                    FechaFin = fechaFin,
                    NumeroPersonas = item.Personas,
                    UnidadPrecio = $"({item.Personas} personas)"
                });
            }

            var mesas = await _integrationService.ObtenerCarritoMesasAsync(clienteId.Value) ?? [];
            foreach (var item in mesas)
            {
                var proveedor = serviciosMap.TryGetValue(item.ServicioId, out var nombre) ? nombre : "Restaurante";
                dbItems.Add(new CartItemViewModel
                {
                    CarritoItemId = item.Id,
                    Tipo = "RESTAURANT",
                    ServicioId = item.ServicioId,
                    IdProducto = item.IdMesa.ToString(),
                    Titulo = $"Mesa {item.NumeroMesa} - {item.TipoMesa}",
                    Detalle = $"{proveedor} | {item.NumeroPersonas} personas",
                    ImagenUrl = item.ImagenUrl,
                    PrecioOriginal = item.Precio,
                    PrecioFinal = item.Precio,
                    PrecioUnitario = item.Precio,
                    Cantidad = 1,
                    FechaInicio = item.FechaReserva,
                    FechaFin = item.FechaReserva,
                    NumeroPersonas = item.NumeroPersonas,
                    UnidadPrecio = "por reserva"
                });
            }

            var sessionMap = cartItems.ToDictionary(BuildCartKey, StringComparer.OrdinalIgnoreCase);
            foreach (var dbItem in dbItems)
            {
                var key = BuildCartKey(dbItem);
                if (sessionMap.TryGetValue(key, out var existing))
                {
                    if (!existing.CarritoItemId.HasValue)
                    {
                        existing.CarritoItemId = dbItem.CarritoItemId;
                    }
                    continue;
                }

                cartItems.Add(dbItem);
                sessionMap[key] = dbItem;
            }

            HttpContext.Session.Set(CART_SESSION_KEY, cartItems);
            return cartItems;
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
            var compras = await _integrationService.ObtenerComprasPorClienteAsync(clienteId.Value)
                ?? new List<Compra>();

            var orders = compras.Select(c => new OrderViewModel
            {
                OrderId = c.Id.ToString(),
                Date = c.FechaCompra,
                Total = c.ValorPagado,
                FacturaTravelioUrl = Url.Action("FacturaTravelio", "Home", new { compraId = c.Id }),
                Items = c.ReservasCompra.Select(rc => {
                    var reserva = rc.Reserva;
                    var servicio = reserva?.Servicio;
                    var reservaActiva = reserva?.Activa ?? true;
                    var tipo = servicio is null ? "PACKAGE" : MapTipoServicio(servicio.TipoServicio);
                    var precioUnitario = (reserva?.ValorPagadoNegocio ?? 0m) + (reserva?.ComisionAgencia ?? 0m);
                    return new OrderItemViewModel
                    {
                        Tipo = tipo,
                        Titulo = servicio?.Nombre ?? "Servicio",
                        Cantidad = 1,
                        PrecioUnitario = precioUnitario,
                        CodigoReserva = reserva?.CodigoReserva ?? "",
                        FacturaProveedorUrl = reserva?.FacturaUrl,
                        ReservaId = reserva?.Id ?? 0,
                        ServicioId = reserva?.ServicioId ?? 0,
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
        public async Task<IActionResult> Carrito()
        {
            var cartItems = await ObtenerCarritoActualAsync();
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

            var cartItems = await ObtenerCarritoActualAsync();

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
                return Json(new { success = false, message = "Debes iniciar sesi�n." });

            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            if (!cartItems.Any())
                return Json(new { success = false, message = "Carrito vac�o." });

            // Validar n�mero de cuenta bancaria
            if (!int.TryParse(model.NumeroCuentaBancaria, out var cuentaBancaria))
                return Json(new { success = false, message = "N�mero de cuenta bancaria inv�lido." });

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
                foreach (var item in cartItems)
                {
                    if (!item.CarritoItemId.HasValue)
                    {
                        continue;
                    }

                    var tipoServicio = MapTipoServicioFromCart(item.Tipo);
                    if (!tipoServicio.HasValue)
                    {
                        continue;
                    }

                    await _integrationService.EliminarItemCarritoAsync(tipoServicio.Value, item.CarritoItemId.Value);
                }

                HttpContext.Session.Remove(CART_SESSION_KEY);
                
                // Guardar resultado en TempData para mostrar en la p�gina de confirmaci�n
                TempData["CheckoutExitoso"] = true;
                TempData["CheckoutMensaje"] = resultado.Mensaje;
                TempData["CompraId"] = resultado.CompraId;
                TempData["TotalPagado"] = resultado.TotalPagado.ToString("C");
                
                // Serializar las reservas para mostrar en la confirmaci�n
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
        /// P�gina de confirmaci�n despu�s de una compra exitosa.
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
            var compra = await _integrationService.ObtenerCompraConReservasAsync(
                compraId,
                isAdmin ? null : clienteId.Value);

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

            // Crear items de la factura basados en reservas activas
            factura.Items = compra.ReservasCompra
                .Where(rc => rc.Reserva?.Activa == true)
                .Select(rc => {
                    var reserva = rc.Reserva;
                    var servicio = reserva?.Servicio;
                    var tipo = servicio is null ? "PACKAGE" : MapTipoServicio(servicio.TipoServicio);
                    var precioUnitario = (reserva?.ValorPagadoNegocio ?? 0m) + (reserva?.ComisionAgencia ?? 0m);
                    return new FacturaItemViewModel
                    {
                        Descripcion = servicio?.Nombre ?? "Servicio de viaje",
                        Tipo = tipo,
                        CodigoReserva = reserva?.CodigoReserva ?? "-",
                        Cantidad = 1,
                        PrecioUnitario = precioUnitario
                    };
                }).ToList();

            // Calcular totales en base a reservas activas
            factura.PorcentajeIVA = 12m;
            factura.Subtotal = factura.Items.Sum(i => i.Total);
            factura.IVA = factura.Subtotal * (factura.PorcentajeIVA / 100m);
            factura.Total = factura.Subtotal + factura.IVA;

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
            var clientes = await _integrationService.ObtenerClientesAsync() ?? new List<Cliente>();
            model.TotalClientes = clientes.Count;

            // Obtener compras con reservas
            var compras = await _integrationService.ObtenerComprasAsync() ?? new List<Compra>();

            model.TotalCompras = compras.Count;
            model.IngresosTotales = compras.Sum(c => c.ValorPagado);
            model.ComprasHoy = compras.Count(c => c.FechaCompra.Date == DateTime.Today);
            model.IngresosHoy = compras.Where(c => c.FechaCompra.Date == DateTime.Today).Sum(c => c.ValorPagado);

            // Obtener reservas
            var reservas = await _integrationService.ObtenerReservasAsync() ?? new List<Reserva>();

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
            var servicios = await _integrationService.ObtenerServiciosActivosAsync() ?? new List<Servicio>();

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
                var estados = await _integrationService.VerificarProveedoresAsync(servicioId);
                if (estados is null)
                {
                    return Json(new { success = false, message = "No se pudo verificar los proveedores." });
                }

                var resultados = estados.Select(e => new
                {
                    protocolo = e.Protocolo,
                    url = e.Url,
                    disponible = e.Disponible,
                    mensaje = e.Mensaje
                });

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
                var resultado = await _integrationService.MarcarReservaComoCanceladaAsync(reservaId);
                if (resultado is null)
                {
                    return Json(new { success = false, message = "No se pudo cancelar la reserva." });
                }

                if (!resultado.Value)
                {
                    return Json(new { success = false, message = "Reserva no encontrada" });
                }

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

            var estadisticas = await _integrationService.ObtenerEstadisticasComprasAsync();
            if (estadisticas is null)
            {
                return Json(new { hoy = 0m, semana = 0m, mes = 0m });
            }

            return Json(new
            {
                hoy = estadisticas.Value.hoy,
                semana = estadisticas.Value.semana,
                mes = estadisticas.Value.mes
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

            // A�adir o incrementar
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
            // Regla simple: 2 productos en la misma ciudad => 10% cada uno, 3 o m�s => 15%
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

            return Ok(new { success = true, message = "A�adido", totalCount = cart.Sum(x => x.Cantidad), recommendations = recomendaciones });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDelCarritoApi(string? titulo, int? cartId, string? tipo)
        {
            var cart =
                HttpContext.Session.Get<List<CartItemViewModel>>(CART_SESSION_KEY)
                ?? new List<CartItemViewModel>();

            CartItemViewModel? item = null;
            if (cartId.HasValue)
            {
                item = cart.FirstOrDefault(x => x.CarritoItemId == cartId.Value);
            }

            if (item == null && !string.IsNullOrWhiteSpace(titulo))
            {
                item = cart.FirstOrDefault(x => x.Titulo == titulo);
            }

            var removed = false;
            if (item != null)
            {
                cart.Remove(item);
                removed = true;
            }

            if (cartId.HasValue)
            {
                var tipoServicio = MapTipoServicioFromCart(tipo);
                if (tipoServicio.HasValue)
                {
                    var eliminado = await _integrationService.EliminarItemCarritoAsync(tipoServicio.Value, cartId.Value);
                    if (eliminado == true)
                    {
                        removed = true;
                    }
                }
            }

            if (removed)
            {
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
        public async Task<IActionResult> GetCartCount()
        {
            var cart = await ObtenerCarritoActualAsync();
            return Ok(new { count = cart.Sum(x => x.Cantidad) });
        }

        // ============================
        //  VISTAS EST�TICAS
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

