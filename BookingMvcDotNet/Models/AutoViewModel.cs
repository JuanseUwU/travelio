namespace BookingMvcDotNet.Models;

/// <summary>
/// ViewModel para mostrar un vehículo de renta.
/// </summary>
public class AutoViewModel
{
    public string IdAuto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int CapacidadPasajeros { get; set; }
    public decimal PrecioNormalPorDia { get; set; }
    public decimal PrecioActualPorDia { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public string UriImagen { get; set; } = "";
    public string Ciudad { get; set; } = "";
    public string Pais { get; set; } = "";

    // Proveedor
    public int ServicioId { get; set; }
    public string NombreProveedor { get; set; } = "";

    public bool TieneDescuento => DescuentoPorcentaje > 0;
}

/// <summary>
/// ViewModel para la búsqueda de autos con filtros.
/// </summary>
public class AutosSearchViewModel
{
    public string? Ciudad { get; set; }
    public string? Categoria { get; set; }
    public string? Transmision { get; set; }
    public int? Capacidad { get; set; }
    public decimal? PrecioMin { get; set; }
    public decimal? PrecioMax { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public List<AutoViewModel> Resultados { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// ViewModel para el detalle de un auto con información de disponibilidad.
/// </summary>
public class AutoDetalleViewModel : AutoViewModel
{
    public DateTime FechaInicio { get; set; } = DateTime.Today.AddDays(1);
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(4);
    public bool Disponible { get; set; } = true;

    public int DiasRenta => Math.Max(1, (int)(FechaFin - FechaInicio).TotalDays);
    public decimal TotalEstimado => PrecioActualPorDia * DiasRenta;
}
