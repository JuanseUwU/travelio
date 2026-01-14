using BookingMvcDotNet.Models;
using TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class HotelesService(TravelioIntegrationService integrationService, ILogger<HotelesService> logger) : IHotelesService
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

    private static string[] CompletarImagenes(string[] imagenes, string? uriBase)
    {
        if (imagenes.Length == 0)
        {
            return Array.Empty<string>();
        }

        return imagenes
            .Where(img => !string.IsNullOrWhiteSpace(img))
            .Select(img => CompletarUrlImagen(img.Trim(), uriBase))
            .Where(img => !string.IsNullOrWhiteSpace(img))
            .ToArray();
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
            var filtroApi = new FiltroHabitaciones(
                FechaInicio: filtros.FechaInicio,
                FechaFin: filtros.FechaFin,
                TipoHabitacion: filtros.TipoHabitacion,
                Capacidad: filtros.Capacidad,
                PrecioMin: filtros.PrecioMin,
                PrecioMax: filtros.PrecioMax);

            var habitaciones = await integrationService.BuscarHabitacionesAsync(filtroApi, logger);
            if (habitaciones is null)
            {
                resultado.ErrorMessage = "Error al buscar habitaciones. Intente nuevamente.";
                return resultado;
            }

            var resultados = new List<HabitacionViewModel>();
            foreach (var habitacion in habitaciones)
            {
                var h = habitacion.Producto;
                if (!string.IsNullOrWhiteSpace(filtros.Ciudad) &&
                    !h.Ciudad.Contains(filtros.Ciudad, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                resultados.Add(new HabitacionViewModel
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
                    Imagenes = CompletarImagenes(h.Imagenes, habitacion.UriBase),
                    ServicioId = habitacion.ServicioId,
                    NombreProveedor = habitacion.ServicioNombre
                });
            }

            resultado.Resultados = resultados;
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
            var resultado = await integrationService.ObtenerHabitacionAsync(servicioId, idHabitacion, logger);
            if (resultado is not { } producto)
            {
                return null;
            }

            var h = producto.Producto;

            return new HabitacionDetalleViewModel
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
                Imagenes = CompletarImagenes(h.Imagenes, producto.UriBase),
                ServicioId = producto.ServicioId,
                NombreProveedor = producto.ServicioNombre
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
            var disponible = await integrationService.VerificarDisponibilidadHabitacionAsync(servicioId, idHabitacion, fechaInicio, fechaFin, logger);
            return disponible ?? false;
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
            return await integrationService.CrearPrerreservaHabitacionAsync(
                servicioId,
                idHabitacion,
                fechaInicio,
                fechaFin,
                numeroHuespedes,
                precioActual,
                logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creando prerreserva");
            return (false, "Error al crear prerreserva", null);
        }
    }
}
