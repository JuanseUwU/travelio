using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

//public class MesaFacturaConsultaResponse
//{
//    public string numero_mesa { get; set; }
//    public string correo { get; set; }
//    public DateTime fecha { get; set; }
//    public int numero_personas { get; set; }
//    public string? categoria { get; set; }
//    public string tipo { get; set; }
//    public int capacidad { get; set; }
//    public string nombre { get; set; }
//    public string apellido { get; set; }
//    public decimal valor_pagado { get; set; }
//    public string uri_factura { get; set; }
//}

public class MesaFacturaConsultaResponse
{
    public string mensaje { get; set; }
    public string idReserva { get; set; }
    public string idMesa { get; set; }
    public string nombreCliente { get; set; }
    public string apellidoCliente { get; set; }
    public string correo { get; set; }
    public string tipoIdentificacion { get; set; }
    public string identificacion { get; set; }
    public DateTime fecha { get; set; }
    public int numeroPersonas { get; set; }
    public decimal valorPagado { get; set; }
    public string uriFactura { get; set; }
}


public static class MesaReservaConsulta
{
    public static async Task<MesaFacturaConsultaResponse> ConsultarMesaReservaAsync(string uri,
        string id_reserva)
    {
        var fullUri = $"{uri}/{id_reserva}";
        var response = await Global.CachedHttpClient.GetAsync(fullUri);
        response.EnsureSuccessStatusCode();
        var mesaReservaConsulta = await response.Content.ReadFromJsonAsync<MesaFacturaConsultaResponse>();
        return mesaReservaConsulta ?? throw new InvalidOperationException();
    }
}
