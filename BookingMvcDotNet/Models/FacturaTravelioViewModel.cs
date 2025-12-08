namespace BookingMvcDotNet.Models;

/// <summary>
/// ViewModel para la factura de Travelio.
/// </summary>
public class FacturaTravelioViewModel
{
    public int CompraId { get; set; }
    public string NumeroFactura { get; set; } = "";
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    
    // Datos del cliente
    public string ClienteNombre { get; set; } = "";
    public string ClienteDocumento { get; set; } = "";
    public string ClienteTipoDocumento { get; set; } = "";
    public string ClienteCorreo { get; set; } = "";
    
    // Items de la factura
    public List<FacturaItemViewModel> Items { get; set; } = new();
    
    // Totales
    public decimal Subtotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }
    public decimal PorcentajeIVA { get; set; } = 12m;
    
    // Información de pago
    public string MetodoPago { get; set; } = "Transferencia Bancaria";
    public string EstadoPago { get; set; } = "Pagado";
}

public class FacturaItemViewModel
{
    public string Descripcion { get; set; } = "";
    public string Tipo { get; set; } = "";  // CAR, HOTEL, etc.
    public string CodigoReserva { get; set; } = "";
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Total => Cantidad * PrecioUnitario;
}
