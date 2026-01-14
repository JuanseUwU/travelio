using BookingMvcDotNet.Models;
using IntegratorModels = TravelioIntegrator.Models;
using TravelioIntegrator.Services;

namespace BookingMvcDotNet.Services;

public class CheckoutService(TravelioIntegrationService integrationService, ILogger<CheckoutService> logger) : ICheckoutService
{
    public async Task<CheckoutResult> ProcesarCheckoutAsync(
        int clienteId,
        int cuentaBancariaCliente,
        List<CartItemViewModel> items,
        DatosFacturacion datosFacturacion)
    {
        var checkoutItems = items.Select(item => new IntegratorModels.CheckoutItem
        {
            Tipo = item.Tipo,
            Titulo = item.Titulo,
            ServicioId = item.ServicioId,
            IdProducto = item.IdProducto,
            FechaInicio = item.FechaInicio,
            FechaFin = item.FechaFin,
            NumeroPersonas = item.NumeroPersonas,
            PrecioFinal = item.PrecioFinal,
            PrecioUnitario = item.PrecioUnitario,
            Cantidad = item.Cantidad
        }).ToList();

        var facturaInfo = new IntegratorModels.FacturaInfo(
            datosFacturacion.NombreCompleto,
            datosFacturacion.TipoDocumento,
            datosFacturacion.NumeroDocumento,
            datosFacturacion.Correo);

        var resultado = await integrationService.ProcesarCheckoutAsync(
            clienteId,
            cuentaBancariaCliente,
            checkoutItems,
            facturaInfo,
            logger);

        return MapCheckoutResult(resultado);
    }

    public async Task<CancelacionResult> CancelarReservaAsync(int reservaId, int clienteId, int cuentaBancariaCliente)
    {
        var resultado = await integrationService.CancelarReservaFrontendAsync(reservaId, clienteId, cuentaBancariaCliente, logger);

        return new CancelacionResult
        {
            Exitoso = resultado.Exitoso,
            Mensaje = resultado.Mensaje,
            MontoReembolsado = resultado.MontoReembolsado
        };
    }

    private static CheckoutResult MapCheckoutResult(IntegratorModels.CheckoutResult resultado)
    {
        var mapped = new CheckoutResult
        {
            Exitoso = resultado.Exitoso,
            Mensaje = resultado.Mensaje,
            CompraId = resultado.CompraId,
            TotalPagado = resultado.TotalPagado,
            Reservas = resultado.Reservas.Select(reserva => new ReservaResult
            {
                Tipo = reserva.Tipo,
                Titulo = reserva.Titulo,
                CodigoReserva = reserva.CodigoReserva,
                FacturaProveedorUrl = reserva.FacturaProveedorUrl,
                Exitoso = reserva.Exitoso,
                Error = reserva.Error
            }).ToList()
        };

        return mapped;
    }
}
