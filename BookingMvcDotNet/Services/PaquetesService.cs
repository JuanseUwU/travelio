using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using PaqueteConnector = TravelioAPIConnector.Paquetes.Connector;
using TravelioAPIConnector.Paquetes;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementacion del servicio de paquetes turisticos que usa TravelioAPIConnector para REST/SOAP.
/// Prioriza REST y usa SOAP como fallback. Cancelacion solo disponible en REST.
/// </summary>
public class PaquetesService(TravelioDbContext dbContext, ILogger<PaquetesService> logger) : IPaquetesService
{
    public async Task<PaquetesSearchViewModel> BuscarPaquetesAsync(PaquetesSearchViewModel filtros)
    {
        var resultado = new PaquetesSearchViewModel
        {
            Ciudad = filtros.Ciudad,
            FechaInicio = filtros.FechaInicio,
            TipoActividad = filtros.TipoActividad,
            PrecioMax = filtros.PrecioMax,
            Personas = filtros.Personas
        };

        try
        {
            var servicios = await dbContext.Servicios
                .Where(s => s.TipoServicio == TipoServicio.PaquetesTuristicos && s.Activo)
                .ToListAsync();

            var servicioIds = servicios.Select(s => s.Id).ToList();
            var detalles = await dbContext.DetallesServicio
                .Where(d => servicioIds.Contains(d.ServicioId))
                .ToListAsync();

            var todosLosPaquetes = new List<PaqueteViewModel>();

            foreach (var servicio in servicios)
            {
                try
                {
                    var detalleRest = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Rest);
                    var detalleSoap = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap);

                    Paquete[] paquetes = [];
                    bool usandoRest = false;

                    // Intentar REST primero
                    if (detalleRest != null)
                    {
                        try
                        {
                            var uriRest = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                            logger.LogInformation("Consultando {Servicio} (REST): {Uri}", servicio.Nombre, uriRest);

                            paquetes = await PaqueteConnector.BuscarPaquetesAsync(
                                uriRest, filtros.Ciudad, filtros.FechaInicio, filtros.TipoActividad, filtros.PrecioMax);
                            usandoRest = true;
                        }
                        catch (Exception exRest)
                        {
                            logger.LogWarning(exRest, "REST fallo para {Servicio}, intentando SOAP", servicio.Nombre);
                        }
                    }

                    // Fallback a SOAP si REST fallo o no existe
                    if (!usandoRest && detalleSoap != null)
                    {
                        var uriSoap = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                        logger.LogInformation("Consultando {Servicio} (SOAP): {Uri}", servicio.Nombre, uriSoap);

                        paquetes = await PaqueteConnector.BuscarPaquetesAsync(
                            uriSoap, filtros.Ciudad, filtros.FechaInicio, filtros.TipoActividad, filtros.PrecioMax, forceSoap: true);
                    }

                    foreach (var p in paquetes)
                    {
                        todosLosPaquetes.Add(new PaqueteViewModel
                        {
                            IdPaquete = p.IdPaquete,
                            Nombre = p.Nombre,
                            Ciudad = p.Ciudad,
                            Pais = p.Pais,
                            TipoActividad = p.TipoActividad,
                            Capacidad = p.Capacidad,
                            PrecioNormal = p.PrecioNormal,
                            PrecioActual = p.PrecioActual,
                            ImagenUrl = p.ImagenUrl,
                            Duracion = p.Duracion,
                            ServicioId = servicio.Id,
                            NombreProveedor = servicio.Nombre
                        });
                    }

                    logger.LogInformation("Encontrados {Count} paquetes en {Servicio} ({Protocolo})",
                        paquetes.Length, servicio.Nombre, usandoRest ? "REST" : "SOAP");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error consultando {Servicio}", servicio.Nombre);
                }
            }

            resultado.Resultados = todosLosPaquetes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en busqueda de paquetes");
            resultado.ErrorMessage = "Error al buscar paquetes. Intente nuevamente.";
        }

        return resultado;
    }

    public async Task<PaqueteDetalleViewModel?> ObtenerPaqueteAsync(int servicioId, string idPaquete)
    {
        try
        {
            var servicio = await dbContext.Servicios.FirstOrDefaultAsync(s => s.Id == servicioId);
            if (servicio == null) return null;

            var detalles = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            Paquete[] paquetes = [];

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                    paquetes = await PaqueteConnector.BuscarPaquetesAsync(uri);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (paquetes.Length == 0 && detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                paquetes = await PaqueteConnector.BuscarPaquetesAsync(uri, forceSoap: true);
            }

            var paquete = paquetes.FirstOrDefault(p => p.IdPaquete == idPaquete);
            if (string.IsNullOrEmpty(paquete.IdPaquete)) return null;

            return new PaqueteDetalleViewModel
            {
                IdPaquete = paquete.IdPaquete,
                Nombre = paquete.Nombre,
                Ciudad = paquete.Ciudad,
                Pais = paquete.Pais,
                TipoActividad = paquete.TipoActividad,
                Capacidad = paquete.Capacidad,
                PrecioNormal = paquete.PrecioNormal,
                PrecioActual = paquete.PrecioActual,
                ImagenUrl = paquete.ImagenUrl,
                Duracion = paquete.Duracion,
                ServicioId = servicioId,
                NombreProveedor = servicio.Nombre
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo paquete {IdPaquete}", idPaquete);
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, string idPaquete, DateTime fechaInicio, int personas)
    {
        try
        {
            var detalles = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ConfirmarProductoEndpoint}";
                    return await PaqueteConnector.ValidarDisponibilidadAsync(uri, idPaquete, fechaInicio, personas);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ConfirmarProductoEndpoint}";
                return await PaqueteConnector.ValidarDisponibilidadAsync(uri, idPaquete, fechaInicio, personas, forceSoap: true);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de paquete");
            return false;
        }
    }
}
