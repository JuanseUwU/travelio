using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Mesas;

public class MesaFacturaRequest
{
    public string id_reserva { get; set; }
    public string correo { get; set; }
    public string nombre { get; set; }
    public string tipo_identificacion { get; set; } = "Cedula";
    public string identificacion { get; set; }
    public decimal valor { get; set; }
}

public class FacturaMesaResponse
{
    public string mensaje { get; set; }
    public string id_factura { get; set; }
    public string id_reserva { get; set; }
    public decimal subtotal { get; set; }
    public decimal iva { get; set; }
    public decimal total { get; set; }
    public string estado_factura { get; set; }
    public string estado_reserva { get; set; }
    public DateTime fecha_generacion { get; set; }
    public string uri_factura { get; set; }
    public _LinksFacturaMesa _links { get; set; }
}

public class _LinksFacturaMesa
{
    public SelfFacturaMesa self { get; set; }
    public VerfacturaFacturaMesa verFactura { get; set; }
    public PdfFacturaMesa pdf { get; set; }
}

public class SelfFacturaMesa
{
    public string href { get; set; }
    public string method { get; set; }
}

public class VerfacturaFacturaMesa
{
    public string href { get; set; }
    public string method { get; set; }
}

public class PdfFacturaMesa
{
    public string href { get; set; }
    public string method { get; set; }
}

public static class MesaFactura
{
    public static async Task<FacturaMesaResponse> CrearMesaFacturaAsync(string uri,
        string id_reserva,
        string correo,
        string nombre,
        string identificacion,
        decimal valor,
        string tipo_identificacion = "Cedula")
    {
        var request = new MesaFacturaRequest
        {
            id_reserva = id_reserva,
            correo = correo,
            nombre = nombre,
            tipo_identificacion = tipo_identificacion,
            identificacion = identificacion,
            valor = valor
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(uri, request);
        var mesaFactura = await response.Content.ReadFromJsonAsync<FacturaMesaResponse>();
        return mesaFactura ?? throw new InvalidOperationException();
    }
}
