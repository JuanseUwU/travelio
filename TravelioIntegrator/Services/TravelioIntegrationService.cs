using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using TravelioAPIConnector.Aerolinea;
using TravelioAPIConnector.Autos;
using TravelioAPIConnector.Habitaciones;
using TravelioAPIConnector.Mesas;
using TravelioAPIConnector.Paquetes;
using static TravelioAPIConnector.Global;
using TravelioBankConnector;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioDatabaseConnector.Models;
using TravelioDatabaseConnector.Models.Carrito;
using TravelioDatabaseConnector.Services;
using TravelioIntegrator.Models;
using TravelioIntegrator.Models.Carrito;
using DbReserva = TravelioDatabaseConnector.Models.Reserva;
using AerolineaConnector = TravelioAPIConnector.Aerolinea.Connector;
using AutosConnector = TravelioAPIConnector.Autos.Connector;
using HabitacionesConnector = TravelioAPIConnector.Habitaciones.Connector;
using MesasConnector = TravelioAPIConnector.Mesas.Connector;
using PaquetesConnector = TravelioAPIConnector.Paquetes.Connector;

namespace TravelioIntegrator.Services;

/// <summary>
/// Capa de orquestación que integra base de datos, APIs de servicios y API bancaria.
/// Todas las funciones devuelven null ante errores catastróficos y registran eventos en ILogger.
/// </summary>
public class TravelioIntegrationService
{
    private readonly TravelioDbContext _db;
    private readonly ILogger _defaultLogger;
    private const decimal COMISION_TRAVELIO = 0.10m;

    public TravelioIntegrationService(TravelioDbContext db, ILogger<TravelioIntegrationService>? logger = null)
    {
        _db = db;
        _defaultLogger = logger ?? NullLogger<TravelioIntegrationService>.Instance;
    }

    private ILogger ResolveLogger(ILogger? logger) => logger ?? _defaultLogger;

    private static string? BuildUri(DetalleServicio detalle, string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(detalle.UriBase))
        {
            return endpoint;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return detalle.UriBase;
        }

