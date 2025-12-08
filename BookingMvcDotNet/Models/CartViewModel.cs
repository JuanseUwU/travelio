using System.Collections.Generic;
using System.Linq;

namespace BookingMvcDotNet.Models
{
    public class CartItemViewModel
    {
        public string Tipo { get; set; } = string.Empty;  // CAR, HOTEL, FLIGHT, RESTAURANT, PACKAGE
        public string Titulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public int Cantidad { get; set; } = 1;

        // --- PRECIOS ---
        public decimal PrecioOriginal { get; set; }   // Precio base (ej. 450)
        public decimal PrecioFinal { get; set; }      // Precio con descuento (ej. 405)
        public decimal PrecioUnitario { get; set; }   // En la UI usamos este como unitario

        public string UnidadPrecio { get; set; } = string.Empty;

        // --- INFORMACIÓN PARA RESERVA (SOAP/REST) ---
        public int ServicioId { get; set; }           // ID del servicio en TravelioDb
        public string IdProducto { get; set; } = string.Empty;  // IdAuto, IdHabitacion, IdVuelo, etc.
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? ImagenUrl { get; set; }
        public int? NumeroPersonas { get; set; }  // Número de huéspedes/pasajeros

        // Lógica de descuento
        public bool TieneDescuento => PrecioFinal < PrecioOriginal;

        public int PorcentajeDescuento =>
            PrecioOriginal > 0
                ? (int)((1 - (PrecioFinal / PrecioOriginal)) * 100)
                : 0;

        // El total se calcula con el precio final (el que paga el usuario)
        public decimal TotalItem => Cantidad * PrecioFinal;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();

        public decimal Subtotal => Items.Sum(i => i.TotalItem);

        // IVA desde configuración global (por defecto 15%)
        public int IvaPercent { get; set; } = 15;

        public decimal MontoIva => Subtotal * (IvaPercent / 100m);

        public decimal Total => Subtotal + MontoIva;

        public bool EstaVacio => Items.Count == 0;
    }
}
