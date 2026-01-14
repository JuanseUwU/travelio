using BookingMvcDotNet.Models;
using TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class PaquetesService(TravelioIntegrationService integrationService, ILogger<PaquetesService> logger) : IPaquetesService
{
    private static string CompletarUrlImagen(string? uriImagen, string? uriBase)
    {
        if (string.IsNullOrEmpty(uriImagen))
        {
            return "";
        }

        if (uriImagen.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uriImagen.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return uriImagen;
        }

        if (string.IsNullOrWhiteSpace(uriBase))
        {
            return uriImagen;
        }

        try
        {
            var baseUri = new Uri(uriBase);
            var dominioBase = $"{baseUri.Scheme}://{baseUri.Host}";
            if (baseUri.Port != 80 && baseUri.Port != 443)
            {
                dominioBase += $":{baseUri.Port}";
            }

            if (!uriImagen.StartsWith("/"))
            {
                uriImagen = "/" + uriImagen;
            }

            return dominioBase + uriImagen;
        }
        catch
        {
            return uriImagen;
        }
    }

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
            var filtroApi = new FiltroPaquetes(
                Ciudad: filtros.Ciudad,
                FechaInicio: filtros.FechaInicio,
                TipoActividad: filtros.TipoActividad,
                PrecioMax: filtros.PrecioMax);

            var paquetes = await integrationService.BuscarPaquetesAsync(filtroApi, logger);
            if (paquetes is null)
            {
                resultado.ErrorMessage = "Error al buscar paquetes. Intente nuevamente.";
                return resultado;
            }

            resultado.Resultados = paquetes.Select(paquete =>
            {
                var p = paquete.Producto;
                return new PaqueteViewModel
                {
                    IdPaquete = p.IdPaquete,
                    Nombre = p.Nombre,
                    Ciudad = p.Ciudad,
                    Pais = p.Pais,
                    TipoActividad = p.TipoActividad,
                    Capacidad = p.Capacidad,
                    PrecioNormal = p.PrecioNormal,
                    PrecioActual = p.PrecioActual,
                    ImagenUrl = CompletarUrlImagen(p.ImagenUrl, paquete.UriBase),
                    Duracion = p.Duracion,
                    ServicioId = paquete.ServicioId,
                    NombreProveedor = paquete.ServicioNombre
                };
            }).ToList();
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
            var resultado = await integrationService.ObtenerPaqueteAsync(servicioId, idPaquete, logger);
            if (resultado is not { } producto)
            {
                return null;
            }

            var p = producto.Producto;

            return new PaqueteDetalleViewModel
            {
                IdPaquete = p.IdPaquete,
                Nombre = p.Nombre,
                Ciudad = p.Ciudad,
                Pais = p.Pais,
                TipoActividad = p.TipoActividad,
                Capacidad = p.Capacidad,
                PrecioNormal = p.PrecioNormal,
                PrecioActual = p.PrecioActual,
                ImagenUrl = CompletarUrlImagen(p.ImagenUrl, producto.UriBase),
                Duracion = p.Duracion,
                ServicioId = producto.ServicioId,
                NombreProveedor = producto.ServicioNombre
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
            var disponible = await integrationService.VerificarDisponibilidadPaqueteAsync(servicioId, idPaquete, fechaInicio, personas, logger);
            return disponible ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de paquete");
            return false;
        }
    }
}