        return $"{detalle.UriBase.TrimEnd('/')}{endpoint}";
    }

    private static IEnumerable<DetalleServicio> FiltrarDetallesPorProtocolo(IEnumerable<DetalleServicio> detalles, bool preferRest)
    {
        var protocoloPreferido = preferRest ? TipoProtocolo.Rest : TipoProtocolo.Soap;
        var seleccion = detalles.Where(d => d.TipoProtocolo == protocoloPreferido).ToList();
        return seleccion.Count > 0 ? seleccion : detalles;
    }

    private static (DetalleServicio? preferido, DetalleServicio? alternativo) SeleccionarDetalle(IEnumerable<DetalleServicio> detalles, bool preferRest)
    {
        var rest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
        var soap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);
        var preferido = preferRest ? rest ?? soap : soap ?? rest;
        var alternativo = preferRest ? soap : rest;
        return (preferido, alternativo);
    }

    #region Usuarios

    public async Task<Cliente?> CrearUsuarioAsync(UserCreateRequest request, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var existing = await _db.Clientes.FirstOrDefaultAsync(c => c.CorreoElectronico == request.Correo);
            if (existing is not null)
            {
                log.LogWarning("Intento de crear usuario ya existente: {Correo}", request.Correo);
                return null;
            }

            var cliente = new Cliente
            {
                CorreoElectronico = request.Correo,
                Nombre = request.Nombre,
                Apellido = request.Apellido,
                Pais = request.Pais,
                FechaNacimiento = request.FechaNacimiento,
                Telefono = request.Telefono,
                TipoIdentificacion = request.TipoIdentificacion,
                DocumentoIdentidad = request.DocumentoIdentidad,
                Rol = request.Rol ?? RolUsuario.Cliente
            };

            log.LogTrace("Creando usuario con datos: {Correo} {Nombre} {Apellido} {PasswordPlano}", request.Correo, request.Nombre, request.Apellido, request.PasswordPlano);

            ClientePasswordService.EstablecerPassword(cliente, request.PasswordPlano);
            _db.Clientes.Add(cliente);
            await _db.SaveChangesAsync();

            return cliente;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear usuario {Correo}", request.Correo);
            return null;
        }
    }

    public Cliente? CrearUsuario(UserCreateRequest request, ILogger? logger = null) =>
        CrearUsuarioAsync(request, logger).GetAwaiter().GetResult();

    public async Task<Cliente?> IniciarSesionAsync(string correo, string passwordPlano, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.CorreoElectronico == correo);
            if (cliente is null)
            {
                return null;
            }

            log.LogTrace("Intento de login usuario {Correo} con password {PasswordPlano}", correo, passwordPlano);

            var valido = ClientePasswordService.EsPasswordValido(cliente, passwordPlano);
            return valido ? cliente : null;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al iniciar sesión para {Correo}", correo);
            return null;
        }
    }

    public Cliente? IniciarSesion(string correo, string passwordPlano, ILogger? logger = null) =>
        IniciarSesionAsync(correo, passwordPlano, logger).GetAwaiter().GetResult();

    public async Task<Cliente?> ObtenerClientePorIdAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Clientes.FindAsync(clienteId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public Cliente? ObtenerClientePorId(int clienteId, ILogger? logger = null) =>
        ObtenerClientePorIdAsync(clienteId, logger).GetAwaiter().GetResult();

    public async Task<Cliente?> ObtenerClientePorEmailAsync(string correo, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Clientes.FirstOrDefaultAsync(c => c.CorreoElectronico == correo);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener cliente por correo {Correo}", correo);
            return null;
        }
    }

    public Cliente? ObtenerClientePorEmail(string correo, ILogger? logger = null) =>
        ObtenerClientePorEmailAsync(correo, logger).GetAwaiter().GetResult();

    public async Task<bool?> EsAdministradorAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente is null) return null;
            return cliente.Rol == RolUsuario.Administrador;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar rol de administrador para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public bool? EsAdministrador(int clienteId, ILogger? logger = null) =>
        EsAdministradorAsync(clienteId, logger).GetAwaiter().GetResult();

    #endregion

    #region Búsqueda de productos

    public async Task<IReadOnlyList<ProductoServicio<Vuelo>>?> BuscarVuelosAsync(FiltroVuelos filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.Servicio.TipoServicio == TipoServicio.Aerolinea)
                .ToListAsync();

            var preferRest = IsREST;
            var detallesFiltrados = FiltrarDetallesPorProtocolo(detalles, preferRest).ToList();

            var resultados = new List<ProductoServicio<Vuelo>>();
            foreach (var det in detallesFiltrados)
            {
                var uri = BuildUri(det, det.ObtenerProductosEndpoint);
                var forceSoap = preferRest && det.TipoProtocolo == TipoProtocolo.Soap;
                try
                {
                    log.LogDebug("Buscando vuelos en {Uri} con filtros {@Filtros}", uri, filtros);
                    try
                    {
                        var vuelos = await AerolineaConnector.GetVuelosAsync(
                            uri!,
                            filtros.Origen,
                            filtros.Destino,
                            filtros.FechaDespegue,
                            filtros.FechaLlegada,
                            filtros.TipoCabina,
                            filtros.Pasajeros,
                            filtros.PrecioMin,
                            filtros.PrecioMax,
                            forceSoap);

                        resultados.AddRange(vuelos.Select(v => new ProductoServicio<Vuelo>(det.ServicioId, det.Servicio.Nombre, v)
                        {
                            UriBase = det.UriBase
                        }));
                    }
                    catch (NotImplementedException) when (preferRest)
                    {
                        var detSoap = detalles.FirstOrDefault(d => d.ServicioId == det.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap);
                        if (detSoap is null) throw;

                        var uriSoap = BuildUri(detSoap, detSoap.ObtenerProductosEndpoint);
                        var vuelos = await AerolineaConnector.GetVuelosAsync(
                            uriSoap!,
                            filtros.Origen,
                            filtros.Destino,
                            filtros.FechaDespegue,
                            filtros.FechaLlegada,
                            filtros.TipoCabina,
                            filtros.Pasajeros,
                            filtros.PrecioMin,
                            filtros.PrecioMax,
                            true);

                        resultados.AddRange(vuelos.Select(v => new ProductoServicio<Vuelo>(detSoap.ServicioId, detSoap.Servicio.Nombre, v)
                        {
                            UriBase = detSoap.UriBase
                        }));
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al buscar vuelos en {Uri}", uri);
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar vuelos");
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Vuelo>>? BuscarVuelos(FiltroVuelos filtros, ILogger? logger = null) =>
        BuscarVuelosAsync(filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Vuelo>>?> BuscarVuelosPorServicioAsync(int servicioId, FiltroVuelos filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.ServicioId == servicioId && d.Servicio.TipoServicio == TipoServicio.Aerolinea)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return Array.Empty<ProductoServicio<Vuelo>>();

            var uri = BuildUri(det, det.ObtenerProductosEndpoint);
            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            log.LogDebug("Buscando vuelos en servicio {ServicioId} uri {Uri} con filtros {@Filtros}", servicioId, uri, filtros);
            try
            {
                var vuelos = await AerolineaConnector.GetVuelosAsync(
                    uri!,
                    filtros.Origen,
                    filtros.Destino,
                    filtros.FechaDespegue,
                    filtros.FechaLlegada,
                    filtros.TipoCabina,
                    filtros.Pasajeros,
                    filtros.PrecioMin,
                    filtros.PrecioMax,
                    forceSoap);

                return vuelos.Select(v => new ProductoServicio<Vuelo>(det.ServicioId, det.Servicio.Nombre, v)
                {
                    UriBase = det.UriBase
                }).ToArray();
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ObtenerProductosEndpoint);
                var vuelos = await AerolineaConnector.GetVuelosAsync(
                    uriSoap!,
                    filtros.Origen,
                    filtros.Destino,
                    filtros.FechaDespegue,
                    filtros.FechaLlegada,
                    filtros.TipoCabina,
                    filtros.Pasajeros,
                    filtros.PrecioMin,
                    filtros.PrecioMax,
                    true);

                return vuelos.Select(v => new ProductoServicio<Vuelo>(alternativo.ServicioId, alternativo.Servicio.Nombre, v)
                {
                    UriBase = alternativo.UriBase
                }).ToArray();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar vuelos del servicio {ServicioId}", servicioId);
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Vuelo>>? BuscarVuelosPorServicio(int servicioId, FiltroVuelos filtros, ILogger? logger = null) =>
        BuscarVuelosPorServicioAsync(servicioId, filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Vehiculo>>?> BuscarAutosAsync(FiltroAutos filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.Servicio.TipoServicio == TipoServicio.RentaVehiculos)
                .ToListAsync();

            var preferRest = IsREST;
            var detallesFiltrados = FiltrarDetallesPorProtocolo(detalles, preferRest).ToList();
            var resultados = new List<ProductoServicio<Vehiculo>>();
            foreach (var det in detallesFiltrados)
            {
                var uri = BuildUri(det, det.ObtenerProductosEndpoint);
                var forceSoap = preferRest && det.TipoProtocolo == TipoProtocolo.Soap;
                try
                {
                    log.LogDebug("Buscando autos en {Uri} con filtros {@Filtros}", uri, filtros);
                    try
                    {
                        var autos = await TravelioAPIConnector.Autos.Connector.GetVehiculosAsync(
                            uri!,
                            filtros.Categoria,
                            filtros.Transmision,
                            filtros.Capacidad,
                            filtros.PrecioMin,
                            filtros.PrecioMax,
                            filtros.Sort,
                            filtros.Ciudad,
                            filtros.Pais,
                            forceSoap);

                        resultados.AddRange(autos.Select(a => new ProductoServicio<Vehiculo>(det.ServicioId, det.Servicio.Nombre, a)
                        {
                            UriBase = det.UriBase
                        }));
                    }
                    catch (NotImplementedException) when (preferRest)
                    {
                        var detSoap = detalles.FirstOrDefault(d => d.ServicioId == det.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap);
                        if (detSoap is null) throw;

                        var uriSoap = BuildUri(detSoap, detSoap.ObtenerProductosEndpoint);
                        var autos = await TravelioAPIConnector.Autos.Connector.GetVehiculosAsync(
                            uriSoap!,
                            filtros.Categoria,
                            filtros.Transmision,
                            filtros.Capacidad,
                            filtros.PrecioMin,
                            filtros.PrecioMax,
                            filtros.Sort,
                            filtros.Ciudad,
                            filtros.Pais,
                            true);

                        resultados.AddRange(autos.Select(a => new ProductoServicio<Vehiculo>(detSoap.ServicioId, detSoap.Servicio.Nombre, a)
                        {
                            UriBase = detSoap.UriBase
                        }));
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al buscar autos en {Uri}", uri);
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar autos");
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Vehiculo>>? BuscarAutos(FiltroAutos filtros, ILogger? logger = null) =>
        BuscarAutosAsync(filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Vehiculo>>?> BuscarAutosPorServicioAsync(int servicioId, FiltroAutos filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.ServicioId == servicioId && d.Servicio.TipoServicio == TipoServicio.RentaVehiculos)
                .ToListAsync();
            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return Array.Empty<ProductoServicio<Vehiculo>>();

            var uri = BuildUri(det, det.ObtenerProductosEndpoint);
            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            log.LogDebug("Buscando autos en servicio {ServicioId} uri {Uri} con filtros {@Filtros}", servicioId, uri, filtros);
            try
            {
                var autos = await TravelioAPIConnector.Autos.Connector.GetVehiculosAsync(
                    uri!,
                    filtros.Categoria,
                    filtros.Transmision,
                    filtros.Capacidad,
                    filtros.PrecioMin,
                    filtros.PrecioMax,
                    filtros.Sort,
                    filtros.Ciudad,
                    filtros.Pais,
                    forceSoap);

                return autos.Select(a => new ProductoServicio<Vehiculo>(det.ServicioId, det.Servicio.Nombre, a)
                {
                    UriBase = det.UriBase
                }).ToArray();
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ObtenerProductosEndpoint);
                var autos = await TravelioAPIConnector.Autos.Connector.GetVehiculosAsync(
                    uriSoap!,
                    filtros.Categoria,
                    filtros.Transmision,
                    filtros.Capacidad,
                    filtros.PrecioMin,
                    filtros.PrecioMax,
                    filtros.Sort,
                    filtros.Ciudad,
                    filtros.Pais,
                    true);

                return autos.Select(a => new ProductoServicio<Vehiculo>(alternativo.ServicioId, alternativo.Servicio.Nombre, a)
                {
                    UriBase = alternativo.UriBase
                }).ToArray();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar autos del servicio {ServicioId}", servicioId);
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Vehiculo>>? BuscarAutosPorServicio(int servicioId, FiltroAutos filtros, ILogger? logger = null) =>
        BuscarAutosPorServicioAsync(servicioId, filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Habitacion>>?> BuscarHabitacionesAsync(FiltroHabitaciones filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.Servicio.TipoServicio == TipoServicio.Hotel)
                .ToListAsync();

            var preferRest = IsREST;
            var detallesFiltrados = FiltrarDetallesPorProtocolo(detalles, preferRest).ToList();
            var resultados = new List<ProductoServicio<Habitacion>>();
            foreach (var det in detallesFiltrados)
            {
                var uri = BuildUri(det, det.ObtenerProductosEndpoint);
                var forceSoap = preferRest && det.TipoProtocolo == TipoProtocolo.Soap;
                try
                {
                    log.LogDebug("Buscando habitaciones en {Uri} con filtros {@Filtros}", uri, filtros);
                    try
                    {
                        var habitaciones = await TravelioAPIConnector.Habitaciones.Connector.BuscarHabitacionesAsync(
                            uri!,
                            filtros.FechaInicio,
                            filtros.FechaFin,
                            filtros.TipoHabitacion,
                            filtros.Capacidad,
                            filtros.PrecioMin,
                            filtros.PrecioMax,
                            forceSoap);

                        resultados.AddRange(habitaciones.Select(h => new ProductoServicio<Habitacion>(det.ServicioId, det.Servicio.Nombre, h)
                        {
                            UriBase = det.UriBase
                        }));
                    }
                    catch (NotImplementedException) when (preferRest)
                    {
                        var detSoap = detalles.FirstOrDefault(d => d.ServicioId == det.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap);
                        if (detSoap is null) throw;

                        var uriSoap = BuildUri(detSoap, detSoap.ObtenerProductosEndpoint);
                        var habitaciones = await TravelioAPIConnector.Habitaciones.Connector.BuscarHabitacionesAsync(
                            uriSoap!,
                            filtros.FechaInicio,
                            filtros.FechaFin,
                            filtros.TipoHabitacion,
                            filtros.Capacidad,
                            filtros.PrecioMin,
                            filtros.PrecioMax,
                            true);

                        resultados.AddRange(habitaciones.Select(h => new ProductoServicio<Habitacion>(detSoap.ServicioId, detSoap.Servicio.Nombre, h)
                        {
                            UriBase = detSoap.UriBase
                        }));
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al buscar habitaciones en {Uri}", uri);
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar habitaciones");
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Habitacion>>? BuscarHabitaciones(FiltroHabitaciones filtros, ILogger? logger = null) =>
        BuscarHabitacionesAsync(filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Habitacion>>?> BuscarHabitacionesPorServicioAsync(int servicioId, FiltroHabitaciones filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.ServicioId == servicioId && d.Servicio.TipoServicio == TipoServicio.Hotel)
                .ToListAsync();
            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return Array.Empty<ProductoServicio<Habitacion>>();

            var uri = BuildUri(det, det.ObtenerProductosEndpoint);
            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            log.LogDebug("Buscando habitaciones en servicio {ServicioId} uri {Uri} con filtros {@Filtros}", servicioId, uri, filtros);
            try
            {
                var habitaciones = await TravelioAPIConnector.Habitaciones.Connector.BuscarHabitacionesAsync(
                    uri!,
                    filtros.FechaInicio,
                    filtros.FechaFin,
                    filtros.TipoHabitacion,
                    filtros.Capacidad,
                    filtros.PrecioMin,
                    filtros.PrecioMax,
                    forceSoap);

                return habitaciones.Select(h => new ProductoServicio<Habitacion>(det.ServicioId, det.Servicio.Nombre, h)
                {
                    UriBase = det.UriBase
                }).ToArray();
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ObtenerProductosEndpoint);
                var habitaciones = await TravelioAPIConnector.Habitaciones.Connector.BuscarHabitacionesAsync(
                    uriSoap!,
                    filtros.FechaInicio,
                    filtros.FechaFin,
                    filtros.TipoHabitacion,
                    filtros.Capacidad,
                    filtros.PrecioMin,
                    filtros.PrecioMax,
                    true);

                return habitaciones.Select(h => new ProductoServicio<Habitacion>(alternativo.ServicioId, alternativo.Servicio.Nombre, h)
                {
                    UriBase = alternativo.UriBase
                }).ToArray();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar habitaciones del servicio {ServicioId}", servicioId);
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Habitacion>>? BuscarHabitacionesPorServicio(int servicioId, FiltroHabitaciones filtros, ILogger? logger = null) =>
        BuscarHabitacionesPorServicioAsync(servicioId, filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Paquete>>?> BuscarPaquetesAsync(FiltroPaquetes filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.Servicio.TipoServicio == TipoServicio.PaquetesTuristicos)
                .ToListAsync();

            var preferRest = IsREST;
            var detallesFiltrados = FiltrarDetallesPorProtocolo(detalles, preferRest).ToList();
            var resultados = new List<ProductoServicio<Paquete>>();
            foreach (var det in detallesFiltrados)
            {
                var uri = BuildUri(det, det.ObtenerProductosEndpoint);
                var forceSoap = preferRest && det.TipoProtocolo == TipoProtocolo.Soap;
                try
                {
                    log.LogDebug("Buscando paquetes en {Uri} con filtros {@Filtros}", uri, filtros);
                    try
                    {
                        var paquetes = await TravelioAPIConnector.Paquetes.Connector.BuscarPaquetesAsync(
                            uri!,
                            filtros.Ciudad,
                            filtros.FechaInicio,
                            filtros.TipoActividad,
                            filtros.PrecioMax,
                            forceSoap);

                        resultados.AddRange(paquetes.Select(p => new ProductoServicio<Paquete>(det.ServicioId, det.Servicio.Nombre, p)
                        {
                            UriBase = det.UriBase
                        }));
                    }
                    catch (NotImplementedException) when (preferRest)
                    {
                        var detSoap = detalles.FirstOrDefault(d => d.ServicioId == det.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap);
                        if (detSoap is null) throw;

                        var uriSoap = BuildUri(detSoap, detSoap.ObtenerProductosEndpoint);
                        var paquetes = await TravelioAPIConnector.Paquetes.Connector.BuscarPaquetesAsync(
                            uriSoap!,
                            filtros.Ciudad,
                            filtros.FechaInicio,
                            filtros.TipoActividad,
                            filtros.PrecioMax,
                            true);

                        resultados.AddRange(paquetes.Select(p => new ProductoServicio<Paquete>(detSoap.ServicioId, detSoap.Servicio.Nombre, p)
                        {
                            UriBase = detSoap.UriBase
                        }));
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al buscar paquetes en {Uri}", uri);
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar paquetes turísticos");
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Paquete>>? BuscarPaquetes(FiltroPaquetes filtros, ILogger? logger = null) =>
        BuscarPaquetesAsync(filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Paquete>>?> BuscarPaquetesPorServicioAsync(int servicioId, FiltroPaquetes filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.ServicioId == servicioId && d.Servicio.TipoServicio == TipoServicio.PaquetesTuristicos)
                .ToListAsync();
            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return Array.Empty<ProductoServicio<Paquete>>();

            var uri = BuildUri(det, det.ObtenerProductosEndpoint);
            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            log.LogDebug("Buscando paquetes en servicio {ServicioId} uri {Uri} con filtros {@Filtros}", servicioId, uri, filtros);
            try
            {
                var paquetes = await TravelioAPIConnector.Paquetes.Connector.BuscarPaquetesAsync(
                    uri!,
                    filtros.Ciudad,
                    filtros.FechaInicio,
                    filtros.TipoActividad,
                    filtros.PrecioMax,
                    forceSoap);

                return paquetes.Select(p => new ProductoServicio<Paquete>(det.ServicioId, det.Servicio.Nombre, p)
                {
                    UriBase = det.UriBase
                }).ToArray();
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ObtenerProductosEndpoint);
                var paquetes = await TravelioAPIConnector.Paquetes.Connector.BuscarPaquetesAsync(
                    uriSoap!,
                    filtros.Ciudad,
                    filtros.FechaInicio,
                    filtros.TipoActividad,
                    filtros.PrecioMax,
                    true);

                return paquetes.Select(p => new ProductoServicio<Paquete>(alternativo.ServicioId, alternativo.Servicio.Nombre, p)
                {
                    UriBase = alternativo.UriBase
                }).ToArray();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar paquetes del servicio {ServicioId}", servicioId);
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Paquete>>? BuscarPaquetesPorServicio(int servicioId, FiltroPaquetes filtros, ILogger? logger = null) =>
        BuscarPaquetesPorServicioAsync(servicioId, filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Mesa>>?> BuscarMesasAsync(FiltroMesas filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.Servicio.TipoServicio == TipoServicio.Restaurante)
                .ToListAsync();

            var preferRest = IsREST;
            var detallesFiltrados = FiltrarDetallesPorProtocolo(detalles, preferRest).ToList();
            var resultados = new List<ProductoServicio<Mesa>>();
            foreach (var det in detallesFiltrados)
            {
                var uri = BuildUri(det, det.ObtenerProductosEndpoint);
                var forceSoap = preferRest && det.TipoProtocolo == TipoProtocolo.Soap;
                try
                {
                    log.LogDebug("Buscando mesas en {Uri} con filtros {@Filtros}", uri, filtros);
                    try
                    {
                        var mesas = await TravelioAPIConnector.Mesas.Connector.BuscarMesasAsync(
                            uri!,
                            filtros.Capacidad,
                            filtros.TipoMesa,
                            filtros.Estado,
                            forceSoap);

                        resultados.AddRange(mesas.Select(m => new ProductoServicio<Mesa>(det.ServicioId, det.Servicio.Nombre, m)
                        {
                            UriBase = det.UriBase
                        }));
                    }
                    catch (NotImplementedException) when (preferRest)
                    {
                        var detSoap = detalles.FirstOrDefault(d => d.ServicioId == det.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap);
                        if (detSoap is null) throw;

                        var uriSoap = BuildUri(detSoap, detSoap.ObtenerProductosEndpoint);
                        var mesas = await TravelioAPIConnector.Mesas.Connector.BuscarMesasAsync(
                            uriSoap!,
                            filtros.Capacidad,
                            filtros.TipoMesa,
                            filtros.Estado,
                            true);

                        resultados.AddRange(mesas.Select(m => new ProductoServicio<Mesa>(detSoap.ServicioId, detSoap.Servicio.Nombre, m)
                        {
                            UriBase = detSoap.UriBase
                        }));
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al buscar mesas en {Uri}", uri);
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar mesas");
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Mesa>>? BuscarMesas(FiltroMesas filtros, ILogger? logger = null) =>
        BuscarMesasAsync(filtros, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<ProductoServicio<Mesa>>?> BuscarMesasPorServicioAsync(int servicioId, FiltroMesas filtros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio.Include(d => d.Servicio)
                .Where(d => d.ServicioId == servicioId && d.Servicio.TipoServicio == TipoServicio.Restaurante)
                .ToListAsync();
            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return Array.Empty<ProductoServicio<Mesa>>();

            var uri = BuildUri(det, det.ObtenerProductosEndpoint);
            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            log.LogDebug("Buscando mesas en servicio {ServicioId} uri {Uri} con filtros {@Filtros}", servicioId, uri, filtros);
            try
            {
                var mesas = await TravelioAPIConnector.Mesas.Connector.BuscarMesasAsync(
                    uri!,
                    filtros.Capacidad,
                    filtros.TipoMesa,
                    filtros.Estado,
                    forceSoap);

                return mesas.Select(m => new ProductoServicio<Mesa>(det.ServicioId, det.Servicio.Nombre, m)
                {
                    UriBase = det.UriBase
                }).ToArray();
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ObtenerProductosEndpoint);
                var mesas = await TravelioAPIConnector.Mesas.Connector.BuscarMesasAsync(
                    uriSoap!,
                    filtros.Capacidad,
                    filtros.TipoMesa,
                    filtros.Estado,
                    true);

                return mesas.Select(m => new ProductoServicio<Mesa>(alternativo.ServicioId, alternativo.Servicio.Nombre, m)
                {
                    UriBase = alternativo.UriBase
                }).ToArray();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al buscar mesas del servicio {ServicioId}", servicioId);
            return null;
        }
    }

    public IReadOnlyList<ProductoServicio<Mesa>>? BuscarMesasPorServicio(int servicioId, FiltroMesas filtros, ILogger? logger = null) =>
        BuscarMesasPorServicioAsync(servicioId, filtros, logger).GetAwaiter().GetResult();

    #endregion

    #region Detalles y disponibilidad

    public async Task<ProductoServicio<Vuelo>?> ObtenerVueloAsync(int servicioId, string idVuelo, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var vuelos = await BuscarVuelosPorServicioAsync(servicioId, default, logger);
            if (vuelos is null) return null;

            foreach (var item in vuelos)
            {
                if (item.Producto.IdVuelo == idVuelo)
                {
                    return item;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener vuelo {IdVuelo} del servicio {ServicioId}", idVuelo, servicioId);
            return null;
        }
    }

    public async Task<ProductoServicio<Vehiculo>?> ObtenerAutoAsync(int servicioId, string idAuto, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var autos = await BuscarAutosPorServicioAsync(servicioId, default, logger);
            if (autos is null) return null;

            foreach (var item in autos)
            {
                if (item.Producto.IdAuto == idAuto)
                {
                    return item;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener auto {IdAuto} del servicio {ServicioId}", idAuto, servicioId);
            return null;
        }
    }

    public async Task<ProductoServicio<Habitacion>?> ObtenerHabitacionAsync(int servicioId, string idHabitacion, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var habitaciones = await BuscarHabitacionesPorServicioAsync(servicioId, default, logger);
            if (habitaciones is null) return null;

            foreach (var item in habitaciones)
            {
                if (item.Producto.IdHabitacion == idHabitacion)
                {
                    return item;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener habitaci\u00f3n {IdHabitacion} del servicio {ServicioId}", idHabitacion, servicioId);
            return null;
        }
    }

    public async Task<ProductoServicio<Paquete>?> ObtenerPaqueteAsync(int servicioId, string idPaquete, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var paquetes = await BuscarPaquetesPorServicioAsync(servicioId, default, logger);
            if (paquetes is null) return null;

            foreach (var item in paquetes)
            {
                if (item.Producto.IdPaquete == idPaquete)
                {
                    return item;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener paquete {IdPaquete} del servicio {ServicioId}", idPaquete, servicioId);
            return null;
        }
    }

    public async Task<ProductoServicio<Mesa>?> ObtenerMesaAsync(int servicioId, int idMesa, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var mesas = await BuscarMesasPorServicioAsync(servicioId, default, logger);
            if (mesas is null) return null;

            foreach (var item in mesas)
            {
                if (item.Producto.IdMesa == idMesa)
                {
                    return item;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener mesa {IdMesa} del servicio {ServicioId}", idMesa, servicioId);
            return null;
        }
    }

    public async Task<bool?> VerificarDisponibilidadVueloAsync(int servicioId, string idVuelo, int pasajeros, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return null;

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
            try
            {
                return await AerolineaConnector.VerificarDisponibilidadVueloAsync(uri!, idVuelo, pasajeros, forceSoap);
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                return await AerolineaConnector.VerificarDisponibilidadVueloAsync(uriSoap!, idVuelo, pasajeros, true);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar disponibilidad de vuelo {IdVuelo} en servicio {ServicioId}", idVuelo, servicioId);
            return null;
        }
    }

    public async Task<bool?> VerificarDisponibilidadAutoAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return null;

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
            try
            {
                return await AutosConnector.VerificarDisponibilidadAutoAsync(uri!, idAuto, fechaInicio, fechaFin, forceSoap);
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                return await AutosConnector.VerificarDisponibilidadAutoAsync(uriSoap!, idAuto, fechaInicio, fechaFin, true);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar disponibilidad de auto {IdAuto} en servicio {ServicioId}", idAuto, servicioId);
            return null;
        }
    }

    public async Task<bool?> VerificarDisponibilidadHabitacionAsync(int servicioId, string idHabitacion, DateTime fechaInicio, DateTime fechaFin, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return null;

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
            try
            {
                return await HabitacionesConnector.ValidarDisponibilidadAsync(uri!, idHabitacion, fechaInicio, fechaFin, forceSoap);
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                return await HabitacionesConnector.ValidarDisponibilidadAsync(uriSoap!, idHabitacion, fechaInicio, fechaFin, true);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar disponibilidad de habitaci\u00f3n {IdHabitacion} en servicio {ServicioId}", idHabitacion, servicioId);
            return null;
        }
    }

    public async Task<bool?> VerificarDisponibilidadPaqueteAsync(int servicioId, string idPaquete, DateTime fechaInicio, int personas, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return null;

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
            try
            {
                return await PaquetesConnector.ValidarDisponibilidadAsync(uri!, idPaquete, fechaInicio, personas, forceSoap);
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                return await PaquetesConnector.ValidarDisponibilidadAsync(uriSoap!, idPaquete, fechaInicio, personas, true);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar disponibilidad de paquete {IdPaquete} en servicio {ServicioId}", idPaquete, servicioId);
            return null;
        }
    }

    public async Task<bool?> VerificarDisponibilidadMesaAsync(int servicioId, int idMesa, DateTime fecha, int personas, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return null;

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
            try
            {
                return await MesasConnector.ValidarDisponibilidadAsync(uri!, idMesa, fecha, personas, forceSoap);
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                return await MesasConnector.ValidarDisponibilidadAsync(uriSoap!, idMesa, fecha, personas, true);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar disponibilidad de mesa {IdMesa} en servicio {ServicioId}", idMesa, servicioId);
            return null;
        }
    }

    public async Task<(bool exito, string mensaje)> CrearPrerreservaAutoAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return (false, "Servicio no encontrado");

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            try
            {
                var uri = BuildUri(det, det.CrearPrerreservaEndpoint);
                var (holdId, _) = await AutosConnector.CrearPrerreservaAsync(uri!, idAuto, fechaInicio, fechaFin, 300, forceSoap);
                return (true, $"Prerreserva creada: {holdId}");
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                var (holdId, _) = await AutosConnector.CrearPrerreservaAsync(uriSoap!, idAuto, fechaInicio, fechaFin, 300, true);
                return (true, $"Prerreserva creada: {holdId}");
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear prerreserva auto {IdAuto} en servicio {ServicioId}", idAuto, servicioId);
            return (false, "Error al crear prerreserva");
        }
    }

    public async Task<(bool exito, string mensaje, string? holdId)> CrearPrerreservaHabitacionAsync(
        int servicioId,
        string idHabitacion,
        DateTime fechaInicio,
        DateTime fechaFin,
        int numeroHuespedes,
        decimal precioActual,
        ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var (det, alternativo) = SeleccionarDetalle(detalles, IsREST);
            if (det is null) return (false, "Servicio no encontrado", null);

            var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
            try
            {
                var uri = BuildUri(det, det.CrearPrerreservaEndpoint);
                var holdId = await HabitacionesConnector.CrearPrerreservaAsync(
                    uri!, idHabitacion, fechaInicio, fechaFin, numeroHuespedes, 300, precioActual, forceSoap);
                return (true, $"Prerreserva creada: {holdId}", holdId);
            }
            catch (NotImplementedException) when (IsREST && alternativo is not null)
            {
                var uriSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                var holdId = await HabitacionesConnector.CrearPrerreservaAsync(
                    uriSoap!, idHabitacion, fechaInicio, fechaFin, numeroHuespedes, 300, precioActual, true);
                return (true, $"Prerreserva creada: {holdId}", holdId);
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear prerreserva habitaci\u00f3n {IdHabitacion} en servicio {ServicioId}", idHabitacion, servicioId);
            return (false, "Error al crear prerreserva", null);
        }
    }

    #endregion

    #region Carrito - agregar/consultar/eliminar

    public async Task<CarritoAerolineaItem?> AgregarVueloACarritoAsync(AerolineaCarritoRequest request, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var entity = new CarritoAerolineaItem
            {
                ClienteId = request.ClienteId,
                ServicioId = request.ServicioId,
                IdVueloProveedor = request.IdVueloProveedor,
                Origen = request.Origen,
                Destino = request.Destino,
                FechaVuelo = request.FechaVuelo,
                TipoCabina = request.TipoCabina,
                NombreAerolinea = request.NombreAerolinea,
                PrecioNormal = request.PrecioNormal,
                PrecioActual = request.PrecioActual,
                DescuentoPorcentaje = request.DescuentoPorcentaje,
                CantidadPasajeros = request.CantidadPasajeros
            };

            foreach (var p in request.Pasajeros)
            {
                entity.Pasajeros.Add(new CarritoAerolineaPasajero
                {
                    Nombre = p.Nombre,
                    Apellido = p.Apellido,
                    TipoIdentificacion = p.TipoIdentificacion,
                    Identificacion = p.Identificacion,
                    FechaNacimiento = p.FechaNacimiento
                });
            }

            _db.CarritosAerolinea.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al agregar vuelo al carrito");
            return null;
        }
    }

    public CarritoAerolineaItem? AgregarVueloACarrito(AerolineaCarritoRequest request, ILogger? logger = null) =>
        AgregarVueloACarritoAsync(request, logger).GetAwaiter().GetResult();

    public async Task<CarritoHabitacionItem?> AgregarHabitacionACarritoAsync(HabitacionCarritoRequest request, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var entity = new CarritoHabitacionItem
            {
                ClienteId = request.ClienteId,
                ServicioId = request.ServicioId,
                IdHabitacionProveedor = request.IdHabitacionProveedor,
                NombreHabitacion = request.NombreHabitacion,
                TipoHabitacion = request.TipoHabitacion,
                Hotel = request.Hotel,
                Ciudad = request.Ciudad,
                Pais = request.Pais,
                Capacidad = request.Capacidad,
                PrecioNormal = request.PrecioNormal,
                PrecioActual = request.PrecioActual,
                PrecioVigente = request.PrecioVigente,
                Amenidades = request.Amenidades,
                Imagenes = request.Imagenes,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin,
                NumeroHuespedes = request.NumeroHuespedes
            };

            _db.CarritosHabitaciones.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al agregar habitación al carrito");
            return null;
        }
    }

    public CarritoHabitacionItem? AgregarHabitacionACarrito(HabitacionCarritoRequest request, ILogger? logger = null) =>
        AgregarHabitacionACarritoAsync(request, logger).GetAwaiter().GetResult();

    public async Task<CarritoAutoItem?> AgregarAutoACarritoAsync(AutoCarritoRequest request, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var entity = new CarritoAutoItem
            {
                ClienteId = request.ClienteId,
                ServicioId = request.ServicioId,
                IdAutoProveedor = request.IdAutoProveedor,
                Tipo = request.Tipo,
                Categoria = request.Categoria,
                Transmision = request.Transmision,
                CapacidadPasajeros = request.CapacidadPasajeros,
                PrecioNormalPorDia = request.PrecioNormalPorDia,
                PrecioActualPorDia = request.PrecioActualPorDia,
                DescuentoPorcentaje = request.DescuentoPorcentaje,
                UriImagen = request.UriImagen,
                Ciudad = request.Ciudad,
                Pais = request.Pais,
                FechaInicio = request.FechaInicio,
                FechaFin = request.FechaFin
            };

            _db.CarritosAutos.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al agregar auto al carrito");
            return null;
        }
    }

    public CarritoAutoItem? AgregarAutoACarrito(AutoCarritoRequest request, ILogger? logger = null) =>
        AgregarAutoACarritoAsync(request, logger).GetAwaiter().GetResult();

    public async Task<CarritoPaqueteItem?> AgregarPaqueteACarritoAsync(PaqueteCarritoRequest request, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var entity = new CarritoPaqueteItem
            {
                ClienteId = request.ClienteId,
                ServicioId = request.ServicioId,
                IdPaqueteProveedor = request.IdPaqueteProveedor,
                Nombre = request.Nombre,
                Ciudad = request.Ciudad,
                Pais = request.Pais,
                TipoActividad = request.TipoActividad,
                Capacidad = request.Capacidad,
                PrecioNormal = request.PrecioNormal,
                PrecioActual = request.PrecioActual,
                ImagenUrl = request.ImagenUrl,
                Duracion = request.Duracion,
                FechaInicio = request.FechaInicio,
                Personas = request.Personas,
                BookingUserId = request.BookingUserId
            };

            foreach (var t in request.Turistas)
            {
                entity.Turistas.Add(new CarritoPaqueteTurista
                {
                    Nombre = t.Nombre,
                    Apellido = t.Apellido,
                    FechaNacimiento = t.FechaNacimiento,
                    TipoIdentificacion = t.TipoIdentificacion,
                    Identificacion = t.Identificacion
                });
            }

            _db.CarritosPaquetes.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al agregar paquete al carrito");
            return null;
        }
    }

    public CarritoPaqueteItem? AgregarPaqueteACarrito(PaqueteCarritoRequest request, ILogger? logger = null) =>
        AgregarPaqueteACarritoAsync(request, logger).GetAwaiter().GetResult();

    public async Task<CarritoMesaItem?> AgregarMesaACarritoAsync(MesaCarritoRequest request, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var entity = new CarritoMesaItem
            {
                ClienteId = request.ClienteId,
                ServicioId = request.ServicioId,
                IdMesa = request.IdMesa,
                IdRestaurante = request.IdRestaurante,
                NumeroMesa = request.NumeroMesa,
                TipoMesa = request.TipoMesa,
                Capacidad = request.Capacidad,
                Precio = request.Precio,
                ImagenUrl = request.ImagenUrl,
                EstadoMesa = request.EstadoMesa,
                FechaReserva = request.FechaReserva,
                NumeroPersonas = request.NumeroPersonas
            };

            _db.CarritosMesas.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al agregar mesa al carrito");
            return null;
        }
    }

    public CarritoMesaItem? AgregarMesaACarrito(MesaCarritoRequest request, ILogger? logger = null) =>
        AgregarMesaACarritoAsync(request, logger).GetAwaiter().GetResult();

    public async Task<bool?> EliminarItemCarritoAsync(TipoServicio tipo, int itemId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            switch (tipo)
            {
                case TipoServicio.Aerolinea:
                    var aero = await _db.CarritosAerolinea.FindAsync(itemId);
                    if (aero is null) return false;
                    _db.CarritosAerolinea.Remove(aero);
                    break;
                case TipoServicio.Hotel:
                    var hab = await _db.CarritosHabitaciones.FindAsync(itemId);
                    if (hab is null) return false;
                    _db.CarritosHabitaciones.Remove(hab);
                    break;
                case TipoServicio.RentaVehiculos:
                    var auto = await _db.CarritosAutos.FindAsync(itemId);
                    if (auto is null) return false;
                    _db.CarritosAutos.Remove(auto);
                    break;
                case TipoServicio.PaquetesTuristicos:
                    var paq = await _db.CarritosPaquetes.FindAsync(itemId);
                    if (paq is null) return false;
                    _db.CarritosPaquetes.Remove(paq);
                    break;
                case TipoServicio.Restaurante:
                    var mesa = await _db.CarritosMesas.FindAsync(itemId);
                    if (mesa is null) return false;
                    _db.CarritosMesas.Remove(mesa);
                    break;
                default:
                    return null;
            }

            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al eliminar item de carrito {Tipo} {ItemId}", tipo, itemId);
            return null;
        }
    }

    public bool? EliminarItemCarrito(TipoServicio tipo, int itemId, ILogger? logger = null) =>
        EliminarItemCarritoAsync(tipo, itemId, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<CarritoAerolineaItem>?> ObtenerCarritoVuelosAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.CarritosAerolinea
                .Include(c => c.Pasajeros)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener carrito de vuelos para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public IReadOnlyList<CarritoAerolineaItem>? ObtenerCarritoVuelos(int clienteId, ILogger? logger = null) =>
        ObtenerCarritoVuelosAsync(clienteId, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<CarritoHabitacionItem>?> ObtenerCarritoHabitacionesAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.CarritosHabitaciones
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener carrito de habitaciones para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public IReadOnlyList<CarritoHabitacionItem>? ObtenerCarritoHabitaciones(int clienteId, ILogger? logger = null) =>
        ObtenerCarritoHabitacionesAsync(clienteId, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<CarritoAutoItem>?> ObtenerCarritoAutosAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.CarritosAutos
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener carrito de autos para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public IReadOnlyList<CarritoAutoItem>? ObtenerCarritoAutos(int clienteId, ILogger? logger = null) =>
        ObtenerCarritoAutosAsync(clienteId, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<CarritoPaqueteItem>?> ObtenerCarritoPaquetesAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.CarritosPaquetes
                .Include(c => c.Turistas)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener carrito de paquetes para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public IReadOnlyList<CarritoPaqueteItem>? ObtenerCarritoPaquetes(int clienteId, ILogger? logger = null) =>
        ObtenerCarritoPaquetesAsync(clienteId, logger).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<CarritoMesaItem>?> ObtenerCarritoMesasAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.CarritosMesas
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener carrito de mesas para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public IReadOnlyList<CarritoMesaItem>? ObtenerCarritoMesas(int clienteId, ILogger? logger = null) =>
        ObtenerCarritoMesasAsync(clienteId, logger).GetAwaiter().GetResult();

    #endregion

    #region Disponibilidad y holds

    public async Task<bool?> VerificarDisponibilidadItemAsync(TipoServicio tipo, int itemId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            switch (tipo)
            {
                case TipoServicio.Aerolinea:
                    var vuelo = await _db.CarritosAerolinea.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                        .FirstOrDefaultAsync(c => c.Id == itemId);
                    if (vuelo is null) return null;
                    {
                        var (det, alternativo) = SeleccionarDetalle(vuelo.Servicio.DetallesServicio, IsREST);
                        if (det is null) return null;
                        var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                        var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
                        try
                        {
                            var disponible = await AerolineaConnector.VerificarDisponibilidadVueloAsync(uri!, vuelo.IdVueloProveedor, vuelo.CantidadPasajeros, forceSoap);
                            return disponible;
                        }
                        catch (NotImplementedException) when (IsREST && alternativo is not null)
                        {
                            var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                            return await AerolineaConnector.VerificarDisponibilidadVueloAsync(uriSoap!, vuelo.IdVueloProveedor, vuelo.CantidadPasajeros, true);
                        }
                    }
                case TipoServicio.Hotel:
                    var hab = await _db.CarritosHabitaciones.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                        .FirstOrDefaultAsync(c => c.Id == itemId);
                    if (hab is null) return null;
                    {
                        var (det, alternativo) = SeleccionarDetalle(hab.Servicio.DetallesServicio, IsREST);
                        if (det is null) return null;
                        var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                        var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
                        try
                        {
                            var disponible = await TravelioAPIConnector.Habitaciones.Connector.ValidarDisponibilidadAsync(
                                uri!, hab.IdHabitacionProveedor, hab.FechaInicio, hab.FechaFin, forceSoap);
                            return disponible;
                        }
                        catch (NotImplementedException) when (IsREST && alternativo is not null)
                        {
                            var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                            return await TravelioAPIConnector.Habitaciones.Connector.ValidarDisponibilidadAsync(
                                uriSoap!, hab.IdHabitacionProveedor, hab.FechaInicio, hab.FechaFin, true);
                        }
                    }
                case TipoServicio.RentaVehiculos:
                    var auto = await _db.CarritosAutos.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                        .FirstOrDefaultAsync(c => c.Id == itemId);
                    if (auto is null) return null;
                    {
                        var (det, alternativo) = SeleccionarDetalle(auto.Servicio.DetallesServicio, IsREST);
                        if (det is null) return null;
                        var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                        var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
                        try
                        {
                            var disponible = await TravelioAPIConnector.Autos.Connector.VerificarDisponibilidadAutoAsync(
                                uri!, auto.IdAutoProveedor, auto.FechaInicio, auto.FechaFin, forceSoap);
                            return disponible;
                        }
                        catch (NotImplementedException) when (IsREST && alternativo is not null)
                        {
                            var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                            return await TravelioAPIConnector.Autos.Connector.VerificarDisponibilidadAutoAsync(
                                uriSoap!, auto.IdAutoProveedor, auto.FechaInicio, auto.FechaFin, true);
                        }
                    }
                case TipoServicio.PaquetesTuristicos:
                    var paq = await _db.CarritosPaquetes.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                        .FirstOrDefaultAsync(c => c.Id == itemId);
                    if (paq is null) return null;
                    {
                        var (det, alternativo) = SeleccionarDetalle(paq.Servicio.DetallesServicio, IsREST);
                        if (det is null) return null;
                        var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                        var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
                        try
                        {
                            var disponible = await TravelioAPIConnector.Paquetes.Connector.ValidarDisponibilidadAsync(
                                uri!, paq.IdPaqueteProveedor, paq.FechaInicio, paq.Personas, forceSoap);
                            return disponible;
                        }
                        catch (NotImplementedException) when (IsREST && alternativo is not null)
                        {
                            var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                            return await TravelioAPIConnector.Paquetes.Connector.ValidarDisponibilidadAsync(
                                uriSoap!, paq.IdPaqueteProveedor, paq.FechaInicio, paq.Personas, true);
                        }
                    }
                case TipoServicio.Restaurante:
                    var mesa = await _db.CarritosMesas.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                        .FirstOrDefaultAsync(c => c.Id == itemId);
                    if (mesa is null) return null;
                    {
                        var (det, alternativo) = SeleccionarDetalle(mesa.Servicio.DetallesServicio, IsREST);
                        if (det is null) return null;
                        var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                        var uri = BuildUri(det, det.ConfirmarProductoEndpoint);
                        try
                        {
                            var disponible = await TravelioAPIConnector.Mesas.Connector.ValidarDisponibilidadAsync(
                                uri!, mesa.IdMesa, mesa.FechaReserva, mesa.NumeroPersonas, forceSoap);
                            return disponible;
                        }
                        catch (NotImplementedException) when (IsREST && alternativo is not null)
                        {
                            var uriSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                            return await TravelioAPIConnector.Mesas.Connector.ValidarDisponibilidadAsync(
                                uriSoap!, mesa.IdMesa, mesa.FechaReserva, mesa.NumeroPersonas, true);
                        }
                    }
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar disponibilidad {Tipo} item {ItemId}", tipo, itemId);
            return null;
        }
    }

    public bool? VerificarDisponibilidadItem(TipoServicio tipo, int itemId, ILogger? logger = null) =>
        VerificarDisponibilidadItemAsync(tipo, itemId, logger).GetAwaiter().GetResult();

    public async Task<bool?> CrearHoldsParaUsuarioAsync(int clienteId, int duracionHoldSegundos = 300, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var created = false;

            var vuelos = await _db.CarritosAerolinea.Include(c => c.Pasajeros)
                .Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
            foreach (var item in vuelos)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;

                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var uriDisponibilidad = BuildUri(det, det.ConfirmarProductoEndpoint);
                log.LogDebug("Verificando disponibilidad vuelo {IdVuelo} en {Uri}", item.IdVueloProveedor, uriDisponibilidad);
                bool disponible;
                try
                {
                    disponible = await AerolineaConnector.VerificarDisponibilidadVueloAsync(uriDisponibilidad!, item.IdVueloProveedor, item.CantidadPasajeros, forceSoap);
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriDispSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                    disponible = await AerolineaConnector.VerificarDisponibilidadVueloAsync(uriDispSoap!, item.IdVueloProveedor, item.CantidadPasajeros, true);
                }
                if (!disponible)
                {
                    log.LogError("Vuelo no disponible {IdVuelo}", item.IdVueloProveedor);
                    return false;
                }

                var pasajeros = item.Pasajeros.Select(p => (p.Nombre, p.Apellido, p.TipoIdentificacion, p.Identificacion, new DateTime(p.FechaNacimiento.Year, p.FechaNacimiento.Month, p.FechaNacimiento.Day))).ToArray();
                var uriHold = BuildUri(det, det.CrearPrerreservaEndpoint);
                log.LogDebug("Creando hold de vuelo en {Uri} por {Segundos} segundos", uriHold, duracionHoldSegundos);
                try
                {
                    var hold = await AerolineaConnector.CrearPrerreservaVueloAsync(uriHold!, item.IdVueloProveedor, pasajeros, duracionHoldSegundos, forceSoap);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.expira;
                    created = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriHoldSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                    var hold = await AerolineaConnector.CrearPrerreservaVueloAsync(uriHoldSoap!, item.IdVueloProveedor, pasajeros, duracionHoldSegundos, true);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.expira;
                    created = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear hold de vuelo {IdVuelo}, se elimina del carrito", item.IdVueloProveedor);
                    _db.CarritosAerolinea.Remove(item);
                }
            }

            var habitaciones = await _db.CarritosHabitaciones.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
            foreach (var item in habitaciones)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var uriDisp = BuildUri(det, det.ConfirmarProductoEndpoint);
                bool disponible;
                try
                {
                    disponible = await TravelioAPIConnector.Habitaciones.Connector.ValidarDisponibilidadAsync(uriDisp!, item.IdHabitacionProveedor, item.FechaInicio, item.FechaFin, forceSoap);
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriDispSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                    disponible = await TravelioAPIConnector.Habitaciones.Connector.ValidarDisponibilidadAsync(
                        uriDispSoap!, item.IdHabitacionProveedor, item.FechaInicio, item.FechaFin, true);
                }
                if (!disponible)
                {
                    log.LogError("Habitación no disponible {Id}", item.IdHabitacionProveedor);
                    return false;
                }

                var uriHold = BuildUri(det, det.CrearPrerreservaEndpoint);
                log.LogDebug("Creando hold de habitación en {Uri}", uriHold);
                try
                {
                    var holdId = await TravelioAPIConnector.Habitaciones.Connector.CrearPrerreservaAsync(
                        uriHold!, item.IdHabitacionProveedor, item.FechaInicio, item.FechaFin, item.NumeroHuespedes, duracionHoldSegundos, item.PrecioActual, forceSoap);
                    item.HoldId = holdId;
                    item.HoldExpira = null;
                    created = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriHoldSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                    var holdId = await TravelioAPIConnector.Habitaciones.Connector.CrearPrerreservaAsync(
                        uriHoldSoap!, item.IdHabitacionProveedor, item.FechaInicio, item.FechaFin, item.NumeroHuespedes, duracionHoldSegundos, item.PrecioActual, true);
                    item.HoldId = holdId;
                    item.HoldExpira = null;
                    created = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear hold de habitación {Id}, se elimina del carrito", item.IdHabitacionProveedor);
                    _db.CarritosHabitaciones.Remove(item);
                }
            }

            var autos = await _db.CarritosAutos.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
            foreach (var item in autos)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var uriDisp = BuildUri(det, det.ConfirmarProductoEndpoint);
                bool disponible;
                try
                {
                    disponible = await TravelioAPIConnector.Autos.Connector.VerificarDisponibilidadAutoAsync(
                        uriDisp!, item.IdAutoProveedor, item.FechaInicio, item.FechaFin, forceSoap);
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriDispSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                    disponible = await TravelioAPIConnector.Autos.Connector.VerificarDisponibilidadAutoAsync(
                        uriDispSoap!, item.IdAutoProveedor, item.FechaInicio, item.FechaFin, true);
                }
                if (!disponible)
                {
                    log.LogError("Auto no disponible {Id}", item.IdAutoProveedor);
                    return false;
                }

                var uriHold = BuildUri(det, det.CrearPrerreservaEndpoint);
                log.LogDebug("Creando hold de auto en {Uri}", uriHold);
                try
                {
                    var hold = await TravelioAPIConnector.Autos.Connector.CrearPrerreservaAsync(
                        uriHold!, item.IdAutoProveedor, item.FechaInicio, item.FechaFin, duracionHoldSegundos, forceSoap);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.holdExpiration;
                    created = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriHoldSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                    var hold = await TravelioAPIConnector.Autos.Connector.CrearPrerreservaAsync(
                        uriHoldSoap!, item.IdAutoProveedor, item.FechaInicio, item.FechaFin, duracionHoldSegundos, true);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.holdExpiration;
                    created = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear hold de auto {Id}, se elimina del carrito", item.IdAutoProveedor);
                    _db.CarritosAutos.Remove(item);
                }
            }

            var paquetes = await _db.CarritosPaquetes.Include(c => c.Turistas)
                .Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
            foreach (var item in paquetes)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var uriDisp = BuildUri(det, det.ConfirmarProductoEndpoint);
                bool disponible;
                try
                {
                    disponible = await TravelioAPIConnector.Paquetes.Connector.ValidarDisponibilidadAsync(
                        uriDisp!, item.IdPaqueteProveedor, item.FechaInicio, item.Personas, forceSoap);
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriDispSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                    disponible = await TravelioAPIConnector.Paquetes.Connector.ValidarDisponibilidadAsync(
                        uriDispSoap!, item.IdPaqueteProveedor, item.FechaInicio, item.Personas, true);
                }
                if (!disponible)
                {
                    log.LogError("Paquete no disponible {Id}", item.IdPaqueteProveedor);
                    return false;
                }

                var uriHold = BuildUri(det, det.CrearPrerreservaEndpoint);
                log.LogDebug("Creando hold de paquete en {Uri}", uriHold);
                try
                {
                    var hold = await TravelioAPIConnector.Paquetes.Connector.CrearHoldAsync(
                        uriHold!, item.IdPaqueteProveedor, item.BookingUserId, item.FechaInicio, item.Personas, duracionHoldSegundos, forceSoap);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.expira;
                    created = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriHoldSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                    var hold = await TravelioAPIConnector.Paquetes.Connector.CrearHoldAsync(
                        uriHoldSoap!, item.IdPaqueteProveedor, item.BookingUserId, item.FechaInicio, item.Personas, duracionHoldSegundos, true);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.expira;
                    created = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear hold de paquete {Id}, se elimina del carrito", item.IdPaqueteProveedor);
                    _db.CarritosPaquetes.Remove(item);
                }
            }

            var mesas = await _db.CarritosMesas.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId)
                .ToListAsync();
            foreach (var item in mesas)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var uriDisp = BuildUri(det, det.ConfirmarProductoEndpoint);
                bool disponible;
                try
                {
                    disponible = await TravelioAPIConnector.Mesas.Connector.ValidarDisponibilidadAsync(
                        uriDisp!, item.IdMesa, item.FechaReserva, item.NumeroPersonas, forceSoap);
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriDispSoap = BuildUri(alternativo, alternativo.ConfirmarProductoEndpoint);
                    disponible = await TravelioAPIConnector.Mesas.Connector.ValidarDisponibilidadAsync(
                        uriDispSoap!, item.IdMesa, item.FechaReserva, item.NumeroPersonas, true);
                }
                if (!disponible)
                {
                    log.LogError("Mesa no disponible {Id}", item.IdMesa);
                    return false;
                }

                var uriHold = BuildUri(det, det.CrearPrerreservaEndpoint);
                log.LogDebug("Creando hold de mesa en {Uri}", uriHold);
                try
                {
                    var hold = await TravelioAPIConnector.Mesas.Connector.CrearPreReservaAsync(
                        uriHold!, item.IdMesa, item.FechaReserva, item.NumeroPersonas, duracionHoldSegundos, forceSoap);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.expira;
                    created = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriHoldSoap = BuildUri(alternativo, alternativo.CrearPrerreservaEndpoint);
                    var hold = await TravelioAPIConnector.Mesas.Connector.CrearPreReservaAsync(
                        uriHoldSoap!, item.IdMesa, item.FechaReserva, item.NumeroPersonas, duracionHoldSegundos, true);
                    item.HoldId = hold.holdId;
                    item.HoldExpira = hold.expira;
                    created = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear hold de mesa {Id}, se elimina del carrito", item.IdMesa);
                    _db.CarritosMesas.Remove(item);
                }
            }

            await _db.SaveChangesAsync();
            return created;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear holds para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public bool? CrearHoldsParaUsuario(int clienteId, int duracionHoldSegundos = 300, ILogger? logger = null) =>
        CrearHoldsParaUsuarioAsync(clienteId, duracionHoldSegundos, logger).GetAwaiter().GetResult();

    #endregion

    public async Task<CheckoutResult> ProcesarCheckoutAsync(
        int clienteId,
        int cuentaBancariaCliente,
        IReadOnlyList<CheckoutItem> items,
        FacturaInfo facturaInfo,
        ILogger? logger = null)
    {
        var resultado = new CheckoutResult();
        var log = ResolveLogger(logger);

        try
        {
            var cliente = await _db.Clientes.FindAsync(clienteId);
            if (cliente is null)
            {
                resultado.Mensaje = "Cliente no encontrado.";
                return resultado;
            }

            decimal totalCarrito = items.Sum(i => i.PrecioFinal * i.Cantidad);
            decimal iva = totalCarrito * 0.12m;
            decimal totalConIva = totalCarrito + iva;

            log.LogInformation("Procesando checkout para cliente {ClienteId}. Total: ${Total}", clienteId, totalConIva);

            var cobroExitoso = await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: TransferirClass.cuentaDefaultTravelio,
                monto: totalConIva,
                cuentaOrigen: cuentaBancariaCliente
            );

            if (!cobroExitoso)
            {
                log.LogWarning("Fallo el cobro al cliente {ClienteId}", clienteId);
                resultado.Mensaje = "No se pudo procesar el pago. Verifica tu saldo o cuenta bancaria.";
                return resultado;
            }

            log.LogInformation("Cobro exitoso de ${Monto} al cliente {ClienteId}", totalConIva, clienteId);

            var compra = new Compra
            {
                ClienteId = clienteId,
                FechaCompra = DateTime.UtcNow,
                ValorPagado = totalConIva
            };
            _db.Compras.Add(compra);
            await _db.SaveChangesAsync();

            resultado.CompraId = compra.Id;
            resultado.TotalPagado = totalConIva;

            foreach (var item in items)
            {
                var reservaResult = new ReservaResult { Tipo = item.Tipo, Titulo = item.Titulo };

                try
                {
                    switch (item.Tipo)
                    {
                        case "CAR":
                            await ProcesarReservaAutoAsync(item, cliente, facturaInfo, compra, reservaResult, log);
                            break;
                        case "HOTEL":
                            await ProcesarReservaHotelAsync(item, cliente, facturaInfo, compra, reservaResult, log);
                            break;
                        case "FLIGHT":
                            await ProcesarReservaVueloAsync(item, cliente, facturaInfo, compra, reservaResult, log);
                            break;
                        case "RESTAURANT":
                            await ProcesarReservaMesaAsync(item, cliente, facturaInfo, compra, reservaResult, log);
                            break;
                        case "PACKAGE":
                            await ProcesarReservaPaqueteAsync(item, cliente, facturaInfo, compra, reservaResult, log);
                            break;
                        default:
                            reservaResult.Error = $"Tipo de servicio desconocido: {item.Tipo}";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error procesando reserva para {Titulo}", item.Titulo);
                    reservaResult.Error = "Error al procesar la reserva";
                }

                resultado.Reservas.Add(reservaResult);
            }

            var todasExitosas = resultado.Reservas.All(r => r.Exitoso);
            var algunaExitosa = resultado.Reservas.Any(r => r.Exitoso);

            if (todasExitosas)
            {
                resultado.Exitoso = true;
                resultado.Mensaje = "Compra realizada con exito! Tus reservas han sido confirmadas.";
            }
            else if (algunaExitosa)
            {
                resultado.Exitoso = true;
                resultado.Mensaje = "Compra parcialmente exitosa. Algunas reservas no pudieron procesarse.";
            }
            else
            {
                resultado.Mensaje = "No se pudieron procesar las reservas. Se intentara reembolsar el pago.";
                await TransferirClass.RealizarTransferenciaAsync(
                    cuentaDestino: cuentaBancariaCliente,
                    monto: totalConIva,
                    cuentaOrigen: TransferirClass.cuentaDefaultTravelio
                );
            }

            await _db.SaveChangesAsync();
            return resultado;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error en checkout para cliente {ClienteId}", clienteId);
            resultado.Mensaje = "Error inesperado al procesar la compra.";
            return resultado;
        }
    }

    #region Checkout helpers

    private async Task<(DetalleServicio? rest, DetalleServicio? soap, Servicio? servicio)> ObtenerDetallesServicioAsync(int servicioId, ILogger log)
    {
        try
        {
            var detalles = await _db.DetallesServicio
                .Include(d => d.Servicio)
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var rest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var soap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);
            var servicio = detalles.FirstOrDefault()?.Servicio;

            return (rest, soap, servicio);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener detalles del servicio {ServicioId}", servicioId);
            return (null, null, null);
        }
    }

    private static string ResolveNombreFactura(Cliente cliente, FacturaInfo facturaInfo)
    {
        if (!string.IsNullOrWhiteSpace(facturaInfo.NombreFactura))
        {
            return facturaInfo.NombreFactura;
        }

        return $"{cliente.Nombre} {cliente.Apellido}".Trim();
    }

    private static (string nombre, string apellido) ResolveNombreApellidoFactura(Cliente cliente, FacturaInfo facturaInfo)
    {
        if (!string.IsNullOrWhiteSpace(facturaInfo.NombreFactura))
        {
            var partes = facturaInfo.NombreFactura.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var nombre = partes.FirstOrDefault() ?? cliente.Nombre;
            var apellido = partes.Skip(1).FirstOrDefault() ?? cliente.Apellido;
            return (nombre, apellido);
        }

        return (cliente.Nombre, cliente.Apellido);
    }

    private async Task ProcesarReservaAutoAsync(
        CheckoutItem item,
        Cliente cliente,
        FacturaInfo facturaInfo,
        Compra compra,
        ReservaResult reservaResult,
        ILogger log)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId, log);

        if (servicio is null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        if (!item.FechaInicio.HasValue || !item.FechaFin.HasValue)
        {
            reservaResult.Error = "Fechas de reserva no validas";
            return;
        }

        var fechaInicio = item.FechaInicio.Value;
        var fechaFin = item.FechaFin.Value;
        bool usandoRest = false;

        log.LogInformation("Procesando reserva de auto {IdAuto} en {Servicio}", item.IdProducto, servicio.Nombre);

        await CrearClienteExternoAutoAsync(detalleRest, detalleSoap, cliente, log);

        string holdId = string.Empty;
        Exception? ultimoError = null;

        if (detalleRest is not null)
        {
            try
            {
                var uri = BuildUri(detalleRest, detalleRest.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva auto (REST): {Uri}", uri);
                    (holdId, _) = await AutosConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin);
                    usandoRest = true;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "REST fallo para prerreserva auto");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap is not null)
        {
            try
            {
                var uri = BuildUri(detalleSoap, detalleSoap.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva auto (SOAP): {Uri}", uri);
                    (holdId, _) = await AutosConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin, forceSoap: true);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SOAP tambien fallo para prerreserva auto");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrWhiteSpace(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        log.LogInformation("Prerreserva auto creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = BuildUri(detalle, detalle.CrearReservaEndpoint);
        if (string.IsNullOrWhiteSpace(uriReserva))
        {
            reservaResult.Error = "Endpoint de reserva no configurado";
            return;
        }

        var reservaId = await AutosConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            cliente.Nombre,
            cliente.Apellido,
            cliente.TipoIdentificacion,
            cliente.DocumentoIdentidad,
            cliente.CorreoElectronico,
            fechaInicio,
            fechaFin,
            forceSoap: !usandoRest);

        log.LogInformation("Reserva auto creada: {ReservaId}", reservaId);
        reservaResult.CodigoReserva = reservaId.ToString();

        try
        {
            var uriFactura = BuildUri(detalle, detalle.GenerarFacturaEndpoint);
            if (!string.IsNullOrWhiteSpace(uriFactura))
            {
                decimal subtotal = item.PrecioFinal;
                decimal iva = subtotal * 0.12m;
                decimal total = subtotal + iva;

                var nombreFactura = ResolveNombreFactura(cliente, facturaInfo);
                var facturaUrl = await AutosConnector.GenerarFacturaAsync(
                    uriFactura,
                    reservaId,
                    subtotal,
                    iva,
                    total,
                    (nombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                    forceSoap: !usandoRest);

                reservaResult.FacturaProveedorUrl = facturaUrl;
            }
            else
            {
                log.LogWarning("No se pudo generar factura auto: endpoint no configurado");
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo generar factura auto");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal, log);
        await RegistrarReservaEnDbAsync(item.ServicioId, reservaId.ToString(), reservaResult.FacturaProveedorUrl, compra, item.PrecioFinal);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoAutoAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente, ILogger log)
    {
        try
        {
            if (rest is not null)
            {
                try
                {
                    var uri = BuildUri(rest, rest.RegistrarClienteEndpoint);
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        await AutosConnector.CrearClienteExternoAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "REST fallo al crear cliente externo auto");
                }
            }

            if (soap is not null)
            {
                var uri = BuildUri(soap, soap.RegistrarClienteEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    await AutosConnector.CrearClienteExternoAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, forceSoap: true);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo crear cliente externo auto");
        }
    }

    private async Task ProcesarReservaHotelAsync(
        CheckoutItem item,
        Cliente cliente,
        FacturaInfo facturaInfo,
        Compra compra,
        ReservaResult reservaResult,
        ILogger log)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId, log);

        if (servicio is null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        if (!item.FechaInicio.HasValue || !item.FechaFin.HasValue)
        {
            reservaResult.Error = "Fechas de reserva no validas";
            return;
        }

        var fechaInicio = item.FechaInicio.Value;
        var fechaFin = item.FechaFin.Value;
        var numeroHuespedes = item.NumeroPersonas ?? 2;
        bool usandoRest = false;

        log.LogInformation("Procesando reserva hotel {IdHabitacion} en {Servicio}", item.IdProducto, servicio.Nombre);

        await CrearClienteExternoHotelAsync(detalleRest, detalleSoap, cliente, log);

        string holdId = string.Empty;
        Exception? ultimoError = null;

        if (detalleRest is not null)
        {
            try
            {
                var uri = BuildUri(detalleRest, detalleRest.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva hotel (REST): {Uri}", uri);
                    holdId = await HabitacionesConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin, numeroHuespedes, 300, item.PrecioUnitario);
                    usandoRest = true;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "REST fallo para prerreserva hotel");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap is not null)
        {
            try
            {
                var uri = BuildUri(detalleSoap, detalleSoap.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva hotel (SOAP): {Uri}", uri);
                    holdId = await HabitacionesConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin, numeroHuespedes, 300, item.PrecioUnitario, forceSoap: true);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SOAP tambien fallo para prerreserva hotel");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrWhiteSpace(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        log.LogInformation("Prerreserva hotel creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = BuildUri(detalle, detalle.CrearReservaEndpoint);
        if (string.IsNullOrWhiteSpace(uriReserva))
        {
            reservaResult.Error = "Endpoint de reserva no configurado";
            return;
        }

        var reservaId = await HabitacionesConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            cliente.Nombre,
            cliente.Apellido,
            cliente.CorreoElectronico,
            cliente.TipoIdentificacion,
            cliente.DocumentoIdentidad,
            fechaInicio,
            fechaFin,
            numeroHuespedes,
            forceSoap: !usandoRest);

        log.LogInformation("Reserva hotel creada: {ReservaId}", reservaId);
        reservaResult.CodigoReserva = reservaId.ToString();

        try
        {
            var uriFactura = BuildUri(detalle, detalle.GenerarFacturaEndpoint);
            if (!string.IsNullOrWhiteSpace(uriFactura))
            {
                var (nombreFactura, apellidoFactura) = ResolveNombreApellidoFactura(cliente, facturaInfo);
                var facturaUrl = await HabitacionesConnector.EmitirFacturaAsync(
                    uriFactura,
                    reservaId,
                    nombreFactura,
                    apellidoFactura,
                    facturaInfo.TipoDocumento,
                    facturaInfo.Documento,
                    facturaInfo.CorreoFactura,
                    forceSoap: !usandoRest);

                reservaResult.FacturaProveedorUrl = facturaUrl;
            }
            else
            {
                log.LogWarning("No se pudo generar factura hotel: endpoint no configurado");
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo generar factura hotel");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal, log);
        await RegistrarReservaEnDbAsync(item.ServicioId, reservaId.ToString(), reservaResult.FacturaProveedorUrl, compra, item.PrecioFinal);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoHotelAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente, ILogger log)
    {
        try
        {
            if (rest is not null)
            {
                try
                {
                    var uri = BuildUri(rest, rest.RegistrarClienteEndpoint);
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        await HabitacionesConnector.CrearUsuarioExternoAsync(uri, cliente.CorreoElectronico, cliente.Nombre, cliente.Apellido);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "REST fallo al crear cliente externo hotel");
                }
            }

            if (soap is not null)
            {
                var uri = BuildUri(soap, soap.RegistrarClienteEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    await HabitacionesConnector.CrearUsuarioExternoAsync(uri, cliente.CorreoElectronico, cliente.Nombre, cliente.Apellido, forceSoap: true);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo crear cliente externo hotel");
        }
    }

    private async Task ProcesarReservaVueloAsync(
        CheckoutItem item,
        Cliente cliente,
        FacturaInfo facturaInfo,
        Compra compra,
        ReservaResult reservaResult,
        ILogger log)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId, log);

        if (servicio is null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        bool usandoRest = false;

        log.LogInformation("Procesando reserva vuelo {IdVuelo} en {Servicio}", item.IdProducto, servicio.Nombre);

        var pasajeros = new (string, string, string, string, DateTime)[]
        {
            (cliente.Nombre, cliente.Apellido, cliente.TipoIdentificacion, cliente.DocumentoIdentidad, DateTime.Now.AddYears(-30))
        };

        await CrearClienteExternoVueloAsync(detalleRest, detalleSoap, cliente, log);

        string holdId = string.Empty;
        Exception? ultimoError = null;

        if (detalleRest is not null)
        {
            try
            {
                var uri = BuildUri(detalleRest, detalleRest.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva vuelo (REST): {Uri}", uri);
                    (holdId, _) = await AerolineaConnector.CrearPrerreservaVueloAsync(uri, item.IdProducto, pasajeros, 300);
                    usandoRest = true;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "REST fallo para prerreserva vuelo");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap is not null)
        {
            try
            {
                var uri = BuildUri(detalleSoap, detalleSoap.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva vuelo (SOAP): {Uri}", uri);
                    (holdId, _) = await AerolineaConnector.CrearPrerreservaVueloAsync(uri, item.IdProducto, pasajeros, 300, forceSoap: true);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SOAP tambien fallo para prerreserva vuelo");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrWhiteSpace(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        log.LogInformation("Prerreserva vuelo creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = BuildUri(detalle, detalle.CrearReservaEndpoint);
        if (string.IsNullOrWhiteSpace(uriReserva))
        {
            reservaResult.Error = "Endpoint de reserva no configurado";
            return;
        }

        var (idReserva, codigoReserva, _) = await AerolineaConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            cliente.CorreoElectronico,
            pasajeros,
            forceSoap: !usandoRest);

        log.LogInformation("Reserva vuelo creada: {IdReserva}", idReserva);
        reservaResult.CodigoReserva = codigoReserva;

        try
        {
            var uriFactura = BuildUri(detalle, detalle.GenerarFacturaEndpoint);
            if (!string.IsNullOrWhiteSpace(uriFactura))
            {
                decimal subtotal = item.PrecioFinal;
                decimal iva = subtotal * 0.12m;
                decimal total = subtotal + iva;

                var nombreFactura = ResolveNombreFactura(cliente, facturaInfo);
                var facturaUrl = await AerolineaConnector.GenerarFacturaAsync(
                    uriFactura,
                    idReserva,
                    subtotal,
                    iva,
                    total,
                    (nombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                    forceSoap: !usandoRest);

                reservaResult.FacturaProveedorUrl = facturaUrl;
            }
            else
            {
                log.LogWarning("No se pudo generar factura vuelo: endpoint no configurado");
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo generar factura vuelo");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal, log);
        await RegistrarReservaEnDbAsync(item.ServicioId, reservaResult.CodigoReserva, reservaResult.FacturaProveedorUrl, compra, item.PrecioFinal);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoVueloAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente, ILogger log)
    {
        try
        {
            if (rest is not null)
            {
                try
                {
                    var uri = BuildUri(rest, rest.RegistrarClienteEndpoint);
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        await AerolineaConnector.CrearClienteExternoAsync(
                            uri,
                            cliente.CorreoElectronico,
                            cliente.Nombre,
                            cliente.Apellido,
                            DateTime.Now.AddYears(-30),
                            cliente.TipoIdentificacion,
                            cliente.DocumentoIdentidad);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "REST fallo al crear cliente externo vuelo");
                }
            }

            if (soap is not null)
            {
                var uri = BuildUri(soap, soap.RegistrarClienteEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    await AerolineaConnector.CrearClienteExternoAsync(
                        uri,
                        cliente.CorreoElectronico,
                        cliente.Nombre,
                        cliente.Apellido,
                        DateTime.Now.AddYears(-30),
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        forceSoap: true);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo crear cliente externo vuelo");
        }
    }

    private async Task ProcesarReservaMesaAsync(
        CheckoutItem item,
        Cliente cliente,
        FacturaInfo facturaInfo,
        Compra compra,
        ReservaResult reservaResult,
        ILogger log)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId, log);

        if (servicio is null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        if (!int.TryParse(item.IdProducto, out var idMesa))
        {
            reservaResult.Error = "Id de mesa invalido";
            return;
        }

        var fecha = item.FechaInicio ?? DateTime.Today;
        var personas = item.NumeroPersonas ?? 2;
        bool usandoRest = false;

        log.LogInformation("Procesando reserva mesa {IdMesa} en {Servicio}", idMesa, servicio.Nombre);

        await CrearClienteExternoMesaAsync(detalleRest, detalleSoap, cliente, log);

        string holdId = string.Empty;
        Exception? ultimoError = null;

        if (detalleRest is not null)
        {
            try
            {
                var uri = BuildUri(detalleRest, detalleRest.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva mesa (REST): {Uri}", uri);
                    (holdId, _) = await MesasConnector.CrearPreReservaAsync(uri, idMesa, fecha, personas, 300);
                    usandoRest = true;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "REST fallo para prerreserva mesa");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap is not null)
        {
            try
            {
                var uri = BuildUri(detalleSoap, detalleSoap.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando prerreserva mesa (SOAP): {Uri}", uri);
                    (holdId, _) = await MesasConnector.CrearPreReservaAsync(uri, idMesa, fecha, personas, 300, forceSoap: true);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SOAP tambien fallo para prerreserva mesa");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrWhiteSpace(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        log.LogInformation("Prerreserva mesa creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = BuildUri(detalle, detalle.CrearReservaEndpoint);
        if (string.IsNullOrWhiteSpace(uriReserva))
        {
            reservaResult.Error = "Endpoint de reserva no configurado";
            return;
        }

        var reserva = await MesasConnector.ConfirmarReservaAsync(
            uriReserva,
            idMesa,
            holdId,
            cliente.Nombre,
            cliente.Apellido,
            cliente.CorreoElectronico,
            cliente.TipoIdentificacion,
            cliente.DocumentoIdentidad,
            fecha,
            personas,
            forceSoap: !usandoRest);

        log.LogInformation("Reserva mesa confirmada: {IdReserva}", reserva.IdReserva);
        reservaResult.CodigoReserva = reserva.IdReserva;

        try
        {
            var uriFactura = BuildUri(detalle, detalle.GenerarFacturaEndpoint);
            if (!string.IsNullOrWhiteSpace(uriFactura))
            {
                var nombreFactura = ResolveNombreFactura(cliente, facturaInfo);
                var facturaUrl = await MesasConnector.GenerarFacturaAsync(
                    uriFactura,
                    reserva.IdReserva,
                    facturaInfo.CorreoFactura,
                    nombreFactura,
                    facturaInfo.TipoDocumento,
                    facturaInfo.Documento,
                    item.PrecioFinal,
                    forceSoap: !usandoRest);

                reservaResult.FacturaProveedorUrl = facturaUrl;
            }
            else
            {
                log.LogWarning("No se pudo generar factura mesa: endpoint no configurado");
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo generar factura mesa");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal, log);
        await RegistrarReservaEnDbAsync(item.ServicioId, reserva.IdReserva, reservaResult.FacturaProveedorUrl, compra, item.PrecioFinal);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoMesaAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente, ILogger log)
    {
        try
        {
            if (rest is not null)
            {
                try
                {
                    var uri = BuildUri(rest, rest.RegistrarClienteEndpoint);
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        await MesasConnector.CrearUsuarioAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, cliente.TipoIdentificacion, cliente.DocumentoIdentidad);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "REST fallo al crear cliente externo mesa");
                }
            }

            if (soap is not null)
            {
                var uri = BuildUri(soap, soap.RegistrarClienteEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    await MesasConnector.CrearUsuarioAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, cliente.TipoIdentificacion, cliente.DocumentoIdentidad, forceSoap: true);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo crear cliente externo mesa");
        }
    }

    private async Task ProcesarReservaPaqueteAsync(
        CheckoutItem item,
        Cliente cliente,
        FacturaInfo facturaInfo,
        Compra compra,
        ReservaResult reservaResult,
        ILogger log)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId, log);

        if (servicio is null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var fechaInicio = item.FechaInicio ?? DateTime.Today;
        var personas = item.NumeroPersonas ?? 1;
        var bookingUserId = cliente.Id.ToString();
        bool usandoRest = false;

        log.LogInformation("Procesando reserva paquete {IdPaquete} en {Servicio}", item.IdProducto, servicio.Nombre);

        await CrearClienteExternoPaqueteAsync(detalleRest, detalleSoap, cliente, bookingUserId, log);

        string holdId = string.Empty;
        Exception? ultimoError = null;

        if (detalleRest is not null)
        {
            try
            {
                var uri = BuildUri(detalleRest, detalleRest.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando hold paquete (REST): {Uri}", uri);
                    (holdId, _) = await PaquetesConnector.CrearHoldAsync(uri, item.IdProducto, bookingUserId, fechaInicio, personas, 300);
                    usandoRest = true;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "REST fallo para hold paquete");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap is not null)
        {
            try
            {
                var uri = BuildUri(detalleSoap, detalleSoap.CrearPrerreservaEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    log.LogInformation("Creando hold paquete (SOAP): {Uri}", uri);
                    (holdId, _) = await PaquetesConnector.CrearHoldAsync(uri, item.IdProducto, bookingUserId, fechaInicio, personas, 300, forceSoap: true);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "SOAP tambien fallo para hold paquete");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrWhiteSpace(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        log.LogInformation("Hold paquete creado ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        var turistas = new (string, string, DateTime?, string, string)[]
        {
            (cliente.Nombre, cliente.Apellido, null, cliente.TipoIdentificacion, cliente.DocumentoIdentidad)
        };

        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = BuildUri(detalle, detalle.CrearReservaEndpoint);
        if (string.IsNullOrWhiteSpace(uriReserva))
        {
            reservaResult.Error = "Endpoint de reserva no configurado";
            return;
        }

        var reserva = await PaquetesConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            bookingUserId,
            "TransferenciaBancaria",
            turistas,
            forceSoap: !usandoRest);

        log.LogInformation("Reserva paquete creada: {IdReserva}", reserva.IdReserva);
        reservaResult.CodigoReserva = reserva.CodigoReserva;

        try
        {
            var uriFactura = BuildUri(detalle, detalle.GenerarFacturaEndpoint);
            if (!string.IsNullOrWhiteSpace(uriFactura))
            {
                decimal subtotal = item.PrecioFinal;
                decimal iva = subtotal * 0.12m;
                decimal total = subtotal + iva;

                var facturaUrl = await PaquetesConnector.EmitirFacturaAsync(uriFactura, reserva.IdReserva, subtotal, iva, total, forceSoap: !usandoRest);
                reservaResult.FacturaProveedorUrl = facturaUrl;
            }
            else
            {
                log.LogWarning("No se pudo generar factura paquete: endpoint no configurado");
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo emitir factura paquete");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal, log);
        await RegistrarReservaEnDbAsync(item.ServicioId, reserva.CodigoReserva, reservaResult.FacturaProveedorUrl, compra, item.PrecioFinal);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoPaqueteAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente, string bookingUserId, ILogger log)
    {
        try
        {
            if (rest is not null)
            {
                try
                {
                    var uri = BuildUri(rest, rest.RegistrarClienteEndpoint);
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        await PaquetesConnector.CrearUsuarioExternoAsync(uri, bookingUserId, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "REST fallo al crear cliente externo paquete");
                }
            }

            if (soap is not null)
            {
                var uri = BuildUri(soap, soap.RegistrarClienteEndpoint);
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    await PaquetesConnector.CrearUsuarioExternoAsync(uri, bookingUserId, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, forceSoap: true);
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo crear cliente externo paquete");
        }
    }

    private async Task PagarProveedorAsync(Servicio servicio, decimal precioFinal, ILogger log)
    {
        if (!int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            log.LogWarning("Cuenta del proveedor no valida para servicio {ServicioId}", servicio.Id);
            return;
        }

        var montoProveedor = precioFinal * (1 - COMISION_TRAVELIO);
        var pagoExitoso = await TransferirClass.RealizarTransferenciaAsync(
            cuentaDestino: cuentaProveedor,
            monto: montoProveedor,
            cuentaOrigen: TransferirClass.cuentaDefaultTravelio);

        if (pagoExitoso)
        {
            log.LogInformation("Pago de ${Monto} realizado a {Servicio}", montoProveedor, servicio.Nombre);
        }
        else
        {
            log.LogWarning("No se pudo pagar a {Servicio}", servicio.Nombre);
        }
    }

    private async Task RegistrarReservaEnDbAsync(int servicioId, string codigoReserva, string? facturaUrl, Compra compra, decimal precioFinal)
    {
        var comision = precioFinal * COMISION_TRAVELIO;
        var valorNegocio = precioFinal - comision;

        var reservaDb = new DbReserva
        {
            ServicioId = servicioId,
            CodigoReserva = codigoReserva,
            FacturaUrl = facturaUrl,
            Activa = true,
            ValorPagadoNegocio = valorNegocio,
            ComisionAgencia = comision
        };

        _db.Reservas.Add(reservaDb);
        await _db.SaveChangesAsync();

        _db.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });
    }

    public async Task<CancelacionResult> CancelarReservaFrontendAsync(int reservaId, int clienteId, int cuentaBancariaCliente, ILogger? logger = null)
    {
        var resultado = new CancelacionResult();
        var log = ResolveLogger(logger);

        try
        {
            var reserva = await _db.Reservas
                .Include(r => r.Servicio)
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva is null)
            {
                resultado.Mensaje = "Reserva no encontrada.";
                return resultado;
            }

            var reservaCompra = await _db.ReservasCompra
                .Include(rc => rc.Compra)
                .FirstOrDefaultAsync(rc => rc.ReservaId == reservaId && rc.Compra.ClienteId == clienteId);

            if (reservaCompra is null)
            {
                resultado.Mensaje = "No tienes permiso para cancelar esta reserva.";
                return resultado;
            }

            var detalleRest = await _db.DetallesServicio
                .FirstOrDefaultAsync(d => d.ServicioId == reserva.ServicioId && d.TipoProtocolo == TipoProtocolo.Rest);

            if (detalleRest is null || string.IsNullOrWhiteSpace(detalleRest.CancelarReservaEndpoint))
            {
                resultado.Mensaje = "Este proveedor no soporta cancelaciones (solo disponible via REST).";
                return resultado;
            }

            var uriCancelar = BuildUri(detalleRest, detalleRest.CancelarReservaEndpoint);
            if (string.IsNullOrWhiteSpace(uriCancelar))
            {
                resultado.Mensaje = "No se pudo construir el endpoint de cancelacion.";
                return resultado;
            }

            bool exito = false;
            decimal valorReembolsado = 0m;

            log.LogInformation("Cancelando reserva {ReservaId} en {Servicio} via REST", reservaId, reserva.Servicio.Nombre);

            switch (reserva.Servicio.TipoServicio)
            {
                case TipoServicio.RentaVehiculos:
                    (exito, valorReembolsado) = await AutosConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    break;
                case TipoServicio.Hotel:
                    (exito, valorReembolsado) = await HabitacionesConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    break;
                case TipoServicio.Aerolinea:
                    (exito, valorReembolsado) = await AerolineaConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    break;
                case TipoServicio.Restaurante:
                    (exito, valorReembolsado) = await MesasConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    break;
                case TipoServicio.PaquetesTuristicos:
                    (exito, valorReembolsado) = await PaquetesConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    break;
                default:
                    resultado.Mensaje = "Tipo de servicio no soportado para cancelacion.";
                    return resultado;
            }

            if (exito)
            {
                if (valorReembolsado > 0)
                {
                    var reembolsoExitoso = await TransferirClass.RealizarTransferenciaAsync(
                        cuentaDestino: cuentaBancariaCliente,
                        monto: valorReembolsado,
                        cuentaOrigen: TransferirClass.cuentaDefaultTravelio);

                    if (reembolsoExitoso)
                    {
                        log.LogInformation("Reembolso de ${Monto} realizado al cliente", valorReembolsado);
                    }
                }

                reserva.Activa = false;
                await _db.SaveChangesAsync();

                resultado.Exitoso = true;
                resultado.MontoReembolsado = valorReembolsado;
                resultado.Mensaje = $"Reserva cancelada exitosamente. Se reembolsaron ${valorReembolsado:N2}";
            }
            else
            {
                resultado.Mensaje = "No se pudo cancelar la reserva en el proveedor.";
            }

            return resultado;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error cancelando reserva {ReservaId}", reservaId);
            resultado.Mensaje = "Error al cancelar la reserva.";
            return resultado;
        }
    }

    #endregion

    #region Pago y reservas

    public async Task<bool?> ProcesarCompraYReservasAsync(int clienteId, int cuentaCliente, FacturaInfo facturaInfo, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
            if (cliente is null)
            {
                log.LogError("Cliente {ClienteId} no encontrado", clienteId);
                return null;
            }

            var now = DateTime.UtcNow;

            var vuelos = await _db.CarritosAerolinea.Include(c => c.Pasajeros)
                .Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId && c.HoldId != null && (c.HoldExpira == null || c.HoldExpira > now))
                .ToListAsync();
            var habitaciones = await _db.CarritosHabitaciones.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId && c.HoldId != null)
                .ToListAsync();
            var autos = await _db.CarritosAutos.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId && c.HoldId != null && (c.HoldExpira == null || c.HoldExpira > now))
                .ToListAsync();
            var paquetes = await _db.CarritosPaquetes.Include(c => c.Turistas)
                .Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId && c.HoldId != null && (c.HoldExpira == null || c.HoldExpira > now))
                .ToListAsync();
            var mesas = await _db.CarritosMesas.Include(c => c.Servicio).ThenInclude(s => s.DetallesServicio)
                .Where(c => c.ClienteId == clienteId && c.HoldId != null && (c.HoldExpira == null || c.HoldExpira > now))
                .ToListAsync();

            var allItems = vuelos.Count + habitaciones.Count + autos.Count + paquetes.Count + mesas.Count;
            if (allItems == 0)
            {
                log.LogWarning("No hay items con hold para cliente {ClienteId}", clienteId);
                return false;
            }

            decimal total = vuelos.Sum(v => v.PrecioActual * v.CantidadPasajeros)
                             + habitaciones.Sum(h => h.PrecioVigente * Math.Max(1, (decimal)(h.FechaFin.Date - h.FechaInicio.Date).TotalDays))
                             + autos.Sum(a => a.PrecioActualPorDia * Math.Max(1, (decimal)(a.FechaFin.Date - a.FechaInicio.Date).TotalDays))
                             + paquetes.Sum(p => p.PrecioActual * p.Personas)
                             + mesas.Sum(m => m.Precio);

            log.LogDebug("Total a debitar de la cuenta {CuentaCliente}: {Total}", cuentaCliente, total);
            var debitoOk = await TransferirClass.RealizarTransferenciaAsync(TransferirClass.cuentaDefaultTravelio, total, cuentaCliente);
            if (!debitoOk)
            {
                log.LogError("No se pudo debitar el total {Total} de la cuenta del cliente {Cuenta}", total, cuentaCliente);
                return null;
            }

            var compra = new Compra
            {
                ClienteId = clienteId,
                FechaCompra = DateTime.UtcNow,
                ValorPagado = total
            };
            _db.Compras.Add(compra);
            await _db.SaveChangesAsync();

            var huboExito = false;

            async Task<bool> PagarNegocioAsync(Servicio servicio, decimal monto)
            {
                if (!int.TryParse(servicio.NumeroCuenta, out var cuentaDestino))
                {
                    log.LogError("Número de cuenta inválido para servicio {ServicioId}", servicio.Id);
                    return false;
                }

                return await TransferirClass.RealizarTransferenciaAsync(cuentaDestino, monto, TransferirClass.cuentaDefaultTravelio);
            }

            foreach (var item in vuelos)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var monto = item.PrecioActual * item.CantidadPasajeros;
                var montoNegocio = monto * 0.9m;
                var comision = monto - montoNegocio;

                if (!await PagarNegocioAsync(item.Servicio, montoNegocio))
                {
                    log.LogError("No se pudo transferir al servicio {ServicioId} por vuelo {CarritoId}", item.ServicioId, item.Id);
                    continue;
                }

                var pasajeros = item.Pasajeros.Select(p => (p.Nombre, p.Apellido, p.TipoIdentificacion, p.Identificacion, new DateTime(p.FechaNacimiento.Year, p.FechaNacimiento.Month, p.FechaNacimiento.Day))).ToArray();
                var uriReserva = BuildUri(det, det.CrearReservaEndpoint);
                try
                {
                    var reserva = await AerolineaConnector.CrearReservaAsync(
                        uriReserva!,
                        item.IdVueloProveedor,
                        item.HoldId ?? string.Empty,
                        cliente.CorreoElectronico,
                        pasajeros,
                        forceSoap);

                    var uriFactura = BuildUri(det, det.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await AerolineaConnector.GenerarFacturaAsync(
                            uriFactura!,
                            reserva.idReserva,
                            monto,
                            0m,
                            monto,
                            (facturaInfo.NombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                            string.Empty,
                            forceSoap);
                    }
                    catch (NotImplementedException) when (IsREST && alternativo is not null)
                    {
                        var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                        facturaUrl = await AerolineaConnector.GenerarFacturaAsync(
                            uriFacturaSoap!,
                            reserva.idReserva,
                            monto,
                            0m,
                            monto,
                            (facturaInfo.NombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                            string.Empty,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de vuelo {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reserva.codigoReserva,
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });

                    _db.CarritosAerolinea.Remove(item);
                    huboExito = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriReservaSoap = BuildUri(alternativo, alternativo.CrearReservaEndpoint);
                    var reserva = await AerolineaConnector.CrearReservaAsync(
                        uriReservaSoap!,
                        item.IdVueloProveedor,
                        item.HoldId ?? string.Empty,
                        cliente.CorreoElectronico,
                        pasajeros,
                        true);

                    var uriFactura = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await AerolineaConnector.GenerarFacturaAsync(
                            uriFactura!,
                            reserva.idReserva,
                            monto,
                            0m,
                            monto,
                            (facturaInfo.NombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                            string.Empty,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de vuelo (fallback SOAP) {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reserva.codigoReserva,
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });

                    _db.CarritosAerolinea.Remove(item);
                    huboExito = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear reserva de vuelo {CarritoId}, se devuelve monto", item.Id);
                    await TransferirClass.RealizarTransferenciaAsync(cuentaCliente, monto, TransferirClass.cuentaDefaultTravelio);
                    _db.CarritosAerolinea.Remove(item);
                }
            }

            foreach (var item in habitaciones)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var noches = Math.Max(1, (decimal)(item.FechaFin.Date - item.FechaInicio.Date).TotalDays);
                var monto = item.PrecioVigente * noches;
                var montoNegocio = monto * 0.9m;
                var comision = monto - montoNegocio;

                if (!await PagarNegocioAsync(item.Servicio, montoNegocio))
                {
                    log.LogError("No se pudo transferir al hotel {ServicioId} por habitación {CarritoId}", item.ServicioId, item.Id);
                    continue;
                }

                var uriReserva = BuildUri(det, det.CrearReservaEndpoint);
                try
                {
                    var reservaId = await TravelioAPIConnector.Habitaciones.Connector.CrearReservaAsync(
                        uriReserva!,
                        item.IdHabitacionProveedor,
                        item.HoldId ?? string.Empty,
                        cliente.Nombre,
                        cliente.Apellido,
                        cliente.CorreoElectronico,
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        item.FechaInicio,
                        item.FechaFin,
                        item.NumeroHuespedes,
                        forceSoap);

                    var uriFactura = BuildUri(det, det.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Habitaciones.Connector.EmitirFacturaAsync(
                            uriFactura!,
                            reservaId,
                        facturaInfo.NombreFactura,
                        facturaInfo.Documento,
                        facturaInfo.TipoDocumento,
                        facturaInfo.Documento,
                        facturaInfo.CorreoFactura,
                        forceSoap);
                    }
                    catch (NotImplementedException) when (IsREST && alternativo is not null)
                    {
                        var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                        facturaUrl = await TravelioAPIConnector.Habitaciones.Connector.EmitirFacturaAsync(
                            uriFacturaSoap!,
                            reservaId,
                            facturaInfo.NombreFactura,
                            facturaInfo.Documento,
                            facturaInfo.TipoDocumento,
                            facturaInfo.Documento,
                            facturaInfo.CorreoFactura,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de habitación {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reservaId.ToString(),
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosHabitaciones.Remove(item);
                    huboExito = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriReservaSoap = BuildUri(alternativo, alternativo.CrearReservaEndpoint);
                    var reservaId = await TravelioAPIConnector.Habitaciones.Connector.CrearReservaAsync(
                        uriReservaSoap!,
                        item.IdHabitacionProveedor,
                        item.HoldId ?? string.Empty,
                        cliente.Nombre,
                        cliente.Apellido,
                        cliente.CorreoElectronico,
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        item.FechaInicio,
                        item.FechaFin,
                        item.NumeroHuespedes,
                        true);

                    var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Habitaciones.Connector.EmitirFacturaAsync(
                            uriFacturaSoap!,
                            reservaId,
                            facturaInfo.NombreFactura,
                            facturaInfo.Documento,
                            facturaInfo.TipoDocumento,
                            facturaInfo.Documento,
                            facturaInfo.CorreoFactura,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de habitación (fallback SOAP) {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reservaId.ToString(),
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosHabitaciones.Remove(item);
                    huboExito = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear reserva de habitación {CarritoId}, se devuelve monto", item.Id);
                    await TransferirClass.RealizarTransferenciaAsync(cuentaCliente, monto, TransferirClass.cuentaDefaultTravelio);
                    _db.CarritosHabitaciones.Remove(item);
                }
            }

            foreach (var item in autos)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var dias = Math.Max(1, (decimal)(item.FechaFin.Date - item.FechaInicio.Date).TotalDays);
                var monto = item.PrecioActualPorDia * dias;
                var montoNegocio = monto * 0.9m;
                var comision = monto - montoNegocio;

                if (!await PagarNegocioAsync(item.Servicio, montoNegocio))
                {
                    log.LogError("No se pudo transferir al rent-a-car {ServicioId} por auto {CarritoId}", item.ServicioId, item.Id);
                    continue;
                }

                var uriReserva = BuildUri(det, det.CrearReservaEndpoint);
                try
                {
                    var reservaId = await TravelioAPIConnector.Autos.Connector.CrearReservaAsync(
                        uriReserva!,
                        item.IdAutoProveedor,
                        item.HoldId ?? string.Empty,
                        cliente.Nombre,
                        cliente.Apellido,
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        cliente.CorreoElectronico,
                        item.FechaInicio,
                        item.FechaFin,
                        forceSoap);

                    var uriFactura = BuildUri(det, det.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Autos.Connector.GenerarFacturaAsync(
                            uriFactura!,
                            reservaId,
                            monto,
                            0m,
                            monto,
                            (facturaInfo.NombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                            forceSoap);
                    }
                    catch (NotImplementedException) when (IsREST && alternativo is not null)
                    {
                        var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                        facturaUrl = await TravelioAPIConnector.Autos.Connector.GenerarFacturaAsync(
                            uriFacturaSoap!,
                            reservaId,
                            monto,
                            0m,
                            monto,
                            (facturaInfo.NombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de auto {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reservaId.ToString(),
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosAutos.Remove(item);
                    huboExito = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriReservaSoap = BuildUri(alternativo, alternativo.CrearReservaEndpoint);
                    var reservaId = await TravelioAPIConnector.Autos.Connector.CrearReservaAsync(
                        uriReservaSoap!,
                        item.IdAutoProveedor,
                        item.HoldId ?? string.Empty,
                        cliente.Nombre,
                        cliente.Apellido,
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        cliente.CorreoElectronico,
                        item.FechaInicio,
                        item.FechaFin,
                        true);

                    var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Autos.Connector.GenerarFacturaAsync(
                            uriFacturaSoap!,
                            reservaId,
                            monto,
                            0m,
                            monto,
                            (facturaInfo.NombreFactura, facturaInfo.TipoDocumento, facturaInfo.Documento, facturaInfo.CorreoFactura),
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de auto (fallback SOAP) {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reservaId.ToString(),
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosAutos.Remove(item);
                    huboExito = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear reserva de auto {CarritoId}, se devuelve monto", item.Id);
                    await TransferirClass.RealizarTransferenciaAsync(cuentaCliente, monto, TransferirClass.cuentaDefaultTravelio);
                    _db.CarritosAutos.Remove(item);
                }
            }

            foreach (var item in paquetes)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var monto = item.PrecioActual * item.Personas;
                var montoNegocio = monto * 0.9m;
                var comision = monto - montoNegocio;

                if (!await PagarNegocioAsync(item.Servicio, montoNegocio))
                {
                    log.LogError("No se pudo transferir al proveedor de paquetes {ServicioId} por paquete {CarritoId}", item.ServicioId, item.Id);
                    continue;
                }

                var turistas = item.Turistas.Select(t => (t.Nombre, t.Apellido, t.FechaNacimiento.HasValue ? new DateTime?(new DateTime(t.FechaNacimiento.Value.Year, t.FechaNacimiento.Value.Month, t.FechaNacimiento.Value.Day)) : null, t.TipoIdentificacion, t.Identificacion)).ToArray();
                var uriReserva = BuildUri(det, det.CrearReservaEndpoint);
                try
                {
                    var reserva = await TravelioAPIConnector.Paquetes.Connector.CrearReservaAsync(
                        uriReserva!,
                        item.IdPaqueteProveedor,
                        item.HoldId ?? string.Empty,
                        item.BookingUserId,
                        "TRANSFER",
                        turistas,
                        forceSoap);

                    var uriFactura = BuildUri(det, det.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Paquetes.Connector.EmitirFacturaAsync(
                            uriFactura!,
                            reserva.IdReserva,
                            monto,
                            0m,
                            monto,
                            forceSoap);
                    }
                    catch (NotImplementedException) when (IsREST && alternativo is not null)
                    {
                        var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                        facturaUrl = await TravelioAPIConnector.Paquetes.Connector.EmitirFacturaAsync(
                            uriFacturaSoap!,
                            reserva.IdReserva,
                            monto,
                            0m,
                            monto,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de paquete {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reserva.CodigoReserva,
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosPaquetes.Remove(item);
                    huboExito = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriReservaSoap = BuildUri(alternativo, alternativo.CrearReservaEndpoint);
                    var reserva = await TravelioAPIConnector.Paquetes.Connector.CrearReservaAsync(
                        uriReservaSoap!,
                        item.IdPaqueteProveedor,
                        item.HoldId ?? string.Empty,
                        item.BookingUserId,
                        "TRANSFER",
                        turistas,
                        true);

                    var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Paquetes.Connector.EmitirFacturaAsync(
                            uriFacturaSoap!,
                            reserva.IdReserva,
                            monto,
                            0m,
                            monto,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de paquete (fallback SOAP) {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reserva.CodigoReserva,
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosPaquetes.Remove(item);
                    huboExito = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear reserva de paquete {CarritoId}, se devuelve monto", item.Id);
                    await TransferirClass.RealizarTransferenciaAsync(cuentaCliente, monto, TransferirClass.cuentaDefaultTravelio);
                    _db.CarritosPaquetes.Remove(item);
                }
            }

            foreach (var item in mesas)
            {
                var (det, alternativo) = SeleccionarDetalle(item.Servicio.DetallesServicio, IsREST);
                if (det is null) continue;
                var forceSoap = IsREST && det.TipoProtocolo == TipoProtocolo.Soap;
                var monto = item.Precio;
                var montoNegocio = monto * 0.9m;
                var comision = monto - montoNegocio;

                if (!await PagarNegocioAsync(item.Servicio, montoNegocio))
                {
                    log.LogError("No se pudo transferir al restaurante {ServicioId} por mesa {CarritoId}", item.ServicioId, item.Id);
                    continue;
                }

                var uriReserva = BuildUri(det, det.CrearReservaEndpoint);
                try
                {
                    var reserva = await TravelioAPIConnector.Mesas.Connector.ConfirmarReservaAsync(
                        uriReserva!,
                        item.IdMesa,
                        item.HoldId ?? string.Empty,
                        cliente.Nombre,
                        cliente.Apellido,
                        cliente.CorreoElectronico,
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        item.FechaReserva,
                        item.NumeroPersonas,
                        forceSoap);

                    var uriFactura = BuildUri(det, det.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Mesas.Connector.GenerarFacturaAsync(
                            uriFactura!,
                            reserva.IdReserva,
                            facturaInfo.CorreoFactura,
                            facturaInfo.NombreFactura,
                            facturaInfo.TipoDocumento,
                            facturaInfo.Documento,
                            monto,
                            forceSoap);
                    }
                    catch (NotImplementedException) when (IsREST && alternativo is not null)
                    {
                        var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                        facturaUrl = await TravelioAPIConnector.Mesas.Connector.GenerarFacturaAsync(
                            uriFacturaSoap!,
                            reserva.IdReserva,
                            facturaInfo.CorreoFactura,
                            facturaInfo.NombreFactura,
                            facturaInfo.TipoDocumento,
                            facturaInfo.Documento,
                            monto,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de mesa {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reserva.IdReserva ?? string.Empty,
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosMesas.Remove(item);
                    huboExito = true;
                }
                catch (NotImplementedException) when (IsREST && alternativo is not null)
                {
                    var uriReservaSoap = BuildUri(alternativo, alternativo.CrearReservaEndpoint);
                    var reserva = await TravelioAPIConnector.Mesas.Connector.ConfirmarReservaAsync(
                        uriReservaSoap!,
                        item.IdMesa,
                        item.HoldId ?? string.Empty,
                        cliente.Nombre,
                        cliente.Apellido,
                        cliente.CorreoElectronico,
                        cliente.TipoIdentificacion,
                        cliente.DocumentoIdentidad,
                        item.FechaReserva,
                        item.NumeroPersonas,
                        true);

                    var uriFacturaSoap = BuildUri(alternativo, alternativo.GenerarFacturaEndpoint);
                    string? facturaUrl = null;
                    try
                    {
                        facturaUrl = await TravelioAPIConnector.Mesas.Connector.GenerarFacturaAsync(
                            uriFacturaSoap!,
                            reserva.IdReserva,
                            facturaInfo.CorreoFactura,
                            facturaInfo.NombreFactura,
                            facturaInfo.TipoDocumento,
                            facturaInfo.Documento,
                            monto,
                            true);
                    }
                    catch (Exception invoiceEx)
                    {
                        log.LogError(invoiceEx, "Error al generar factura de mesa (fallback SOAP) {CarritoId}", item.Id);
                    }

                    var reservaDb = new DbReserva
                    {
                        ServicioId = item.ServicioId,
                        CodigoReserva = reserva.IdReserva ?? string.Empty,
                        FacturaUrl = facturaUrl,
                        Activa = true,
                        ValorPagadoNegocio = montoNegocio,
                        ComisionAgencia = comision
                    };
                    _db.Reservas.Add(reservaDb);
                    await _db.SaveChangesAsync();
                    _db.ReservasCompra.Add(new ReservaCompra { CompraId = compra.Id, ReservaId = reservaDb.Id });
                    _db.CarritosMesas.Remove(item);
                    huboExito = true;
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error al crear reserva de mesa {CarritoId}, se devuelve monto", item.Id);
                    await TransferirClass.RealizarTransferenciaAsync(cuentaCliente, monto, TransferirClass.cuentaDefaultTravelio);
                    _db.CarritosMesas.Remove(item);
                }
            }

            await _db.SaveChangesAsync();
            return huboExito;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al procesar compra y reservas para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public bool? ProcesarCompraYReservas(int clienteId, int cuentaCliente, FacturaInfo facturaInfo, ILogger? logger = null) =>
        ProcesarCompraYReservasAsync(clienteId, cuentaCliente, facturaInfo, logger).GetAwaiter().GetResult();

    #endregion

    #region Cancelaciones y facturas

    public async Task<bool?> CancelarReservaAsync(int reservaId, int cuentaCliente, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var reserva = await _db.Reservas
                .Include(r => r.Servicio).ThenInclude(s => s.DetallesServicio)
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva is null)
            {
                log.LogWarning("Reserva {ReservaId} no encontrada para cancelaci▋n", reservaId);
                return null;
            }

            if (!reserva.Activa)
            {
                log.LogInformation("Reserva {ReservaId} ya estaba cancelada", reservaId);
                return false;
            }

            var detallesRest = FiltrarDetallesPorProtocolo(reserva.Servicio.DetallesServicio, preferRest: true);
            var detCancel = detallesRest.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest) ?? detallesRest.FirstOrDefault();
            if (detCancel is null || string.IsNullOrWhiteSpace(detCancel.CancelarReservaEndpoint))
            {
                log.LogError("No se encontr▋ detalle REST para cancelar la reserva {ReservaId}", reservaId);
                return false;
            }

            var uriCancel = BuildUri(detCancel, detCancel.CancelarReservaEndpoint ?? detCancel.ObtenerReservaEndpoint);

            try
            {
                switch (reserva.Servicio.TipoServicio)
                {
                    case TipoServicio.Aerolinea:
                        await AerolineaConnector.CancelarReservaAsync(uriCancel!, reserva.CodigoReserva);
                        break;
                    case TipoServicio.Hotel:
                        await HabitacionesConnector.CancelarReservaAsync(uriCancel!, reserva.CodigoReserva);
                        break;
                    case TipoServicio.RentaVehiculos:
                        await AutosConnector.CancelarReservaAsync(uriCancel!, reserva.CodigoReserva);
                        break;
                    case TipoServicio.PaquetesTuristicos:
                        await PaquetesConnector.CancelarReservaAsync(uriCancel!, reserva.CodigoReserva);
                        break;
                    case TipoServicio.Restaurante:
                        await MesasConnector.CancelarReservaAsync(uriCancel!, reserva.CodigoReserva);
                        break;
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error al invocar la cancelaci▋n remota para la reserva {ReservaId}", reservaId);
            }

            if (!int.TryParse(reserva.Servicio.NumeroCuenta, out var cuentaNegocio))
            {
                log.LogError("Cuenta del negocio inv▋lida para reserva {ReservaId}", reservaId);
                return false;
            }

            var totalReembolso = reserva.ValorPagadoNegocio + reserva.ComisionAgencia;

            var retiroNegocio = await TransferirClass.RealizarTransferenciaAsync(TransferirClass.cuentaDefaultTravelio, reserva.ValorPagadoNegocio, cuentaNegocio);
            if (!retiroNegocio)
            {
                log.LogError("No se pudo revertir el pago al negocio {ServicioId} para la reserva {ReservaId}", reserva.ServicioId, reservaId);
                return false;
            }

            var reembolsoCliente = await TransferirClass.RealizarTransferenciaAsync(cuentaCliente, totalReembolso, TransferirClass.cuentaDefaultTravelio);
            if (!reembolsoCliente)
            {
                log.LogError("No se pudo reembolsar al cliente por la reserva {ReservaId}", reservaId);
                return false;
            }

            reserva.Activa = false;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            log.LogError(ex, "Error al cancelar la reserva {ReservaId}", reservaId);
            return null;
        }
    }

    public bool? CancelarReserva(int reservaId, int cuentaCliente, ILogger? logger = null) =>
        CancelarReservaAsync(reservaId, cuentaCliente, logger).GetAwaiter().GetResult();

    public async Task<bool?> ReservaEstaCanceladaAsync(int reservaId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var reserva = await _db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId);
            if (reserva is null) return null;
            return !reserva.Activa;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al consultar estado de cancelaci▋n para reserva {ReservaId}", reservaId);
            return null;
        }
    }

    public bool? ReservaEstaCancelada(int reservaId, ILogger? logger = null) =>
        ReservaEstaCanceladaAsync(reservaId, logger).GetAwaiter().GetResult();

    public async Task<bool?> EstablecerFacturaCompraAsync(int compraId, string? facturaUrl, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var compra = await _db.Compras.FirstOrDefaultAsync(c => c.Id == compraId);
            if (compra is null) return null;

            compra.FacturaUrl = facturaUrl;
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al establecer factura de compra {CompraId}", compraId);
            return null;
        }
    }

    public bool? EstablecerFacturaCompra(int compraId, string? facturaUrl, ILogger? logger = null) =>
        EstablecerFacturaCompraAsync(compraId, facturaUrl, logger).GetAwaiter().GetResult();

    public async Task<string?> ObtenerFacturaCompraAsync(int compraId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var factura = await _db.Compras
                .Where(c => c.Id == compraId)
                .Select(c => c.FacturaUrl)
                .FirstOrDefaultAsync();
            return factura;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener factura de la compra {CompraId}", compraId);
            return null;
        }
    }

    public string? ObtenerFacturaCompra(int compraId, ILogger? logger = null) =>
        ObtenerFacturaCompraAsync(compraId, logger).GetAwaiter().GetResult();

    #endregion

    #region Consultas de reservas

    public async Task<int[]?> ObtenerReservasIdsPorClienteAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var ids = await _db.ReservasCompra
                .Include(rc => rc.Compra)
                .Where(rc => rc.Compra.ClienteId == clienteId)
                .Select(rc => rc.ReservaId)
                .Distinct()
                .ToArrayAsync();
            return ids;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener ids de reservas del cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public int[]? ObtenerReservasIdsPorCliente(int clienteId, ILogger? logger = null) =>
        ObtenerReservasIdsPorClienteAsync(clienteId, logger).GetAwaiter().GetResult();

    public async Task<DbReserva?> ObtenerReservaPorIdAsync(int reservaId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener reserva {ReservaId}", reservaId);
            return null;
        }
    }

    public DbReserva? ObtenerReservaPorId(int reservaId, ILogger? logger = null) =>
        ObtenerReservaPorIdAsync(reservaId, logger).GetAwaiter().GetResult();

    #endregion

    #region Consultas frontend

    public async Task<IReadOnlyList<Compra>?> ObtenerComprasPorClienteAsync(int clienteId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Compras
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener compras del cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public async Task<Compra?> ObtenerCompraConReservasAsync(int compraId, int? clienteId = null, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var query = _db.Compras
                .Include(c => c.Cliente)
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .Where(c => c.Id == compraId);

            if (clienteId.HasValue)
            {
                query = query.Where(c => c.ClienteId == clienteId.Value);
            }

            return await query.FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener compra {CompraId}", compraId);
            return null;
        }
    }

    public async Task<IReadOnlyList<Cliente>?> ObtenerClientesAsync(ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Clientes.ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener clientes");
            return null;
        }
    }

    public async Task<IReadOnlyList<Compra>?> ObtenerComprasAsync(ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Compras
                .Include(c => c.Cliente)
                .Include(c => c.ReservasCompra)
                    .ThenInclude(rc => rc.Reserva)
                        .ThenInclude(r => r.Servicio)
                .OrderByDescending(c => c.FechaCompra)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener compras");
            return null;
        }
    }

    public async Task<IReadOnlyList<DbReserva>?> ObtenerReservasAsync(ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Reservas
                .Include(r => r.Servicio)
                .Include(r => r.ReservasCompra)
                    .ThenInclude(rc => rc.Compra)
                        .ThenInclude(c => c.Cliente)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener reservas");
            return null;
        }
    }

    public async Task<IReadOnlyList<Servicio>?> ObtenerServiciosActivosAsync(ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            return await _db.Servicios
                .Include(s => s.DetallesServicio)
                .Where(s => s.Activo)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener servicios activos");
            return null;
        }
    }

    public async Task<(decimal hoy, decimal semana, decimal mes)?> ObtenerEstadisticasComprasAsync(ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var hoy = await _db.Compras
                .Where(c => c.FechaCompra.Date == DateTime.Today)
                .Select(c => c.ValorPagado)
                .DefaultIfEmpty(0m)
                .SumAsync();

            var semana = await _db.Compras
                .Where(c => c.FechaCompra >= DateTime.Today.AddDays(-7))
                .Select(c => c.ValorPagado)
                .DefaultIfEmpty(0m)
                .SumAsync();

            var mes = await _db.Compras
                .Where(c => c.FechaCompra >= DateTime.Today.AddMonths(-1))
                .Select(c => c.ValorPagado)
                .DefaultIfEmpty(0m)
                .SumAsync();

            return (hoy, semana, mes);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al obtener estad\u00edsticas de compras");
            return null;
        }
    }

    public async Task<bool?> MarcarReservaComoCanceladaAsync(int reservaId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var reserva = await _db.Reservas.FindAsync(reservaId);
            if (reserva is null) return false;

            reserva.Activa = false;
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al marcar reserva {ReservaId} como cancelada", reservaId);
            return null;
        }
    }

    public async Task<IReadOnlyList<ProveedorStatus>?> VerificarProveedoresAsync(int servicioId, ILogger? logger = null)
    {
        var log = ResolveLogger(logger);
        try
        {
            var detalles = await _db.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var resultados = new List<ProveedorStatus>();
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            foreach (var detalle in detalles)
            {
                var url = BuildUri(detalle, detalle.ObtenerProductosEndpoint);
                if (string.IsNullOrWhiteSpace(url))
                {
                    resultados.Add(new ProveedorStatus(detalle.TipoProtocolo.ToString(), string.Empty, false, "URL no configurada"));
                    continue;
                }

                try
                {
                    var response = await httpClient.GetAsync(url);
                    var disponible = response.IsSuccessStatusCode;
                    var mensaje = disponible ? "OK" : $"Error: {response.StatusCode}";
                    resultados.Add(new ProveedorStatus(detalle.TipoProtocolo.ToString(), url, disponible, mensaje));
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Error verificando proveedor {ServicioId} ({Protocolo})", servicioId, detalle.TipoProtocolo);
                    resultados.Add(new ProveedorStatus(detalle.TipoProtocolo.ToString(), url, false, $"Error: {ex.Message}"));
                }
            }

            return resultados;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al verificar proveedores del servicio {ServicioId}", servicioId);
            return null;
        }
    }

    #endregion
}
