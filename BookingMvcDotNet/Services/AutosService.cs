using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioAPIConnector.Autos;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementación del servicio de autos que usa TravelioAPIConnector para SOAP/REST.
/// Nota: El Connector está configurado para usar SOAP (IsREST = false en Global.cs).
/// </summary>
public class AutosService(TravelioDbContext dbContext, ILogger<AutosService> logger) : IAutosService
{
    /// <summary>
    /// Completa una URL de imagen relativa con la URL base del proveedor.
    /// Si la URL ya es absoluta (empieza con http), la retorna sin cambios.
    /// </summary>
    private static string CompletarUrlImagen(string? uriImagen, string uriBase)
    {
        if (string.IsNullOrEmpty(uriImagen))
            return "";

        // Si ya es URL absoluta, retornar sin cambios
        if (uriImagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uriImagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return uriImagen;
        }

        // Obtener solo el dominio base (sin el path del endpoint SOAP)
        try
        {
            var baseUri = new Uri(uriBase);
            var dominioBase = $"{baseUri.Scheme}://{baseUri.Host}";
            
            if (baseUri.Port != 80 && baseUri.Port != 443)
                dominioBase += $":{baseUri.Port}";

            // Combinar dominio con la ruta relativa
            if (!uriImagen.StartsWith("/"))
                uriImagen = "/" + uriImagen;

            return dominioBase + uriImagen;
        }
        catch
        {
            // Si falla el parsing, retornar la imagen original
            return uriImagen;
        }
    }

    public async Task<AutosSearchViewModel> BuscarAutosAsync(AutosSearchViewModel filtros)
    {
        var resultado = new AutosSearchViewModel
        {
            Ciudad = filtros.Ciudad,
            Categoria = filtros.Categoria,
            Transmision = filtros.Transmision,
            Capacidad = filtros.Capacidad,
            PrecioMin = filtros.PrecioMin,
            PrecioMax = filtros.PrecioMax,
            FechaInicio = filtros.FechaInicio,
            FechaFin = filtros.FechaFin
        };

        try
        {
            // Obtener todos los servicios de renta de autos activos
            var servicios = await dbContext.Servicios
                .Where(s => s.TipoServicio == TipoServicio.RentaVehiculos && s.Activo)
                .ToListAsync();

            // Obtener los detalles de servicio por separado
            var servicioIds = servicios.Select(s => s.Id).ToList();
            var detalles = await dbContext.DetallesServicio
                .Where(d => servicioIds.Contains(d.ServicioId))
                .ToListAsync();

            var todosLosAutos = new List<AutoViewModel>();

            foreach (var servicio in servicios)
            {
                try
                {
                    // Buscar detalle SOAP (preferido porque IsREST = false en el Connector)
                    var detalle = detalles
                        .Where(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Soap)
                        .FirstOrDefault();

                    if (detalle == null) continue;

                    // Construir URI completa para SOAP
                    var uri = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";

                    logger.LogInformation("Consultando {Servicio} (SOAP): {Uri}",
                        servicio.Nombre, uri);

                    // Llamar al Connector existente de TravelioAPIConnector (usa SOAP)
                    var vehiculos = await Connector.GetVehiculosAsync(
                        uri,
                        filtros.Categoria,
                        filtros.Transmision,
                        filtros.Capacidad,
                        filtros.PrecioMin,
                        filtros.PrecioMax,
                        null, // sort
                        filtros.Ciudad,
                        null  // pais
                    );

                    // Mapear resultados al ViewModel
                    foreach (var v in vehiculos)
                    {
                        var imagenUrl = CompletarUrlImagen(v.UriImagen, detalle.UriBase);
                        
                        logger.LogInformation("Auto {IdAuto} de {Proveedor}: Imagen='{Imagen}'",
                            v.IdAuto, servicio.Nombre, imagenUrl);
                        
                        todosLosAutos.Add(new AutoViewModel
                        {
                            IdAuto = v.IdAuto,
                            Tipo = v.Tipo,
                            CapacidadPasajeros = v.CapacidadPasajeros,
                            PrecioNormalPorDia = v.PrecioNormalPorDia,
                            PrecioActualPorDia = v.PrecioActualPorDia,
                            DescuentoPorcentaje = v.DescuentoPorcentaje,
                            UriImagen = imagenUrl,
                            Ciudad = v.Ciudad,
                            Pais = v.Pais,
                            ServicioId = servicio.Id,
                            NombreProveedor = servicio.Nombre
                        });
                    }

                    logger.LogInformation("Encontrados {Count} vehículos en {Servicio}",
                        vehiculos.Length, servicio.Nombre);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error consultando {Servicio}", servicio.Nombre);
                    // Continuar con el siguiente proveedor
                }
            }

            resultado.Resultados = todosLosAutos;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en búsqueda de autos");
            resultado.ErrorMessage = "Error al buscar vehículos. Intente nuevamente.";
        }

        return resultado;
    }

    public async Task<AutoDetalleViewModel?> ObtenerAutoAsync(int servicioId, string idAuto)
    {
        try
        {
            var servicio = await dbContext.Servicios
                .FirstOrDefaultAsync(s => s.Id == servicioId);

            if (servicio == null) return null;

            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return null;

            var uri = $"{detalle.UriBase}{detalle.ObtenerProductosEndpoint}";

            var vehiculos = await Connector.GetVehiculosAsync(uri);
            var vehiculo = vehiculos.FirstOrDefault(v => v.IdAuto == idAuto);

            if (string.IsNullOrEmpty(vehiculo.IdAuto)) return null;

            return new AutoDetalleViewModel
            {
                IdAuto = vehiculo.IdAuto,
                Tipo = vehiculo.Tipo,
                CapacidadPasajeros = vehiculo.CapacidadPasajeros,
                PrecioNormalPorDia = vehiculo.PrecioNormalPorDia,
                PrecioActualPorDia = vehiculo.PrecioActualPorDia,
                DescuentoPorcentaje = vehiculo.DescuentoPorcentaje,
                UriImagen = CompletarUrlImagen(vehiculo.UriImagen, detalle.UriBase),
                Ciudad = vehiculo.Ciudad,
                Pais = vehiculo.Pais,
                ServicioId = servicioId,
                NombreProveedor = servicio.Nombre
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error obteniendo auto {IdAuto}", idAuto);
            return null;
        }
    }

    public async Task<bool> VerificarDisponibilidadAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null) return false;

            var uri = $"{detalle.UriBase}{detalle.ConfirmarProductoEndpoint}";

            return await Connector.VerificarDisponibilidadAutoAsync(uri, idAuto, fechaInicio, fechaFin);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad");
            return false;
        }
    }

    public async Task<(bool exito, string mensaje)> CrearPrerreservaAsync(int servicioId, string idAuto, DateTime fechaInicio, DateTime fechaFin)
    {
        try
        {
            var detalle = await dbContext.DetallesServicio
                .Where(d => d.ServicioId == servicioId && d.TipoProtocolo == TipoProtocolo.Soap)
                .FirstOrDefaultAsync();

            if (detalle == null)
                return (false, "Servicio no encontrado");

            var uri = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";

            var (holdId, expiracion) = await Connector.CrearPrerreservaAsync(uri, idAuto, fechaInicio, fechaFin);

            return (true, $"Prerreserva creada: {holdId}, expira: {expiracion:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creando prerreserva");
            return (false, "Error al crear prerreserva");
        }
    }
}

