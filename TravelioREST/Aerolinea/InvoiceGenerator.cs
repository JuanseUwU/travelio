using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;


public sealed class FacturaRequest
{
    public int ReservaId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public required Cliente Cliente { get; set; }
}

public sealed class Cliente
{
    public required string Nombre { get; set; }
    public required string Documento { get; set; }
    public required string Correo { get; set; }
}

public class FacturaResponse
{
    public required string UriFactura { get; set; }
    public bool Pagado { get; set; }
}

public static class InvoiceGenerator
{
    public static async Task<FacturaResponse> GenerateInvoiceAsync(string url,
        int reservaId,
        decimal subtotal,
        decimal iva,
        decimal total,
        (string nombre, string documento, string correo) cliente)
    {
        throw new NotImplementedException("La generación de facturas ha sido deshabilitada temporalmente.");

        //var facturaRequest = new FacturaRequest
        //{
        //    ReservaId = reservaId,
        //    Subtotal = subtotal,
        //    Iva = iva,
        //    Total = total,
        //    Cliente = new Cliente
        //    {
        //        Nombre = cliente.nombre,
        //        Documento = cliente.documento,
        //        Correo = cliente.correo
        //    }
        //};
        //var response = await Global.CachedHttpClient.PostAsJsonAsync(url, facturaRequest);
        //var facturaResponse = await response.Content.ReadFromJsonAsync<FacturaResponse>();
        //return facturaResponse ?? throw new InvalidOperationException();
    }
}
