using System.ComponentModel.DataAnnotations;

namespace BookingMvcDotNet.Models;

/// <summary>
/// ViewModel para la página de checkout/pago.
/// </summary>
public class CheckoutViewModel
{
    // Items del carrito
    public List<CartItemViewModel> Items { get; set; } = new();

    // Datos de facturación (pre-llenados del cliente)
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "El tipo de documento es obligatorio.")]
    [Display(Name = "Tipo de documento")]
    public string TipoDocumento { get; set; } = "";

    [Required(ErrorMessage = "El número de documento es obligatorio.")]
    [Display(Name = "Número de documento")]
    public string NumeroDocumento { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [Display(Name = "Correo electrónico")]
    public string Correo { get; set; } = "";

    // Datos bancarios para el pago
    [Required(ErrorMessage = "El número de cuenta es obligatorio.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Solo se permiten números.")]
    [Display(Name = "Número de cuenta bancaria")]
    public string NumeroCuentaBancaria { get; set; } = "";

    // Totales calculados
    public decimal Subtotal => Items.Sum(i => i.PrecioFinal * i.Cantidad);
    public decimal IVA => Subtotal * 0.12m;
    public decimal Total => Subtotal + IVA;
}
