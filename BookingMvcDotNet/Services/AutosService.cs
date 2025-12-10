using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioAPIConnector.Autos;

namespace BookingMvcDotNet.Services;

public class AutosService(TravelioDbContext dbContext, ILogger<AutosService> logger) : IAutosService
{
    private static string CompletarUrlImagen(string? uriImagen, string uriBase)
    {
        if (string.IsNullOrEmpty(uriImagen)) return "";

        if (uriImagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uriImagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return uriImagen;

        try
        {
            var baseUri = new Uri(uriBase);
            var dominioBase = $"{baseUri.Scheme}://{baseUri.Host}";
            if (baseUri.Port != 80 && baseUri.Port != 443)
                dominioBase += $":{baseUri.Port}";
            if (!uriImagen.StartsWith("/"))
                uriImagen = "/" + uriImagen;
            return dominioBase + uriImagen;
        }
        catch { return uriImagen; }
    }

    public async Task<AutosSearchViewModel> BuscarAutosAsync(AutosSearchViewModel filtros)
    {
        var resultado = new AutosSearchViewModel
        {
            Ciudad = filtros.Ciudad, Categoria = filtros.Categoria, Transmision = filtros.Transmision,
            Capacidad = filtros.Capacidad, PrecioMin = filtros.PrecioMin, PrecioMax = filtros.PrecioMax,
            FechaInicio = filtros.FechaInicio, FechaFin = filtros.FechaFin
        };

        try
        {
            var servicios = await dbContext.Servicios
                .Where(s => s.TipoServicio == TipoServicio.RentaVehiculos && s.Activo).ToListAsync();
            var servicioIds = servicios.Select(s => s.Id).ToList();
            var detalles = await dbContext.DetallesServicio
                .Where(d => servicioIds.Contains(d.ServicioId)).ToListAsync();

            var todosLosAutos = new List<AutoViewModel>();

            foreach (var servicio in servicios)
            {
                try
                {
                    var detalleRest = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Rest);
                    var detalleSoap = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap);

                    Vehiculo[] vehiculos = [];
                    string uriBaseUsada = "";
                    bool usandoRest = false;

                    if (detalleRest != null)
                    {
                        try
                        {
                            var uriRest = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                            logger.LogInformation("Consultando {Servicio} (REST): {Uri}", servicio.Nombre, uriRest);
                            vehiculos = await Connector.GetVehiculosAsync(uriRest, filtros.Categoria, filtros.Transmision, 
                                filtros.Capacidad, filtros.PrecioMin, filtros.PrecioMax, null, filtros.Ciudad, null);
                            uriBaseUsada = detalleRest.UriBase;
                            usandoRest = true;
                        }
                        catch (Exception ex) { logger.LogWarning(ex, "REST fallo para {Servicio}", servicio.Nombre); }
                    }

                    if (!usandoRest && detalleSoap != null)
                    {
                        var uriSoap = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                        logger.LogInformation("Consultando {Servicio} (SOAP): {Uri}", servicio.Nombre, uriSoap);
                        vehiculos = await Connector.GetVehiculosAsync(uriSoap, filtros.Categoria, filtros.Transmision,
                            filtros.Capacidad, filtros.PrecioMin, filtros.PrecioMax, null, filtros.Ciudad, null, forceSoap: true);
                        uriBaseUsada = detalleSoap.UriBase;
                    }

                    foreach (var v in vehiculos)
                    {
                        todosLosAutos.Add(new AutoViewModel
                        {
                            IdAuto = v.IdAuto, Tipo = v.Tipo, CapacidadPasajeros = v.CapacidadPasajeros,
                            PrecioNormalPorDia = v.PrecioNormalPorDia, PrecioActualPorDia = v.PrecioActualPorDia,
                            DescuentoPorcentaje = v.DescuentoPorcentaje, UriImagen = CompletarUrlImagen(v.UriImagen, uriBaseUsada),
                            Ciudad = v.Ciudad, Pais = v.Pais, ServicioId = servicio.Id, NombreProveedor = servicio.Nombre
                        });
                    }
                    logger.LogInformation("Encontrados {Count} vehiculos en {Servicio} ({Protocolo})", vehiculos.Length, servicio.Nombre, usandoRest ? "REST" : "SOAP");
                }
                catch (Exception ex) { logger.LogWarning(ex, "Error consultando {Servicio}", servicio.Nombre); }
            }
            resultado.Resultados = todosLosAutos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en busqueda de autos");
            resultado.ErrorMessage = "Error al buscar vehiculos. Intente nuevamente.";
        }
        return resultado;
    }

    public async Task<AutoDetalleViewModel?> ObtenerAutoAsync(int servicioId, string idAuto)
    {
        try
        {
            var servicio = await dbContext.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId);
            if (servicio == null) return null;

            var detalles = await dbContext.DetallesServicio.Where(d => d.ServicioId == servicioId).ToListAsync();
            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            Vehiculo[] vehiculos = [];
            string uriBaseUsada = "";

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                    vehiculos = await Connector.GetVehiculosAsync(uri);
                    uriBaseUsada = detalleRest.UriBase;
                }
                catch { }
            }

            if (vehiculos.Length == 0 && detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                vehiculos = await Connector.GetVehiculosAsync(uri, forceSoap: true);
                uriBaseUsada = detalleSoap.UriBase;
            }

            var vehiculo = vehiculos.FirstOrDefault(v => v.IdAuto == idAuto);
            if (string.IsNullOrEmpty(vehiculo.IdAuto)) return null;

            return new AutoDetalleViewModel
            {
                IdAuto = vehiculo.IdAuto, Tipo = vehiculo.Tipo, CapacidadPasajeros = vehiculo.CapacidadPasajeros,
                PrecioNormalPorDia = vehiculo.PrecioNormalPorDia, PrecioActualPorDia = vehiculo.PrecioActualPorDia,
                DescuentoPorcentaje = vehiculo.DescuentoPorcentaje, UriImagen = CompletarUrlImagen(vehiculo.UriImagen, uriBaseUsada),
                Ciudad = vehiculo.Ciudad, Pais = vehiculo.Pais, ServicioId = servicioId, NombreProveedor = servicio.Nombre
            };
        }
        catch (Exception ex) { logger.LogError(ex, "Error obteniendo auto {IdAuto}", idAuto); return null; }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var detalles = await dbContext.DetallesServicio.Where(d => d.ServicioId == servicioId).ToListAsync();
            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ConfirmarProductoEndpoint}";
                    return await Connector.VerificarDisponibilidadAutoAsync(uri, idAuto, fechaInicio, fechaFin);
                }
                catch { }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ConfirmarProductoEndpoint}";
                return await Connector.VerificarDisponibilidadAutoAsync(uri, idAuto, fechaInicio, fechaFin, forceSoap: true);
            }
            return false;
        }
        catch (Exception ex) { logger.LogError(ex, "Error verificando disponibilidad"); return false; }
    }

    public async Task<(bool exito, string mensaje)> CrearPrerreservaAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var detalles = await dbContext.DetallesServicio.Where(d => d.ServicioId == servicioId).ToListAsync();
            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                    var (holdId, expiracion) = await Connector.CrearPrerreservaAsync(uri, idAuto, fechaInicio, fechaFin);
                    return (true, $"Prerreserva creada (REST): {holdId}");
                }
                catch { }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                var (holdId, expiracion) = await Connector.CrearPrerreservaAsync(uri, idAuto, fechaInicio, fechaFin, forceSoap: true);
                return (true, $"Prerreserva creada (SOAP): {holdId}");
            }
            return (false, "Servicio no encontrado");
        }
        catch (Exception ex) { logger.LogError(ex, "Error creando prerreserva"); return (false, "Error al crear prerreserva"); }
    }
}
