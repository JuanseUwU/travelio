using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Aerolinea;

//public sealed class FacturaRequest
//{
//    public int ReservaId { get; set; }
//    public decimal Subtotal { get; set; }
//    public decimal Iva { get; set; }
//    public decimal Total { get; set; }
//    public required Cliente Cliente { get; set; }
//}

//public sealed class Cliente
//{
//    public required string Nombre { get; set; }
//    public required string Documento { get; set; }
//    public required string Correo { get; set; }
//}

//public class FacturaResponse
//{
//    public required string UriFactura { get; set; }
//    public bool Pagado { get; set; }
//}

public class FacturaRequest
{
    public string reservaId { get; set; }
    public decimal subtotal { get; set; }
    public decimal iva { get; set; }
    public decimal total { get; set; }
    public ClienteFacturaRequest cliente { get; set; }
    public string idTransaccion { get; set; }
}

public class ClienteFacturaRequest
{
    public string nombre { get; set; }
    public string tipoIdentificacion { get; set; }
    public string documento { get; set; }
    public string correo { get; set; }
}
public class FacturaResponse
{
    public bool success { get; set; }
    public string message { get; set; }
    public DataFacturaResponse data { get; set; }
    public string[] errors { get; set; }
    public DateTime timestamp { get; set; }
}

public class DataFacturaResponse
{
    public bool success { get; set; }
    public string uriFactura { get; set; }
    public string numeroFactura { get; set; }
    public string mensaje { get; set; }
}

public static class InvoiceGenerator
{
    public static async Task<FacturaResponse> GenerateInvoiceAsync(string uri,
        string idReserva,
        decimal subtotal,
        decimal iva,
        decimal total,
        (string nombre, string tipoDocumento, string documento, string correo) cliente,
        string idTransaccionBanco = "")
    {
        var request = new FacturaRequest
        {
            reservaId = idReserva,
            subtotal = subtotal,
            iva = iva,
            total = total,
            cliente = new ClienteFacturaRequest
            {
                nombre = cliente.nombre,
                tipoIdentificacion = cliente.tipoDocumento,
                documento = cliente.documento,
                correo = cliente.correo
            },
            idTransaccion = idTransaccionBanco
        };

        var response = await Global.CachedHttpClient.PostAsJsonAsync(uri, request);
        response.EnsureSuccessStatusCode();
        var facturaResponse = await response.Content.ReadFromJsonAsync<FacturaResponse>();
        return facturaResponse ?? throw new InvalidOperationException("El servicio de facturación no está disponible o envió una respuesta no válida");
    }
}
