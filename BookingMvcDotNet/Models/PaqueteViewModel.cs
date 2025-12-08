namespace BookingMvcDotNet.Models;

public class PaqueteViewModel
{
    public string IdPaquete { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Pais { get; set; } = "";
    public string TipoActividad { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    public string ImagenUrl { get; set; } = "";
    public int Duracion { get; set; } // días
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";

    public bool TieneDescuento => PrecioActual < PrecioNormal && PrecioNormal > 0;
    public decimal DescuentoPorcentaje => PrecioNormal > 0 ? (1 - PrecioActual / PrecioNormal) * 100 : 0;
}

public class PaquetesSearchViewModel
{
    public string? Ciudad { get; set; }
    public DateTime? FechaInicio { get; set; }
    public string? TipoActividad { get; set; }
    public decimal? PrecioMax { get; set; }
    public int Personas { get; set; } = 1;

    public List<PaqueteViewModel> Resultados { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class PaqueteDetalleViewModel
{
    public string IdPaquete { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Pais { get; set; } = "";
    public string TipoActividad { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    public string ImagenUrl { get; set; } = "";
    public int Duracion { get; set; }
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";
    
    public DateTime? FechaInicio { get; set; }
    public int NumeroPersonas { get; set; } = 1;

    public bool TieneDescuento => PrecioActual < PrecioNormal && PrecioNormal > 0;
    public decimal DescuentoPorcentaje => PrecioNormal > 0 ? (1 - PrecioActual / PrecioNormal) * 100 : 0;
    public decimal PrecioTotal => PrecioActual * NumeroPersonas;
}
