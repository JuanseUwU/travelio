namespace TravelioIntegrator.Models;

public record struct ProductoServicio<TProducto>(int ServicioId, string ServicioNombre, TProducto Producto)
{
    public string? UriBase { get; init; }
}
