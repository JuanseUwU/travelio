using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace TravelioREST.Habitaciones;

public class HabitacionGetResponse
{
    public string idHabitacion { get; set; }
    public string nombreHabitacion { get; set; }
    public string tipoHabitacion { get; set; }
    public string nombreHotel { get; set; }
    public string nombreCiudad { get; set; }
    public string nombrePais { get; set; }
    public int capacidad { get; set; }
    public decimal precioNormal { get; set; }
    public decimal precioActual { get; set; }
    public decimal precioVigente { get; set; }
    public string amenidades { get; set; }
    public string imagenes { get; set; }
    public _LinksGetRef _links { get; set; }
}

public class _LinksGetRef
{
    public SelfGetRef self { get; set; }
    public DisponibilidadGetRef disponibilidad { get; set; }
    public HoldGetRef hold { get; set; }
}

public class SelfGetRef
{
    public string href { get; set; }
    public string method { get; set; }
}

public class DisponibilidadGetRef
{
    public string href { get; set; }
    public string method { get; set; }
}

public class HoldGetRef
{
    public string href { get; set; }
    public string method { get; set; }
}


public static class HabitacionesGetter
{
    public static async Task<HabitacionGetResponse[]> GetHabitacionesAsync(
        string baseUri,
        DateTime? fechaInicio = null,
        DateTime? fechaFin = null,
        string? tipoHabitacion = null,
        int? capacidad = null,
        decimal? precioMin = null,
        decimal? precioMax = null)
    {
        var uriBuilder = new UriBuilder(baseUri);

        var query = HttpUtility.ParseQueryString(uriBuilder.Query);

        if (!string.IsNullOrEmpty(tipoHabitacion))
            query["tipo_habitacion"] = Uri.EscapeDataString(tipoHabitacion);

        if (fechaInicio.HasValue)
            query["date_from"] = fechaInicio.ToString();

        if (fechaFin.HasValue)
            query["date_to"] = fechaFin.ToString();

        if (capacidad.HasValue)
            query["capacidad"] = capacidad.ToString();

        if (precioMin.HasValue)
            query["precio_min"] = precioMin.ToString();

        if (precioMax.HasValue)
            query["precio_max"] = precioMax.ToString();

        uriBuilder.Query = query.ToString();

        var result = await Global.CachedHttpClient.GetFromJsonAsync<HabitacionGetResponse[]>(uriBuilder.ToString());
        return result ?? throw new InvalidOperationException();
    }
}
