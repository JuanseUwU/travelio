using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioAPIConnector.Aerolinea;

namespace BookingMvcDotNet.Services;

public class VuelosService(TravelioDbContext dbContext, ILogger<VuelosService> logger) : IVuelosService
{
    public async Task<VuelosSearchViewModel> BuscarVuelosAsync(VuelosSearchViewModel filtros)
    {
        var resultado = new VuelosSearchViewModel
        {
            Origen = filtros.Origen,
            Destino = filtros.Destino,
            FechaSalida = filtros.FechaSalida,
            TipoCabina = filtros.TipoCabina,
            Pasajeros = filtros.Pasajeros,
            PrecioMin = filtros.PrecioMin,
            PrecioMax = filtros.PrecioMax
        };

        try
        {
            var servicios = await dbContext.Servicios
                .Where(s => s.TipoServicio == TipoServicio.Aerolinea && s.Activo)
                .ToListAsync();

            var servicioIds = servicios.Select(s => s.Id).ToList();
            var detalles = await dbContext.DetallesServicio
                .Where(d => servicioIds.Contains(d.ServicioId))
                .ToListAsync();

            var todosLosVuelos = new List<VueloViewModel>();

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

                    var vuelos = await Connector.GetVuelosAsync(
                        uri,
                        filtros.Origen,
                        filtros.Destino,
                        filtros.FechaSalida,
                        null,
                        filtros.TipoCabina,
                        filtros.Pasajeros,
                        filtros.PrecioMin,
                        filtros.PrecioMax
                    );

                    foreach (var v in vuelos)
                    {
                        todosLosVuelos.Add(new VueloViewModel
                        {
                            IdVuelo = v.IdVuelo,
                            Origen = v.Origen,
                            Destino = v.Destino,
                            Fecha = v.Fecha,
                            TipoCabina = v.TipoCabina,
                            NombreAerolinea = v.NombreAerolinea,
                            CapacidadPasajeros = v.CapacidadPasajeros,
                            AsientosDisponibles = v.CapacidadActual,
                            PrecioNormal = v.PrecioNormal,
                            PrecioActual = v.PrecioActual,
                            ServicioId = servicio.Id,
                            NombreProveedor = servicio.Nombre
                        });
                    }

                    logger.LogInformation("Encontrados {Count} vuelos en {Servicio}", vuelos.Length, servicio.Nombre);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error consultando {Servicio}", servicio.Nombre);
                }
            }

            resultado.Resultados = todosLosVuelos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en búsqueda de vuelos");
            resultado.ErrorMessage = "Error al buscar vuelos. Intente nuevamente.";
        }

        return resultado;
    }

    public async Task<VueloDetalleViewModel?> ObtenerVueloAsync(int servicioId, string idVuelo)
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

            var vuelos = await Connector.GetVuelosAsync(uri);
            var vuelo = vuelos.FirstOrDefault(v => v.IdVuelo == idVuelo);

            if (string.IsNullOrEmpty(vuelo.IdVuelo)) return null;

            return new VueloDetalleViewModel
            {
                IdVuelo = vuelo.IdVuelo,
                Origen = vuelo.Origen,
                Destino = vuelo.Destino,
                Fecha = vuelo.Fecha,
                TipoCabina = vuelo.TipoCabina,
                NombreAerolinea = vuelo.NombreAerolinea,
                CapacidadPasajeros = vuelo.CapacidadPasajeros,
                AsientosDisponibles = vuelo.CapacidadActual,
                PrecioNormal = vuelo.PrecioNormal,
                PrecioActual = vuelo.PrecioActual,
                ServicioId = servicioId,
                NombreProveedor = servicio.Nombre
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo vuelo {IdVuelo}", idVuelo);
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, string idVuelo, int pasajeros)
    {
        try
        {
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return false;

            var uri = $"{detalle.UriBase}{detalle.ConfirmarProductoEndpoint}";

            return await Connector.VerificarDisponibilidadVueloAsync(uri, idVuelo, pasajeros);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de vuelo");
            return false;
        }
    }
}
