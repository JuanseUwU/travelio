using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;

namespace TravelioREST.Autos;


//public class AutoReservaRequest
//{
//    public string id_auto { get; set; }
//    public string id_hold { get; set; }
//    public string nombre { get; set; }
//    public string apellido { get; set; }
//    public string tipo_identificacion { get; set; }
//    public string identificacion { get; set; }
//    public string correo { get; set; }
//    public DateTime fecha_inicio { get; set; }
//    public DateTime fecha_fin { get; set; }
//}

public class AutoReservaRequest
{
    public string IdAuto { get; set; }
    public string IdHold { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Correo { get; set; }
    public string Identificacion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string TipoIdentificacion { get; set; }
}

/*
public sealed class AutoReservaRequest
{
    public required string id_auto { get; set; }
    public required string id_hold { get; set; }
    public required string nombre { get; set; }
    public required string apellido { get; set; }
    public required string tipo_identificacion { get; set; }
    public required string identificacion { get; set; }
    public required string correo { get; set; }
    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_fin { get; set; }
}
*/

/*
 {
  "datos": {
    "mensaje": "Reserva creada correctamente",
    "id_reserva": 19,
    "id_hold": "14",
    "id_auto": "21",
    "nombre_titular": "Juan Pérez",
    "tipo_identificacion": "ID",
    "identificacion": "12",
    "correo": "jperez@correo.com",
    "vehiculo": "Mazda CX-5",
    "fecha_inicio": "2025-11-25T15:04:06.751Z",
    "fecha_fin": "2025-11-25T18:04:06.751Z",
    "total": 9.375,
    "estado": "Confirmada",
    "fecha_reserva": "2025-11-24T16:18:30.8342272+01:00"
  },
  "_links": [
    {
      "rel": "self",
      "href": "http://cuencautosinte.runasp.net/api/ReservaIntegracion",
      "method": "POST"
    },
    {
      "rel": "detalle_reserva",
      "href": "http://cuencautosinte.runasp.net/api/Reserva/19",
      "method": "GET"
    },
    {
      "rel": "detalle_hold",
      "href": "http://cuencautosinte.runasp.net/api/IntegracionHold?id_hold=14",
      "method": "GET"
    },
    {
      "rel": "detalle_auto",
      "href": "http://cuencautosinte.runasp.net/api/Autos?id_auto=21",
      "method": "GET"
    },
    {
      "rel": "emitir_factura",
      "href": "http://cuencautosinte.runasp.net/api/EmitirFactura",
      "method": "POST"
    }
  ]
}
 */


/*
{
  "datos": {
    "mensaje": "Reserva creada correctamente",
    "id_reserva": 75,
    "id_hold": "31",
    "id_auto": "22",
    "nombre_titular": "string string",
    "identificacion": null,
    "correo": "string",
    "vehiculo": "Kia Rio WS2E81",
    "fecha_inicio": "2025-12-03T14:16:18.442Z",
    "fecha_fin": "2025-12-04T14:16:18.442Z",
    "total": 35,
    "estado": "Confirmada"
  },
  "_links": [
    {
      "rel": "self",
      "href": "http://cuencautosinte.runasp.net/api/ReservaIntegracion",
      "method": "POST"
    },
    {
      "rel": "detalle_reserva",
      "href": "http://cuencautosinte.runasp.net/api/Reserva/75",
      "method": "GET"
    },
    {
      "rel": "detalle_hold",
      "href": "http://cuencautosinte.runasp.net/api/IntegracionHold?id_hold=31",
      "method": "GET"
    },
    {
      "rel": "detalle_auto",
      "href": "http://cuencautosinte.runasp.net/api/Autos?id_auto=22",
      "method": "GET"
    },
    {
      "rel": "emitir_factura",
      "href": "http://cuencautosinte.runasp.net/api/EmitirFactura",
      "method": "POST"
    }
  ]
}
 */


public class ReservaResponse
{
    public DatosReserva datos { get; set; }
    public _LinksReserva[] _links { get; set; }
}

public class DatosReserva
{
    public string mensaje { get; set; }
    public int id_reserva { get; set; }
    public string id_hold { get; set; }
    public string id_auto { get; set; }
    public string nombre_titular { get; set; }
    public string identificacion { get; set; }
    public string correo { get; set; }
    public string vehiculo { get; set; }
    public DateTime fecha_inicio { get; set; }
    public DateTime fecha_fin { get; set; }
    public decimal total { get; set; }
    public string estado { get; set; }
}

public class _LinksReserva
{
    public string rel { get; set; }
    public string href { get; set; }
    public string method { get; set; }
}

public static class AutosReservaCreador
{
    public static async Task<ReservaResponse> CrearReservaAsync(string url,
        string idAuto,
        string idHold,
        string nombre,
        string apellido,
        string tipoIdentificacion,
        string identificacion,
        string correo,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        var request = new AutoReservaRequest()
        {
            IdAuto = idAuto,
            IdHold = idHold,
            Nombre = nombre,
            Apellido = apellido,
            TipoIdentificacion = tipoIdentificacion,
            Identificacion = identificacion,
            Correo = correo,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin
        };
        var response = await Global.CachedHttpClient.PostAsJsonAsync(url, request);
        Debug.WriteLine($"{response.StatusCode}");
        var reserva = await response.Content.ReadFromJsonAsync<ReservaResponse>();
        return reserva ?? throw new InvalidOperationException("No se pudo crear la reserva.");
    }
}
