using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioAPIConnector.Habitaciones;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementacion del servicio de hoteles que usa TravelioAPIConnector para REST/SOAP.
/// Prioriza REST y usa SOAP como fallback. Cancelacion solo disponible en REST.
/// </summary>
public class HotelesService(TravelioDbContext dbContext, ILogger<HotelesService> logger) : IHotelesService
{
    private static string CompletarUrlImagen(string? uriImagen, string uriBase)
    {
        if (string.IsNullOrEmpty(uriImagen))
            return "";

        if (uriImagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uriImagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return uriImagen;
        }

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
        catch
        {
            return uriImagen;
        }
    }

    public async Task<HabitacionesSearchViewModel> BuscarHabitacionesAsync(HabitacionesSearchViewModel filtros)
    {
        var resultado = new HabitacionesSearchViewModel
        {
            Ciudad = filtros.Ciudad,
            TipoHabitacion = filtros.TipoHabitacion,
            Capacidad = filtros.Capacidad,
            PrecioMin = filtros.PrecioMin,
            PrecioMax = filtros.PrecioMax,
            FechaInicio = filtros.FechaInicio,
            FechaFin = filtros.FechaFin,
            NumeroHuespedes = filtros.NumeroHuespedes
        };

        try
        {
            var servicios = await dbContext.Servicios
                .Where(s => s.TipoServicio == TipoServicio.Hotel && s.Activo)
                .ToListAsync();

            var servicioIds = servicios.Select(s => s.Id).ToList();
            var detalles = await dbContext.DetallesServicio
                .Where(d => servicioIds.Contains(d.ServicioId))
                .ToListAsync();

            var todasLasHabitaciones = new List<HabitacionViewModel>();

            foreach (var servicio in servicios)
            {
                try
                {
                    var detalleRest = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Rest);
                    var detalleSoap = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap);

                    Habitacion[] habitaciones = [];
                    string uriBaseUsada = "";
                    bool usandoRest = false;

                    // Intentar REST primero
                    if (detalleRest != null)
                    {
                        try
                        {
                            var uriRest = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                            logger.LogInformation("Consultando {Servicio} (REST): {Uri}", servicio.Nombre, uriRest);

                            habitaciones = await Connector.BuscarHabitacionesAsync(
                                uriRest, filtros.FechaInicio, filtros.FechaFin, filtros.TipoHabitacion,
                                filtros.Capacidad, filtros.PrecioMin, filtros.PrecioMax);
                            uriBaseUsada = detalleRest.UriBase;
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

                        habitaciones = await Connector.BuscarHabitacionesAsync(
                            uriSoap, filtros.FechaInicio, filtros.FechaFin, filtros.TipoHabitacion,
                            filtros.Capacidad, filtros.PrecioMin, filtros.PrecioMax, forceSoap: true);
                        uriBaseUsada = detalleSoap.UriBase;
                    }

                    foreach (var h in habitaciones)
                    {
                        if (!string.IsNullOrEmpty(filtros.Ciudad) && 
                            !h.Ciudad.Contains(filtros.Ciudad, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var imagenesCompletas = h.Imagenes
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Select(img => CompletarUrlImagen(img.Trim(), uriBaseUsada))
                            .Where(img => !string.IsNullOrEmpty(img))
                            .ToArray();

                        todasLasHabitaciones.Add(new HabitacionViewModel
                        {
                            IdHabitacion = h.IdHabitacion,
                            NombreHabitacion = h.NombreHabitacion,
                            TipoHabitacion = h.TipoHabitacion,
                            Hotel = h.Hotel,
                            Ciudad = h.Ciudad,
                            Pais = h.Pais,
                            Capacidad = h.Capacidad,
                            PrecioNormal = h.PrecioNormal,
                            PrecioActual = h.PrecioActual,
                            Amenidades = h.Amenidades,
                            Imagenes = imagenesCompletas,
                            ServicioId = servicio.Id,
                            NombreProveedor = servicio.Nombre
                        });
                    }

                    logger.LogInformation("Encontradas {Count} habitaciones en {Servicio} ({Protocolo})",
                        habitaciones.Length, servicio.Nombre, usandoRest ? "REST" : "SOAP");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error consultando {Servicio}", servicio.Nombre);
                }
            }

            resultado.Resultados = todasLasHabitaciones;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en busqueda de habitaciones");
            resultado.ErrorMessage = "Error al buscar habitaciones. Intente nuevamente.";
        }

        return resultado;
    }

    public async Task<HabitacionDetalleViewModel?> ObtenerHabitacionAsync(int servicioId, string idHabitacion)
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

            Habitacion[] habitaciones = [];
            string uriBaseUsada = "";

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                    habitaciones = await Connector.BuscarHabitacionesAsync(uri);
                    uriBaseUsada = detalleRest.UriBase;
                }
                catch { /* Fallback a SOAP */ }
            }

            if (habitaciones.Length == 0 && detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                habitaciones = await Connector.BuscarHabitacionesAsync(uri, forceSoap: true);
                uriBaseUsada = detalleSoap.UriBase;
            }

            var habitacion = habitaciones.FirstOrDefault(h => h.IdHabitacion == idHabitacion);
            if (string.IsNullOrEmpty(habitacion.IdHabitacion)) return null;

            var imagenesCompletas = habitacion.Imagenes
                .Where(img => !string.IsNullOrWhiteSpace(img))
                .Select(img => CompletarUrlImagen(img.Trim(), uriBaseUsada))
                .Where(img => !string.IsNullOrEmpty(img))
                .ToArray();

            return new HabitacionDetalleViewModel
            {
                IdHabitacion = habitacion.IdHabitacion,
                NombreHabitacion = habitacion.NombreHabitacion,
                TipoHabitacion = habitacion.TipoHabitacion,
                Hotel = habitacion.Hotel,
                Ciudad = habitacion.Ciudad,
                Pais = habitacion.Pais,
                Capacidad = habitacion.Capacidad,
                PrecioNormal = habitacion.PrecioNormal,
                PrecioActual = habitacion.PrecioActual,
                Amenidades = habitacion.Amenidades,
                Imagenes = imagenesCompletas,
                ServicioId = servicioId,
                NombreProveedor = servicio.Nombre
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo habitacion {IdHabitacion}", idHabitacion);
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, string idHabitacion, DateTime fechaInicio, DateTime fechaFin)
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
                    return await Connector.ValidarDisponibilidadAsync(uri, idHabitacion, fechaInicio, fechaFin);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ConfirmarProductoEndpoint}";
                return await Connector.ValidarDisponibilidadAsync(uri, idHabitacion, fechaInicio, fechaFin, forceSoap: true);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad");
            return false;
        }
    }

    public async Task<(bool exito, string mensaje, string? holdId)> CrearPrerreservaAsync(
        int servicioId, 
        string idHabitacion, 
        DateTime fechaInicio, 
        DateTime fechaFin,
        int numeroHuespedes,
        decimal precioActual)
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
                    var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                    var holdId = await Connector.CrearPrerreservaAsync(uri, idHabitacion, fechaInicio, fechaFin, numeroHuespedes, 300, precioActual);
                    return (true, $"Prerreserva creada (REST): {holdId}", holdId);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                var holdId = await Connector.CrearPrerreservaAsync(uri, idHabitacion, fechaInicio, fechaFin, numeroHuespedes, 300, precioActual, forceSoap: true);
                return (true, $"Prerreserva creada (SOAP): {holdId}", holdId);
            }

            return (false, "Servicio no encontrado", null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creando prerreserva");
            return (false, "Error al crear prerreserva", null);
        }
    }
}
