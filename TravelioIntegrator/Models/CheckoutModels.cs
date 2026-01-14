using System.Collections.Generic;

namespace TravelioIntegrator.Models;

public class CheckoutItem
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public int ServicioId { get; set; }
    public string IdProducto { get; set; } = string.Empty;
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int? NumeroPersonas { get; set; }
    public decimal PrecioFinal { get; set; }
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class CheckoutResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public int? CompraId { get; set; }
    public decimal TotalPagado { get; set; }
    public List<ReservaResult> Reservas { get; set; } = new();
}

public class CancelacionResult
{
    public bool Exitoso { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public decimal MontoReembolsado { get; set; }
}

public class ReservaResult
{
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string CodigoReserva { get; set; } = string.Empty;
    public string? FacturaProveedorUrl { get; set; }
    public bool Exitoso { get; set; }
    public string? Error { get; set; }
}
