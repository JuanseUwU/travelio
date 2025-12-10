using TravelioDatabaseConnector.Enums;

namespace TravelioDatabaseConnector.Models;

public class DetalleServicio
{
    public int Id { get; set; }
    public int ServicioId { get; set; }
    public Servicio Servicio { get; set; } = null!;
    public TipoProtocolo TipoProtocolo { get; set; }
    public string? UriBase { get; set; }
    public string? ObtenerProductosEndpoint { get; set; }
    public string? RegistrarClienteEndpoint { get; set; }
    public string? ConfirmarProductoEndpoint { get; set; }
    public string? CrearPrerreservaEndpoint { get; set; }
    public string? CrearReservaEndpoint { get; set; }
    public string? GenerarFacturaEndpoint { get; set; }
    public string? ObtenerReservaEndpoint { get; set; }
    public string? CancelarReservaEndpoint { get; set; }
}
