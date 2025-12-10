using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using MesaConnector = TravelioAPIConnector.Mesas.Connector;
using TravelioAPIConnector.Mesas;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementacion del servicio de restaurantes que usa TravelioAPIConnector para REST/SOAP.
/// Prioriza REST y usa SOAP como fallback. Cancelacion solo disponible en REST.
/// </summary>
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
                    var detalleRest = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Rest);
                    var detalleSoap = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap);

                    Mesa[] mesas = [];
                    bool usandoRest = false;

                    // Intentar REST primero
                    if (detalleRest != null)
                    {
                        try
                        {
                            var uriRest = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                            logger.LogInformation("Consultando {Servicio} (REST): {Uri}", servicio.Nombre, uriRest);

                            mesas = await MesaConnector.BuscarMesasAsync(uriRest, filtros.Capacidad, filtros.TipoMesa, "Disponible");
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

                        mesas = await MesaConnector.BuscarMesasAsync(uriSoap, filtros.Capacidad, filtros.TipoMesa, "Disponible", forceSoap: true);
                    }

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

                    logger.LogInformation("Encontradas {Count} mesas en {Servicio} ({Protocolo})",
                        mesas.Length, servicio.Nombre, usandoRest ? "REST" : "SOAP");
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
            logger.LogError(ex, "Error en busqueda de mesas");
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

            var detalles = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            Mesa[] mesas = [];

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                    mesas = await MesaConnector.BuscarMesasAsync(uri);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (mesas.Length == 0 && detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                mesas = await MesaConnector.BuscarMesasAsync(uri, forceSoap: true);
            }

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
                    return await MesaConnector.ValidarDisponibilidadAsync(uri, idMesa, fecha, personas);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ConfirmarProductoEndpoint}";
                return await MesaConnector.ValidarDisponibilidadAsync(uri, idMesa, fecha, personas, forceSoap: true);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de mesa");
            return false;
        }
    }
}
