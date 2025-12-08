using System.Collections.Generic;

namespace BookingMvcDotNet.Models
{
    public class ServiceDetailViewModel
    {
        public string Tipo { get; set; } = "";      // HOTEL / CAR / FLIGHT / RESTAURANT
        public string Titulo { get; set; } = "";
        public string Ciudad { get; set; } = "";

        // ================================================================
        // PROPIEDAD DE RATING (NUEVA)
        // ================================================================
        public double Rating { get; set; }

        public decimal Precio { get; set; }
        public string? UnidadPrecio { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? Descripcion { get; set; }
    }
}