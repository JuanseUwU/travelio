using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioAPIConnector.Habitaciones;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementación del servicio de hoteles que usa TravelioAPIConnector para SOAP/REST.
/// </summary>
public class HotelesService(TravelioDbContext dbContext, ILogger<HotelesService> logger) : IHotelesService
{
    /// <summary>
    /// Completa una URL de imagen relativa con la URL base del proveedor.
    /// </summary>
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
            // Obtener todos los servicios de hoteles activos
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
                    var detalle = detalles
                        .Where(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap)
                        .FirstOrDefault();

                    if (detalle == null) continue;

                    var uri = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";

                    logger.LogInformation("Consultando {Servicio} (SOAP): {Uri}", servicio.Nombre, uri);

                    var habitaciones = await Connector.BuscarHabitacionesAsync(
                        uri,
                        filtros.FechaInicio,
                        filtros.FechaFin,
                        filtros.TipoHabitacion,
                        filtros.Capacidad,
                        filtros.PrecioMin,
                        filtros.PrecioMax
                    );

                    foreach (var h in habitaciones)
                    {
                        // Filtrar por ciudad si se especificó
                        if (!string.IsNullOrEmpty(filtros.Ciudad) && 
                            !h.Ciudad.Contains(filtros.Ciudad, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Procesar imágenes: filtrar vacías y completar URLs
                        var imagenesCompletas = h.Imagenes
                            .Where(img => !string.IsNullOrWhiteSpace(img))
                            .Select(img => CompletarUrlImagen(img.Trim(), detalle.UriBase))
                            .Where(img => !string.IsNullOrEmpty(img))
                            .ToArray();

                        logger.LogInformation("Habitación {Id} de {Hotel}: {NumImagenes} imágenes, Primera='{Imagen}'",
                            h.IdHabitacion, h.Hotel, imagenesCompletas.Length, imagenesCompletas.FirstOrDefault() ?? "ninguna");

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

                    logger.LogInformation("Encontradas {Count} habitaciones en {Servicio}",
                        habitaciones.Length, servicio.Nombre);
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
            logger.LogError(ex, "Error en búsqueda de habitaciones");
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

            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return null;

            var uri = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";

            var habitaciones = await Connector.BuscarHabitacionesAsync(uri);
            var habitacion = habitaciones.FirstOrDefault(h => h.IdHabitacion == idHabitacion);

            if (string.IsNullOrEmpty(habitacion.IdHabitacion)) return null;

            var imagenesCompletas = habitacion.Imagenes
                .Where(img => !string.IsNullOrWhiteSpace(img))
                .Select(img => CompletarUrlImagen(img.Trim(), detalle.UriBase))
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
            logger.LogError(ex, "Error obteniendo habitación {IdHabitacion}", idHabitacion);
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, string idHabitacion, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return false;

            var uri = $"{detalle.UriBase}{detalle.ConfirmarProductoEndpoint}";

            return await Connector.ValidarDisponibilidadAsync(uri, idHabitacion, fechaInicio, fechaFin);
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
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null)
                return (false, "Servicio no encontrado", null);

            var uri = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";

            var holdId = await Connector.CrearPrerreservaAsync(
                uri, 
                idHabitacion, 
                fechaInicio, 
                fechaFin, 
                numeroHuespedes,
                300, // 5 minutos de hold
                precioActual
            );

            return (true, $"Prerreserva creada: {holdId}", holdId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creando prerreserva");
            return (false, "Error al crear prerreserva", null);
        }
    }
}
