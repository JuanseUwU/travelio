using System.ComponentModel.DataAnnotations;

namespace BookingMvcDotNet.Models
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ\s]{2,40}$",
            ErrorMessage = "Solo letras, 2–40 caracteres.")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ\s]{2,40}$",
            ErrorMessage = "Solo letras, 2–40 caracteres.")]
        [Display(Name = "Apellido")]
        public string Apellido { get; set; } = "";

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            ErrorMessage = "Correo no válido (ej: nombre@dominio.com).")]
        [Display(Name = "Correo")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100,
            ErrorMessage = "La {0} debe tener al menos {2} caracteres.",
            MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "La contraseña debe tener mayúscula, minúscula, número y símbolo.")]
        public string Password { get; set; } = "";

        // Campos adicionales requeridos por TravelioDb
        [Required(ErrorMessage = "El tipo de identificación es obligatorio.")]
        [Display(Name = "Tipo de Identificación")]
        public string TipoIdentificacion { get; set; } = "Cedula";

        [Required(ErrorMessage = "El documento de identidad es obligatorio.")]
        [Display(Name = "Número de Documento")]
        public string DocumentoIdentidad { get; set; } = "";

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-18);

        [Display(Name = "Teléfono")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 dígitos numéricos.")]
        public string? Telefono { get; set; }

        [Display(Name = "País")]
        public string? Pais { get; set; }

        // Validación personalizada según el tipo de documento
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(DocumentoIdentidad))
                yield break;

            switch (TipoIdentificacion)
            {
                case "Cedula":
                    if (!System.Text.RegularExpressions.Regex.IsMatch(DocumentoIdentidad, @"^\d{10}$"))
                    {
                        yield return new ValidationResult(
                            "La cédula debe tener exactamente 10 dígitos numéricos.",
                            new[] { nameof(DocumentoIdentidad) });
                    }
                    break;

                case "RUC":
                    if (!System.Text.RegularExpressions.Regex.IsMatch(DocumentoIdentidad, @"^\d{13}$"))
                    {
                        yield return new ValidationResult(
                            "El RUC debe tener exactamente 13 dígitos numéricos.",
                            new[] { nameof(DocumentoIdentidad) });
                    }
                    break;

                case "Pasaporte":
                    if (!System.Text.RegularExpressions.Regex.IsMatch(DocumentoIdentidad, @"^[A-Za-z0-9]{6,20}$"))
                    {
                        yield return new ValidationResult(
                            "El pasaporte debe tener entre 6 y 20 caracteres alfanuméricos.",
                            new[] { nameof(DocumentoIdentidad) });
                    }
                    break;
            }

            // Validar que la fecha de nacimiento sea de alguien mayor de 18 años
            var edad = DateTime.Today.Year - FechaNacimiento.Year;
            if (FechaNacimiento.Date > DateTime.Today.AddYears(-edad)) edad--;
            
            if (edad < 18)
            {
                yield return new ValidationResult(
                    "Debes ser mayor de 18 años para registrarte.",
                    new[] { nameof(FechaNacimiento) });
            }
        }
    }
}
