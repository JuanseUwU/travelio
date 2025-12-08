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
        
        // URLs de facturas
        public string? FacturaTravelioUrl { get; set; }
        
        public List<OrderItemViewModel> Items { get; set; } = new();
    }

    public class OrderItemViewModel
    {
        public string Tipo { get; set; } = "";  // CAR, HOTEL, FLIGHT, etc.
        public string Titulo { get; set; } = "";
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public string? CodigoReserva { get; set; }
        public string? FacturaProveedorUrl { get; set; }
        
        public decimal Total => PrecioUnitario * Cantidad;
    }
}