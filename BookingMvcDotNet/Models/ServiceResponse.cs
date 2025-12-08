namespace BookingMvcDotNet.Models
{
    public class ServiceResponse<T>
    {
        public T? Data { get; set; }

        // Inicializamos en true por defecto
        public bool Success { get; set; } = true;

        public int StatusCode { get; set; }

        // ESTA ES LA PROPIEDAD QUE FALTABA:
        // Se usa para mensajes generales (ej: "Búsqueda exitosa" o "Error de validación")
        public string Message { get; set; } = string.Empty;

        // Esta ya la tenías, sirve para detalles técnicos del error
        public string? ErrorMessage { get; set; }
    }
}