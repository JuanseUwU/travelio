using TravelioDatabaseConnector.Enums;
using TravelioDatabaseConnector.Models.Carrito;

namespace TravelioDatabaseConnector.Models;

public class Cliente
{
    public int Id { get; set; }
    public string CorreoElectronico { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public string Apellido { get; set; } = default!;
    public string? Pais { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string? Telefono { get; set; }
    public string TipoIdentificacion { get; set; } = default!;
    public string DocumentoIdentidad { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string PasswordSalt { get; set; } = default!;
    public RolUsuario Rol { get; set; } = RolUsuario.Cliente;

    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public ICollection<CarritoAerolineaItem> CarritoAerolineaItems { get; set; } = new List<CarritoAerolineaItem>();
    public ICollection<CarritoHabitacionItem> CarritoHabitacionItems { get; set; } = new List<CarritoHabitacionItem>();
    public ICollection<CarritoAutoItem> CarritoAutoItems { get; set; } = new List<CarritoAutoItem>();
    public ICollection<CarritoPaqueteItem> CarritoPaqueteItems { get; set; } = new List<CarritoPaqueteItem>();
    public ICollection<CarritoMesaItem> CarritoMesaItems { get; set; } = new List<CarritoMesaItem>();
}
