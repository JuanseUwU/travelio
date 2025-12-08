using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using MesaConnector = TravelioAPIConnector.Mesas.Connector;

namespace BookingMvcDotNet.Services;

public class RestaurantesService(TravelioDbContext dbContext, ILogger<RestaurantesService> logger) : IRestaurantesService
{
    public async Task<MesasSearchViewModel> BuscarMesasAsync(MesasSearchViewModel filtros)
    {
        var resultado = new MesasSearchViewModel
        {
            Capacidad = filtros.Capacidad,
            TipoMesa = filtros.TipoMesa,
            Fecha = filtros.Fecha,
            NumeroPersonas = filtros.NumeroPersonas
        };

        try
        {
            var servicios = await dbContext.Servicios
                .Where(s => s.TipoServicio == TipoServicio.Restaurante && s.Activo)
                .ToListAsync();

            var servicioIds = servicios.Select(s => s.Id).ToList();
            var detalles = await dbContext.DetallesServicio
                .Where(d => servicioIds.Contains(d.ServicioId))
                .ToListAsync();

            var todasLasMesas = new List<MesaViewModel>();

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

                    var mesas = await MesaConnector.BuscarMesasAsync(
                        uri,
                        filtros.Capacidad,
                        filtros.TipoMesa,
                        "Disponible"
                    );

                    foreach (var m in mesas)
                    {
                        todasLasMesas.Add(new MesaViewModel
                        {
                            IdMesa = m.IdMesa,
                            IdRestaurante = m.IdRestaurante,
                            NumeroMesa = m.NumeroMesa,
                            TipoMesa = m.TipoMesa,
                            Capacidad = m.Capacidad,
                            Precio = m.Precio,
                            ImagenUrl = m.ImagenUrl,
                            Estado = m.Estado,
                            ServicioId = servicio.Id,
                            NombreProveedor = servicio.Nombre
                        });
                    }

                    logger.LogInformation("Encontradas {Count} mesas en {Servicio}", mesas.Length, servicio.Nombre);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error consultando {Servicio}", servicio.Nombre);
                }
            }

            resultado.Resultados = todasLasMesas;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en búsqueda de mesas");
            resultado.ErrorMessage = "Error al buscar mesas. Intente nuevamente.";
        }

        return resultado;
    }

    public async Task<MesaDetalleViewModel?> ObtenerMesaAsync(int servicioId, int idMesa)
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
            var mesas = await MesaConnector.BuscarMesasAsync(uri);
            var mesa = mesas.FirstOrDefault(m => m.IdMesa == idMesa);

            if (mesa.IdMesa == 0) return null;

            return new MesaDetalleViewModel
            {
                IdMesa = mesa.IdMesa,
                IdRestaurante = mesa.IdRestaurante,
                NumeroMesa = mesa.NumeroMesa,
                TipoMesa = mesa.TipoMesa,
                Capacidad = mesa.Capacidad,
                Precio = mesa.Precio,
                ImagenUrl = mesa.ImagenUrl,
                Estado = mesa.Estado,
                ServicioId = servicioId,
                NombreProveedor = servicio.Nombre
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo mesa {IdMesa}", idMesa);
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, int idMesa, DateTime fecha, int personas)
    {
        try
        {
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return false;

            var uri = $"{detalle.UriBase}{detalle.ConfirmarProductoEndpoint}";
            return await MesaConnector.ValidarDisponibilidadAsync(uri, idMesa, fecha, personas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de mesa");
            return false;
        }
    }
}
