using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using PaqueteConnector = TravelioAPIConnector.Paquetes.Connector;

namespace BookingMvcDotNet.Services;

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
                    var detalle = detalles
                        .Where(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap)
                        .FirstOrDefault();

                    if (detalle == null) continue;

                    var uri = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";

                    logger.LogInformation("Consultando {Servicio} (SOAP): {Uri}", servicio.Nombre, uri);

                    var paquetes = await PaqueteConnector.BuscarPaquetesAsync(
                        uri,
                        filtros.Ciudad,
                        filtros.FechaInicio,
                        filtros.TipoActividad,
                        filtros.PrecioMax
                    );

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

                    logger.LogInformation("Encontrados {Count} paquetes en {Servicio}", paquetes.Length, servicio.Nombre);
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
            logger.LogError(ex, "Error en búsqueda de paquetes");
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

            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return null;

            var uri = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";
            var paquetes = await PaqueteConnector.BuscarPaquetesAsync(uri);
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
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return false;

            var uri = $"{detalle.UriBase}{detalle.ConfirmarProductoEndpoint}";
            return await PaqueteConnector.ValidarDisponibilidadAsync(uri, idPaquete, fechaInicio, personas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de paquete");
            return false;
        }
    }
}
