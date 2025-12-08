using BookingMvcDotNet.Models;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Servicio para procesar el checkout/pago del carrito.
/// </summary>
public interface ICheckoutService
{
    /// <summary>
    /// Procesa el pago completo del carrito:
    /// 1. Cobra al cliente usando la API del banco
    /// 2. Crea prerreservas y reservas en los proveedores SOAP
    /// 3. Genera facturas de los proveedores
    /// 4. Registra todo en TravelioDb
    /// </summary>
    Task<CheckoutResult> ProcesarCheckoutAsync(int clienteId, int cuentaBancariaCliente, List<CartItemViewModel> items, DatosFacturacion datosFacturacion);
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
