using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioAPIConnector.Aerolinea;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementacion del servicio de vuelos que usa TravelioAPIConnector para REST/SOAP.
/// Prioriza REST y usa SOAP como fallback. Cancelacion solo disponible en REST.
/// </summary>
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
                    var detalleRest = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Rest);
                    var detalleSoap = detalles.FirstOrDefault(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap);

                    Vuelo[] vuelos = [];
                    bool usandoRest = false;

                    // Intentar REST primero
                    if (detalleRest != null)
                    {
                        try
                        {
                            var uriRest = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                            logger.LogInformation("Consultando {Servicio} (REST): {Uri}", servicio.Nombre, uriRest);

                            vuelos = await Connector.GetVuelosAsync(
                                uriRest, filtros.Origen, filtros.Destino, filtros.FechaSalida, null,
                                filtros.TipoCabina, filtros.Pasajeros, filtros.PrecioMin, filtros.PrecioMax);
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

                        vuelos = await Connector.GetVuelosAsync(
                            uriSoap, filtros.Origen, filtros.Destino, filtros.FechaSalida, null,
                            filtros.TipoCabina, filtros.Pasajeros, filtros.PrecioMin, filtros.PrecioMax, forceSoap: true);
                    }

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

                    logger.LogInformation("Encontrados {Count} vuelos en {Servicio} ({Protocolo})",
                        vuelos.Length, servicio.Nombre, usandoRest ? "REST" : "SOAP");
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
            logger.LogError(ex, "Error en busqueda de vuelos");
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

            var detalles = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId)
                .ToListAsync();

            var detalleRest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
            var detalleSoap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);

            Vuelo[] vuelos = [];

            if (detalleRest != null)
            {
                try
                {
                    var uri = $"{detalleRest.UriBase}{detalleRest.ObtenerProductosEndpoint}";
                    vuelos = await Connector.GetVuelosAsync(uri);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (vuelos.Length == 0 && detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ObtenerProductosEndpoint}";
                vuelos = await Connector.GetVuelosAsync(uri, forceSoap: true);
            }

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
                    return await Connector.VerificarDisponibilidadVueloAsync(uri, idVuelo, pasajeros);
                }
                catch { /* Fallback a SOAP */ }
            }

            if (detalleSoap != null)
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.ConfirmarProductoEndpoint}";
                return await Connector.VerificarDisponibilidadVueloAsync(uri, idVuelo, pasajeros, forceSoap: true);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de vuelo");
            return false;
        }
    }
}
