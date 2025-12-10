namespace TravelioDatabaseConnector.Models;

public class Reserva
{
    public int Id { get; set; }
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public string CodigoReserva { get; set; } = default!;
    public string? FacturaUrl { get; set; }
    public bool Activa { get; set; } = true;
    public decimal ValorPagadoNegocio { get; set; }
    public decimal ComisionAgencia { get; set; }

    public ICollection<ReservaCompra> ReservasCompra { get; set; } = new List<ReservaCompra>();
}
