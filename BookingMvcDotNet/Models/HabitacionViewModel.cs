namespace BookingMvcDotNet.Models;

/// <summary>
/// ViewModel para mostrar una habitación en la lista.
/// </summary>
public class HabitacionViewModel
{
    public string IdHabitacion { get; set; } = "";
    public string NombreHabitacion { get; set; } = "";
    public string TipoHabitacion { get; set; } = "";
    public string Hotel { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Pais { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    public string Amenidades { get; set; } = "";
    public string[] Imagenes { get; set; } = [];
    
    // Datos del proveedor (servicio)
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";

    // Propiedades calculadas
    public bool TieneDescuento => PrecioActual < PrecioNormal && PrecioNormal > 0;
    public decimal DescuentoPorcentaje => PrecioNormal > 0 ? (1 - PrecioActual / PrecioNormal) * 100 : 0;
    public string ImagenPrincipal => Imagenes.Length > 0 ? Imagenes[0] : "";
}

/// <summary>
/// ViewModel para la búsqueda de habitaciones con filtros.
/// </summary>
public class HabitacionesSearchViewModel
{
    // Filtros de búsqueda
    public string? Ciudad { get; set; }
    public string? TipoHabitacion { get; set; }
    public int? Capacidad { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int NumeroHuespedes { get; set; } = 2;

    // Resultados
    public List<HabitacionViewModel> Resultados { get; set; } = new();

    // Errores
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ViewModel para el detalle de una habitación.
/// </summary>
public class HabitacionDetalleViewModel
{
    public string IdHabitacion { get; set; } = "";
    public string NombreHabitacion { get; set; } = "";
    public string TipoHabitacion { get; set; } = "";
    public string Hotel { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Pais { get; set; } = "";
    public int Capacidad { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    public string Amenidades { get; set; } = "";
    public string[] Imagenes { get; set; } = [];
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";

    // Datos de búsqueda
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int NumeroHuespedes { get; set; } = 2;

    // Propiedades calculadas
    public bool TieneDescuento => PrecioActual < PrecioNormal && PrecioNormal > 0;
    public decimal DescuentoPorcentaje => PrecioNormal > 0 ? (1 - PrecioActual / PrecioNormal) * 100 : 0;
    public string ImagenPrincipal => Imagenes.Length > 0 ? Imagenes[0] : "";
    public string[] AmenidadesList => Amenidades.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    
    public int NumeroNoches => FechaInicio.HasValue && FechaFin.HasValue 
        ? Math.Max(1, (FechaFin.Value - FechaInicio.Value).Days) 
        : 1;
    public decimal PrecioTotal => PrecioActual * NumeroNoches;
}
