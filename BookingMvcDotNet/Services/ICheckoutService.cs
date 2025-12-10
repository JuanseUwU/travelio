using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Servicio para procesar el checkout/pago del carrito y cancelaciones.
/// </summary>
public interface ICheckoutService
{
    /// <summary>
    /// Procesa el pago completo del carrito.
    /// </summary>
    Task<CheckoutResult> ProcesarCheckoutAsync(int clienteId, int cuentaBancariaCliente, List<CartItemViewModel> items, DatosFacturacion datosFacturacion);

    /// <summary>
    /// Cancela una reserva y reembolsa al cliente (solo funciona con REST).
    /// </summary>
    Task<CancelacionResult> CancelarReservaAsync(int reservaId, int clienteId, int cuentaBancariaCliente);
}

/// <summary>
/// Resultado de una cancelacion de reserva.
/// </summary>
public class CancelacionResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = "";
    public decimal MontoReembolsado { get; set; }
}

/// <summary>
/// Datos de facturación del cliente.
/// </summary>
public class DatosFacturacion
{
    public string NombreCompleto { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string Correo { get; set; } = "";
}

/// <summary>
/// Resultado del proceso de checkout.
/// </summary>
public class CheckoutResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = "";
    public int? CompraId { get; set; }
    public decimal TotalPagado { get; set; }
    public List<ReservaResult> Reservas { get; set; } = new();
}

/// <summary>
/// Resultado de una reserva individual.
/// </summary>
public class ReservaResult
{
    public string Tipo { get; set; } = "";  // CAR, HOTEL, etc.
    public string Titulo { get; set; } = "";
    public string CodigoReserva { get; set; } = "";
    public string? FacturaProveedorUrl { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
