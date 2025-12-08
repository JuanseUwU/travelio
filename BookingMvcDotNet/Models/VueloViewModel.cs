namespace BookingMvcDotNet.Models;

/// <summary>
/// ViewModel para mostrar un vuelo en la lista.
/// </summary>
public class VueloViewModel
{
    public string IdVuelo { get; set; } = "";
    public string Origen { get; set; } = "";
    public string Destino { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string TipoCabina { get; set; } = "";
    public string NombreAerolinea { get; set; } = "";
    public int CapacidadPasajeros { get; set; }
    public int AsientosDisponibles { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";

    public bool TieneDescuento => PrecioActual < PrecioNormal && PrecioNormal > 0;
    public decimal DescuentoPorcentaje => PrecioNormal > 0 ? (1 - PrecioActual / PrecioNormal) * 100 : 0;
}

/// <summary>
/// ViewModel para la búsqueda de vuelos.
/// </summary>
public class VuelosSearchViewModel
{
    public string? Origen { get; set; }
    public string? Destino { get; set; }
    public DateTime? FechaSalida { get; set; }
    public string? TipoCabina { get; set; }
    public int Pasajeros { get; set; } = 1;
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }

    public List<VueloViewModel> Resultados { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ViewModel para el detalle de un vuelo.
/// </summary>
public class VueloDetalleViewModel
{
    public string IdVuelo { get; set; } = "";
    public string Origen { get; set; } = "";
    public string Destino { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string TipoCabina { get; set; } = "";
    public string NombreAerolinea { get; set; } = "";
    public int CapacidadPasajeros { get; set; }
    public int AsientosDisponibles { get; set; }
    public decimal PrecioNormal { get; set; }
    public decimal PrecioActual { get; set; }
    
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";
    
    public int NumeroPasajeros { get; set; } = 1;

    public bool TieneDescuento => PrecioActual < PrecioNormal && PrecioNormal > 0;
    public decimal DescuentoPorcentaje => PrecioNormal > 0 ? (1 - PrecioActual / PrecioNormal) * 100 : 0;
    public decimal PrecioTotal => PrecioActual * NumeroPasajeros;
}
