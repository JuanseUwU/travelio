namespace BookingMvcDotNet.Models;

public class MesaViewModel
{
    public int IdMesa { get; set; }
    public int IdRestaurante { get; set; }
    public int NumeroMesa { get; set; }
    public string TipoMesa { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal Precio { get; set; }
    public string ImagenUrl { get; set; } = "";
    public string Estado { get; set; } = "";
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";
}

public class MesasSearchViewModel
{
    public int? Capacidad { get; set; }
    public string? TipoMesa { get; set; }
    public DateTime? Fecha { get; set; }
    public int NumeroPersonas { get; set; } = 2;

    public List<MesaViewModel> Resultados { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class MesaDetalleViewModel
{
    public int IdMesa { get; set; }
    public int IdRestaurante { get; set; }
    public int NumeroMesa { get; set; }
    public string TipoMesa { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal Precio { get; set; }
    public string ImagenUrl { get; set; } = "";
    public string Estado { get; set; } = "";
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";
    
    public DateTime? FechaReserva { get; set; }
    public int NumeroPersonas { get; set; } = 2;
}
