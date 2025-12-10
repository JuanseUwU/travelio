namespace BookingMvcDotNet.Models;

public class AdminDashboardViewModel
{
    // Estadísticas generales
    public int TotalClientes { get; set; }
    public int TotalCompras { get; set; }
    public int TotalReservas { get; set; }
    public decimal IngresosTotales { get; set; }
    public decimal ComisionesTotales { get; set; }
    
    // Estadísticas de hoy
    public int ComprasHoy { get; set; }
    public decimal IngresosHoy { get; set; }
    
    // Estadísticas de reservas
    public int ReservasActivas { get; set; }
    public int ReservasCanceladas { get; set; }
    
    // Listas
    public List<ClienteAdminViewModel> Clientes { get; set; } = [];
    public List<CompraAdminViewModel> UltimasCompras { get; set; } = [];
    public List<ReservaAdminViewModel> Reservas { get; set; } = [];
    public List<ProveedorStatusViewModel> EstadoProveedores { get; set; } = [];
}

public class ClienteAdminViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Documento { get; set; } = "";
    public DateTime FechaRegistro { get; set; }
    public int TotalCompras { get; set; }
    public decimal TotalGastado { get; set; }
}

public class CompraAdminViewModel
{
    public int Id { get; set; }
    public string ClienteNombre { get; set; } = "";
    public string ClienteEmail { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public int CantidadReservas { get; set; }
    public List<ReservaAdminViewModel> Reservas { get; set; } = [];
}

public class ReservaAdminViewModel
{
    public int Id { get; set; }
    public string CodigoReserva { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string TipoServicio { get; set; } = "";
    public bool Activa { get; set; }
    public string? FacturaUrl { get; set; }
    public decimal ValorNegocio { get; set; }
    public decimal Comision { get; set; }
    public DateTime? FechaCompra { get; set; }
    public string ClienteNombre { get; set; } = "";
}

public class ProveedorStatusViewModel
{
    public int ServicioId { get; set; }
    public string Nombre { get; set; } = "";
    public string TipoServicio { get; set; } = "";
    public bool TieneRest { get; set; }
    public bool TieneSoap { get; set; }
    public string? UrlRest { get; set; }
    public string? UrlSoap { get; set; }
    public bool Activo { get; set; }
    public string EstadoRest { get; set; } = "No configurado";
    public string EstadoSoap { get; set; } = "No configurado";
}
