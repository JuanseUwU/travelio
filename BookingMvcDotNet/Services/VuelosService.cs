using BookingMvcDotNet.Models;
using TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class VuelosService(TravelioIntegrationService integrationService, ILogger<VuelosService> logger) : IVuelosService
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
            var filtroApi = new FiltroVuelos(
                Origen: filtros.Origen,
                Destino: filtros.Destino,
                FechaDespegue: filtros.FechaSalida,
                FechaLlegada: null,
                TipoCabina: filtros.TipoCabina,
                Pasajeros: filtros.Pasajeros,
                PrecioMin: filtros.PrecioMin,
                PrecioMax: filtros.PrecioMax);

            var vuelos = await integrationService.BuscarVuelosAsync(filtroApi, logger);
            if (vuelos is null)
            {
                resultado.ErrorMessage = "Error al buscar vuelos. Intente nuevamente.";
                return resultado;
            }

            resultado.Resultados = vuelos.Select(vuelo =>
            {
                var v = vuelo.Producto;
                return new VueloViewModel
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
                    ServicioId = vuelo.ServicioId,
                    NombreProveedor = vuelo.ServicioNombre
                };
            }).ToList();
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
            var resultado = await integrationService.ObtenerVueloAsync(servicioId, idVuelo, logger);
            if (resultado is not { } producto)
            {
                return null;
            }

            var v = producto.Producto;

            return new VueloDetalleViewModel
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
                ServicioId = producto.ServicioId,
                NombreProveedor = producto.ServicioNombre
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
            var disponible = await integrationService.VerificarDisponibilidadVueloAsync(servicioId, idVuelo, pasajeros, logger);
            return disponible ?? false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verificando disponibilidad de vuelo");
            return false;
        }
    }
}
