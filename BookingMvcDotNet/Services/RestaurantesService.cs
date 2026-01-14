using BookingMvcDotNet.Models;
using TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class RestaurantesService(TravelioIntegrationService integrationService, ILogger<RestaurantesService> logger) : IRestaurantesService
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
            var filtroApi = new FiltroMesas(
                Capacidad: filtros.Capacidad,
                TipoMesa: filtros.TipoMesa,
                Estado: "Disponible");

            var mesas = await integrationService.BuscarMesasAsync(filtroApi, logger);
            if (mesas is null)
            {
                resultado.ErrorMessage = "Error al buscar mesas. Intente nuevamente.";
                return resultado;
            }

            resultado.Resultados = mesas.Select(mesa =>
            {
                var m = mesa.Producto;
                return new MesaViewModel
                {
                    IdMesa = m.IdMesa,
                    IdRestaurante = m.IdRestaurante,
                    NumeroMesa = m.NumeroMesa,
                    TipoMesa = m.TipoMesa,
                    Capacidad = m.Capacidad,
                    Precio = m.Precio,
                    ImagenUrl = CompletarUrlImagen(m.ImagenUrl, mesa.UriBase),
                    Estado = m.Estado,
                    ServicioId = mesa.ServicioId,
                    NombreProveedor = mesa.ServicioNombre
                };
            }).ToList();
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
            var resultado = await integrationService.ObtenerMesaAsync(servicioId, idMesa, logger);
            if (resultado is not { } producto)
            {
                return null;
            }

            var m = producto.Producto;

            return new MesaDetalleViewModel
            {
                IdMesa = m.IdMesa,
                IdRestaurante = m.IdRestaurante,
                NumeroMesa = m.NumeroMesa,
                TipoMesa = m.TipoMesa,
                Capacidad = m.Capacidad,
                Precio = m.Precio,
                ImagenUrl = CompletarUrlImagen(m.ImagenUrl, producto.UriBase),
                Estado = m.Estado,
                ServicioId = producto.ServicioId,
                NombreProveedor = producto.ServicioNombre
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
            var disponible = await integrationService.VerificarDisponibilidadMesaAsync(servicioId, idMesa, fecha, personas, logger);
            return disponible ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de mesa");
            return false;
        }
    }
}
