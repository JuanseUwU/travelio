using System;
using System.Collections.Generic;

namespace BookingMvcDotNet.Models
{
    // Modelo para el Usuario (Base de datos simulada)
    public class UserViewModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public bool IsAdmin { get; set; } = false;

        // Historial de compras del usuario
        public List<OrderViewModel> Orders { get; set; } = new List<OrderViewModel>();
    }

}