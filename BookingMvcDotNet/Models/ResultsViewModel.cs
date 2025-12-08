using System;
using System.Collections.Generic;

namespace BookingMvcDotNet.Models
{
    /// <summary>
    /// Un ítem individual de la lista de resultados (card de hotel, auto, etc.).
    /// </summary>
    public class ResultItemViewModel
    {
        public string Tipo { get; set; } = "";          // HOTEL, CAR, FLIGHT, RESTAURANT
        public string Titulo { get; set; } = "";        // "Hotel Sol Andino"
        public string Subtitulo { get; set; } = "";     // "Quito", "Guayaquil", etc.

        // ================================================================
        // PROPIEDAD DE RATING (NUEVA)
        // Necesaria para mostrar las estrellas y filtrar en el Home
        // ================================================================
        public double Rating { get; set; }

        // ================================================================
        // COMPATIBILIDAD CON VISTAS ANTERIORES:
        // La propiedad 'Ciudad' apunta a 'Subtitulo'.
        // ================================================================
        public string Ciudad
        {
            get { return Subtitulo; }
            set { Subtitulo = value; }
        }

        public decimal Precio { get; set; }             // 85, 35, etc.
        public string UnidadPrecio { get; set; } = "";  // "por noche", "por día"
        public List<string> Tags { get; set; } = new(); // badges tipo "Cancelación gratis"
    }

    /// <summary>
    /// ViewModel para la página de resultados.
    /// </summary>
    public class ResultsViewModel
    {
        public string? Query { get; set; }
        public string? Tipo { get; set; }       // HOTEL / CAR / FLIGHT / RESTAURANT / null = todos
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        public List<ResultItemViewModel> Items { get; set; } = new();

        public string? ErrorMessage { get; set; }
    }
}