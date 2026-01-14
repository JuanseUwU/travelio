using BookingMvcDotNet.Models;
using TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class AutosService(TravelioIntegrationService integrationService, ILogger<AutosService> logger) : IAutosService
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
            var filtroApi = new FiltroAutos(
                Categoria: filtros.Categoria,
                Transmision: filtros.Transmision,
                Capacidad: filtros.Capacidad,
                PrecioMin: filtros.PrecioMin,
                PrecioMax: filtros.PrecioMax,
                Sort: null,
                Ciudad: filtros.Ciudad,
                Pais: null);

            var autos = await integrationService.BuscarAutosAsync(filtroApi, logger);
            if (autos is null)
            {
                resultado.ErrorMessage = "Error al buscar vehiculos. Intente nuevamente.";
                return resultado;
            }

            resultado.Resultados = autos.Select(auto =>
            {
                var v = auto.Producto;
                return new AutoViewModel
                {
                    IdAuto = v.IdAuto,
                    Tipo = v.Tipo,
                    CapacidadPasajeros = v.CapacidadPasajeros,
                    PrecioNormalPorDia = v.PrecioNormalPorDia,
                    PrecioActualPorDia = v.PrecioActualPorDia,
                    DescuentoPorcentaje = v.DescuentoPorcentaje,
                    UriImagen = CompletarUrlImagen(v.UriImagen, auto.UriBase),
                    Ciudad = v.Ciudad,
                    Pais = v.Pais,
                    ServicioId = auto.ServicioId,
                    NombreProveedor = auto.ServicioNombre
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en busqueda de autos");
            resultado.ErrorMessage = "Error al buscar vehiculos. Intente nuevamente.";
        }

        return resultado;
    }

    public async Task<AutoDetalleViewModel?> ObtenerAutoAsync(int servicioId, string idAuto)
    {
        try
        {
            var resultado = await integrationService.ObtenerAutoAsync(servicioId, idAuto, logger);
            if (resultado is not { } producto)
            {
                return null;
            }

            var v = producto.Producto;

            return new AutoDetalleViewModel
            {
                IdAuto = v.IdAuto,
                Tipo = v.Tipo,
                CapacidadPasajeros = v.CapacidadPasajeros,
                PrecioNormalPorDia = v.PrecioNormalPorDia,
                PrecioActualPorDia = v.PrecioActualPorDia,
                DescuentoPorcentaje = v.DescuentoPorcentaje,
                UriImagen = CompletarUrlImagen(v.UriImagen, producto.UriBase),
                Ciudad = v.Ciudad,
                Pais = v.Pais,
                ServicioId = producto.ServicioId,
                NombreProveedor = producto.ServicioNombre
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
            var disponible = await integrationService.VerificarDisponibilidadAutoAsync(servicioId, idAuto, fechaInicio, fechaFin, logger);
            return disponible ?? false;
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
            return await integrationService.CrearPrerreservaAutoAsync(servicioId, idAuto, fechaInicio, fechaFin, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creando prerreserva");
            return (false, "Error al crear prerreserva");
        }
    }
}
