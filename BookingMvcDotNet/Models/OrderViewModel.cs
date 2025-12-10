using System;
using System.Collections.Generic;

namespace BookingMvcDotNet.Models
{
    public class OrderViewModel
    {
        public string OrderId { get; set; } = "";
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "Pagado";
        public string? FacturaTravelioUrl { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public string Tipo { get; set; } = "";
        public string Titulo { get; set; } = "";
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public string? CodigoReserva { get; set; }
        public string? FacturaProveedorUrl { get; set; }
        
        // Para cancelacion
        public int ReservaId { get; set; }
        public int ServicioId { get; set; }
        public bool Activa { get; set; } = true;
        public bool PuedeCancelar { get; set; } = false;
        
        public decimal Total => PrecioUnitario * Cantidad;
    }
}