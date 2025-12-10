using BookingMvcDotNet.Models;
using Microsoft.EntityFrameworkCore;
using TravelioBankConnector;
using TravelioDatabaseConnector.Data;
using TravelioDatabaseConnector.Enums;
using TravelioDatabaseConnector.Models;
using DbReserva = TravelioDatabaseConnector.Models.Reserva;
using AutoConnector = TravelioAPIConnector.Autos.Connector;
using HotelConnector = TravelioAPIConnector.Habitaciones.Connector;
using VueloConnector = TravelioAPIConnector.Aerolinea.Connector;
using MesaConnector = TravelioAPIConnector.Mesas.Connector;
using PaqueteConnector = TravelioAPIConnector.Paquetes.Connector;

namespace BookingMvcDotNet.Services;

/// <summary>
/// Implementacion del servicio de checkout que integra:
/// - API del Banco para cobros
/// - Servicios REST/SOAP de proveedores para reservas (REST primero, fallback a SOAP)
/// - Base de datos TravelioDb para registro
/// </summary>
public class CheckoutService(TravelioDbContext dbContext, ILogger<CheckoutService> logger) : ICheckoutService
{
    private const decimal COMISION_TRAVELIO = 0.10m;

    public async Task<CheckoutResult> ProcesarCheckoutAsync(
        int clienteId, 
        int cuentaBancariaCliente, 
        List<CartItemViewModel> items, 
        DatosFacturacion datosFacturacion)
    {
        var resultado = new CheckoutResult();

        try
        {
            var cliente = await dbContext.Clientes.FindAsync(clienteId);
            if (cliente == null)
            {
                resultado.Mensaje = "Cliente no encontrado.";
                return resultado;
            }

            decimal totalCarrito = items.Sum(i => i.PrecioFinal * i.Cantidad);
            decimal iva = totalCarrito * 0.12m;
            decimal totalConIva = totalCarrito + iva;

            logger.LogInformation("Procesando checkout para cliente {ClienteId}. Total: ${Total}", clienteId, totalConIva);

            var cobroExitoso = await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: TransferirClass.cuentaDefaultTravelio,
                monto: totalConIva,
                cuentaOrigen: cuentaBancariaCliente
            );

            if (!cobroExitoso)
            {
                logger.LogWarning("Fallo el cobro al cliente {ClienteId}", clienteId);
                resultado.Mensaje = "No se pudo procesar el pago. Verifica tu saldo o cuenta bancaria.";
                return resultado;
            }

            logger.LogInformation("Cobro exitoso de ${Monto} al cliente {ClienteId}", totalConIva, clienteId);

            var compra = new Compra
            {
                ClienteId = clienteId,
                FechaCompra = DateTime.UtcNow,
                ValorPagado = totalConIva
            };
            dbContext.Compras.Add(compra);
            await dbContext.SaveChangesAsync();

            resultado.CompraId = compra.Id;
            resultado.TotalPagado = totalConIva;

            foreach (var item in items)
            {
                var reservaResult = new ReservaResult { Tipo = item.Tipo, Titulo = item.Titulo };

                try
                {
                    switch (item.Tipo)
                    {
                        case "CAR":
                            await ProcesarReservaAutoAsync(item, cliente, datosFacturacion, compra, reservaResult);
                            break;
                        case "HOTEL":
                            await ProcesarReservaHotelAsync(item, cliente, datosFacturacion, compra, reservaResult);
                            break;
                        case "FLIGHT":
                            await ProcesarReservaVueloAsync(item, cliente, datosFacturacion, compra, reservaResult);
                            break;
                        case "RESTAURANT":
                            await ProcesarReservaMesaAsync(item, cliente, datosFacturacion, compra, reservaResult);
                            break;
                        case "PACKAGE":
                            await ProcesarReservaPaqueteAsync(item, cliente, datosFacturacion, compra, reservaResult);
                            break;
                        default:
                            reservaResult.Error = $"Tipo de servicio desconocido: {item.Tipo}";
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error procesando reserva para {Titulo}", item.Titulo);
                    reservaResult.Error = "Error al procesar la reserva";
                }

                resultado.Reservas.Add(reservaResult);
            }

            var todasExitosas = resultado.Reservas.All(r => r.Exitoso);
            var algunaExitosa = resultado.Reservas.Any(r => r.Exitoso);

            if (todasExitosas)
            {
                resultado.Exitoso = true;
                resultado.Mensaje = "Compra realizada con exito! Tus reservas han sido confirmadas.";
            }
            else if (algunaExitosa)
            {
                resultado.Exitoso = true;
                resultado.Mensaje = "Compra parcialmente exitosa. Algunas reservas no pudieron procesarse.";
            }
            else
            {
                resultado.Mensaje = "No se pudieron procesar las reservas. Se intentara reembolsar el pago.";
                await TransferirClass.RealizarTransferenciaAsync(
                    cuentaDestino: cuentaBancariaCliente,
                    monto: totalConIva,
                    cuentaOrigen: TransferirClass.cuentaDefaultTravelio
                );
            }

            await dbContext.SaveChangesAsync();
            return resultado;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error en checkout para cliente {ClienteId}", clienteId);
            resultado.Mensaje = "Error inesperado al procesar la compra.";
            return resultado;
        }
    }

    private async Task<(DetalleServicio? rest, DetalleServicio? soap, Servicio servicio)> ObtenerDetallesServicioAsync(int servicioId)
    {
        var detalles = await dbContext.DetallesServicio
            .Include(d => d.Servicio)
            .Where(d => d.ServicioId == servicioId)
            .ToListAsync();

        var rest = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Rest);
        var soap = detalles.FirstOrDefault(d => d.TipoProtocolo == TipoProtocolo.Soap);
        var servicio = detalles.FirstOrDefault()?.Servicio;

        return (rest, soap, servicio!);
    }

    private async Task ProcesarReservaAutoAsync(
        CartItemViewModel item, Cliente cliente, DatosFacturacion datosFacturacion,
        Compra compra, ReservaResult reservaResult)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId);

        if (servicio == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        if (!item.FechaInicio.HasValue || !item.FechaFin.HasValue)
        {
            reservaResult.Error = "Fechas de reserva no validas";
            return;
        }

        var fechaInicio = item.FechaInicio.Value;
        var fechaFin = item.FechaFin.Value;
        bool usandoRest = false;

        logger.LogInformation("Procesando reserva de auto {IdAuto} en {Servicio}", item.IdProducto, servicio.Nombre);

        // Crear cliente externo (REST primero, fallback SOAP)
        await CrearClienteExternoAutoAsync(detalleRest, detalleSoap, cliente);

        // 1. Crear prerreserva (REST primero, fallback SOAP)
        string holdId = "";
        DateTime holdExpira = DateTime.MinValue;
        Exception? ultimoError = null;

        if (detalleRest != null)
        {
            try
            {
                var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva auto (REST): {Uri}", uri);
                (holdId, holdExpira) = await AutoConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin);
                usandoRest = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "REST fallo para prerreserva auto");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap != null)
        {
            try
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva auto (SOAP): {Uri}", uri);
                (holdId, holdExpira) = await AutoConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin, forceSoap: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SOAP tambien fallo para prerreserva auto");
                ultimoError = ex;
            }
        }

        // Verificar si se pudo crear la prerreserva
        if (string.IsNullOrEmpty(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        logger.LogInformation("Prerreserva auto creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        // 2. Crear reserva
        int reservaId = 0;
        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        
        reservaId = await AutoConnector.CrearReservaAsync(
            uriReserva, item.IdProducto, holdId, cliente.Nombre, cliente.Apellido,
            cliente.TipoIdentificacion, cliente.DocumentoIdentidad, cliente.CorreoElectronico,
            fechaInicio, fechaFin, forceSoap: !usandoRest);

        logger.LogInformation("Reserva auto creada: {ReservaId}", reservaId);
        reservaResult.CodigoReserva = reservaId.ToString();

        // 3. Generar factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            decimal subtotal = item.PrecioFinal;
            decimal iva = subtotal * 0.12m;
            decimal total = subtotal + iva;

            var facturaUrl = await AutoConnector.GenerarFacturaAsync(
                uriFactura, reservaId, subtotal, iva, total,
                (datosFacturacion.NombreCompleto, datosFacturacion.TipoDocumento, datosFacturacion.NumeroDocumento, datosFacturacion.Correo),
                forceSoap: !usandoRest);

            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura auto");
        }

        // 4. Pagar al proveedor
        await PagarProveedorAsync(servicio, item.PrecioFinal);

        // 5. Registrar en DB
        await RegistrarReservaEnDbAsync(item.ServicioId, reservaId.ToString(), reservaResult.FacturaProveedorUrl, compra);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoAutoAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente)
    {
        try
        {
            if (rest != null && !string.IsNullOrEmpty(rest.RegistrarClienteEndpoint))
            {
                try
                {
                    var uri = $"{rest.UriBase}{rest.RegistrarClienteEndpoint}";
                    await AutoConnector.CrearClienteExternoAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico);
                    return;
                }
                catch { }
            }

            if (soap != null && !string.IsNullOrEmpty(soap.RegistrarClienteEndpoint))
            {
                var uri = $"{soap.UriBase}{soap.RegistrarClienteEndpoint}";
                await AutoConnector.CrearClienteExternoAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, forceSoap: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo auto");
        }
    }

    private async Task ProcesarReservaHotelAsync(
        CartItemViewModel item, Cliente cliente, DatosFacturacion datosFacturacion,
        Compra compra, ReservaResult reservaResult)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId);

        if (servicio == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        if (!item.FechaInicio.HasValue || !item.FechaFin.HasValue)
        {
            reservaResult.Error = "Fechas de reserva no validas";
            return;
        }

        var fechaInicio = item.FechaInicio.Value;
        var fechaFin = item.FechaFin.Value;
        var numeroHuespedes = item.NumeroPersonas ?? 2;
        bool usandoRest = false;

        logger.LogInformation("Procesando reserva hotel {IdHabitacion} en {Servicio}", item.IdProducto, servicio.Nombre);

        // Crear usuario externo
        await CrearClienteExternoHotelAsync(detalleRest, detalleSoap, cliente);

        // 1. Crear prerreserva
        string holdId = "";
        Exception? ultimoError = null;

        if (detalleRest != null)
        {
            try
            {
                var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva hotel (REST): {Uri}", uri);
                holdId = await HotelConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin, numeroHuespedes, 300, item.PrecioUnitario);
                usandoRest = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "REST fallo para prerreserva hotel");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap != null)
        {
            try
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva hotel (SOAP): {Uri}", uri);
                holdId = await HotelConnector.CrearPrerreservaAsync(uri, item.IdProducto, fechaInicio, fechaFin, numeroHuespedes, 300, item.PrecioUnitario, forceSoap: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SOAP tambien fallo para prerreserva hotel");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrEmpty(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        logger.LogInformation("Prerreserva hotel creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        // 2. Crear reserva
        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        
        var reservaId = await HotelConnector.CrearReservaAsync(
            uriReserva, item.IdProducto, holdId, cliente.Nombre, cliente.Apellido,
            cliente.CorreoElectronico, cliente.TipoIdentificacion, cliente.DocumentoIdentidad,
            fechaInicio, fechaFin, numeroHuespedes, forceSoap: !usandoRest);

        logger.LogInformation("Reserva hotel creada: {ReservaId}", reservaId);
        reservaResult.CodigoReserva = reservaId.ToString();

        // 3. Generar factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            var facturaUrl = await HotelConnector.EmitirFacturaAsync(
                uriFactura, reservaId,
                datosFacturacion.NombreCompleto.Split(' ').FirstOrDefault() ?? cliente.Nombre,
                datosFacturacion.NombreCompleto.Split(' ').Skip(1).FirstOrDefault() ?? cliente.Apellido,
                datosFacturacion.TipoDocumento, datosFacturacion.NumeroDocumento, datosFacturacion.Correo,
                forceSoap: !usandoRest);

            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura hotel");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal);
        await RegistrarReservaEnDbAsync(item.ServicioId, reservaId.ToString(), reservaResult.FacturaProveedorUrl, compra);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoHotelAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente)
    {
        try
        {
            if (rest != null && !string.IsNullOrEmpty(rest.RegistrarClienteEndpoint))
            {
                try
                {
                    var uri = $"{rest.UriBase}{rest.RegistrarClienteEndpoint}";
                    await HotelConnector.CrearUsuarioExternoAsync(uri, cliente.CorreoElectronico, cliente.Nombre, cliente.Apellido);
                    return;
                }
                catch { }
            }

            if (soap != null && !string.IsNullOrEmpty(soap.RegistrarClienteEndpoint))
            {
                var uri = $"{soap.UriBase}{soap.RegistrarClienteEndpoint}";
                await HotelConnector.CrearUsuarioExternoAsync(uri, cliente.CorreoElectronico, cliente.Nombre, cliente.Apellido, forceSoap: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo hotel");
        }
    }

    private async Task ProcesarReservaVueloAsync(
        CartItemViewModel item, Cliente cliente, DatosFacturacion datosFacturacion,
        Compra compra, ReservaResult reservaResult)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId);

        if (servicio == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        bool usandoRest = false;

        logger.LogInformation("Procesando reserva vuelo {IdVuelo} en {Servicio}", item.IdProducto, servicio.Nombre);

        var pasajeros = new (string, string, string, string, DateTime)[]
        {
            (cliente.Nombre, cliente.Apellido, cliente.TipoIdentificacion, cliente.DocumentoIdentidad, DateTime.Now.AddYears(-30))
        };

        // Crear usuario externo
        await CrearClienteExternoVueloAsync(detalleRest, detalleSoap, cliente);

        // 1. Crear prerreserva
        string holdId = "";
        Exception? ultimoError = null;

        if (detalleRest != null)
        {
            try
            {
                var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva vuelo (REST): {Uri}", uri);
                (holdId, _) = await VueloConnector.CrearPrerreservaVueloAsync(uri, item.IdProducto, pasajeros, 300);
                usandoRest = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "REST fallo para prerreserva vuelo");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap != null)
        {
            try
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva vuelo (SOAP): {Uri}", uri);
                (holdId, _) = await VueloConnector.CrearPrerreservaVueloAsync(uri, item.IdProducto, pasajeros, 300, forceSoap: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SOAP tambien fallo para prerreserva vuelo");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrEmpty(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        logger.LogInformation("Prerreserva vuelo creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        // 2. Crear reserva
        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        
        var (idReserva, codigoReserva, _) = await VueloConnector.CrearReservaAsync(
            uriReserva, item.IdProducto, holdId, cliente.CorreoElectronico, pasajeros, forceSoap: !usandoRest);

        logger.LogInformation("Reserva vuelo creada: {IdReserva}", idReserva);
        reservaResult.CodigoReserva = codigoReserva;

        // 3. Generar factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            decimal subtotal = item.PrecioFinal;
            decimal iva = subtotal * 0.12m;
            decimal total = subtotal + iva;

            var facturaUrl = await VueloConnector.GenerarFacturaAsync(
                uriFactura, idReserva, subtotal, iva, total,
                (datosFacturacion.NombreCompleto, datosFacturacion.TipoDocumento, datosFacturacion.NumeroDocumento, datosFacturacion.Correo),
                forceSoap: !usandoRest);

            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura vuelo");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal);
        await RegistrarReservaEnDbAsync(item.ServicioId, codigoReserva, reservaResult.FacturaProveedorUrl, compra);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoVueloAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente)
    {
        try
        {
            if (rest != null && !string.IsNullOrEmpty(rest.RegistrarClienteEndpoint))
            {
                try
                {
                    var uri = $"{rest.UriBase}{rest.RegistrarClienteEndpoint}";
                    await VueloConnector.CrearClienteExternoAsync(uri, cliente.CorreoElectronico, cliente.Nombre, cliente.Apellido, DateTime.Now.AddYears(-30), cliente.TipoIdentificacion, cliente.DocumentoIdentidad);
                    return;
                }
                catch { }
            }

            if (soap != null && !string.IsNullOrEmpty(soap.RegistrarClienteEndpoint))
            {
                var uri = $"{soap.UriBase}{soap.RegistrarClienteEndpoint}";
                await VueloConnector.CrearClienteExternoAsync(uri, cliente.CorreoElectronico, cliente.Nombre, cliente.Apellido, DateTime.Now.AddYears(-30), cliente.TipoIdentificacion, cliente.DocumentoIdentidad, forceSoap: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo vuelo");
        }
    }

    private async Task ProcesarReservaMesaAsync(
        CartItemViewModel item, Cliente cliente, DatosFacturacion datosFacturacion,
        Compra compra, ReservaResult reservaResult)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId);

        if (servicio == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var fecha = item.FechaInicio ?? DateTime.Today;
        var personas = item.NumeroPersonas ?? 2;
        var idMesa = int.Parse(item.IdProducto);
        bool usandoRest = false;

        logger.LogInformation("Procesando reserva mesa {IdMesa} en {Servicio}", idMesa, servicio.Nombre);

        // Crear usuario externo
        await CrearClienteExternoMesaAsync(detalleRest, detalleSoap, cliente);

        // 1. Crear prerreserva
        string holdId = "";
        Exception? ultimoError = null;

        if (detalleRest != null)
        {
            try
            {
                var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva mesa (REST): {Uri}", uri);
                (holdId, _) = await MesaConnector.CrearPreReservaAsync(uri, idMesa, fecha, personas, 300);
                usandoRest = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "REST fallo para prerreserva mesa");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap != null)
        {
            try
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando prerreserva mesa (SOAP): {Uri}", uri);
                (holdId, _) = await MesaConnector.CrearPreReservaAsync(uri, idMesa, fecha, personas, 300, forceSoap: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SOAP tambien fallo para prerreserva mesa");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrEmpty(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        logger.LogInformation("Prerreserva mesa creada ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        // 2. Confirmar reserva
        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";

        var reserva = await MesaConnector.ConfirmarReservaAsync(
            uriReserva, idMesa, holdId, cliente.Nombre, cliente.Apellido,
            cliente.CorreoElectronico, cliente.TipoIdentificacion, cliente.DocumentoIdentidad,
            fecha, personas, forceSoap: !usandoRest);

        logger.LogInformation("Reserva mesa confirmada: {IdReserva}", reserva.IdReserva);
        reservaResult.CodigoReserva = reserva.IdReserva;

        // 3. Generar factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            var facturaUrl = await MesaConnector.GenerarFacturaAsync(
                uriFactura, reserva.IdReserva, datosFacturacion.Correo, datosFacturacion.NombreCompleto,
                datosFacturacion.TipoDocumento, datosFacturacion.NumeroDocumento, item.PrecioFinal, forceSoap: !usandoRest);

            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura mesa");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal);
        await RegistrarReservaEnDbAsync(item.ServicioId, reserva.IdReserva, reservaResult.FacturaProveedorUrl, compra);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoMesaAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente)
    {
        try
        {
            if (rest != null && !string.IsNullOrEmpty(rest.RegistrarClienteEndpoint))
            {
                try
                {
                    var uri = $"{rest.UriBase}{rest.RegistrarClienteEndpoint}";
                    await MesaConnector.CrearUsuarioAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, cliente.TipoIdentificacion, cliente.DocumentoIdentidad);
                    return;
                }
                catch { }
            }

            if (soap != null && !string.IsNullOrEmpty(soap.RegistrarClienteEndpoint))
            {
                var uri = $"{soap.UriBase}{soap.RegistrarClienteEndpoint}";
                await MesaConnector.CrearUsuarioAsync(uri, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, cliente.TipoIdentificacion, cliente.DocumentoIdentidad, forceSoap: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo mesa");
        }
    }

    private async Task ProcesarReservaPaqueteAsync(
        CartItemViewModel item, Cliente cliente, DatosFacturacion datosFacturacion,
        Compra compra, ReservaResult reservaResult)
    {
        var (detalleRest, detalleSoap, servicio) = await ObtenerDetallesServicioAsync(item.ServicioId);

        if (servicio == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var fechaInicio = item.FechaInicio ?? DateTime.Today;
        var personas = item.NumeroPersonas ?? 1;
        var bookingUserId = cliente.Id.ToString();
        bool usandoRest = false;

        logger.LogInformation("Procesando reserva paquete {IdPaquete} en {Servicio}", item.IdProducto, servicio.Nombre);

        // Crear usuario externo
        await CrearClienteExternoPaqueteAsync(detalleRest, detalleSoap, cliente, bookingUserId);

        // 1. Crear hold
        string holdId = "";
        Exception? ultimoError = null;

        if (detalleRest != null)
        {
            try
            {
                var uri = $"{detalleRest.UriBase}{detalleRest.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando hold paquete (REST): {Uri}", uri);
                (holdId, _) = await PaqueteConnector.CrearHoldAsync(uri, item.IdProducto, bookingUserId, fechaInicio, personas, 300);
                usandoRest = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "REST fallo para hold paquete");
                ultimoError = ex;
            }
        }

        if (!usandoRest && detalleSoap != null)
        {
            try
            {
                var uri = $"{detalleSoap.UriBase}{detalleSoap.CrearPrerreservaEndpoint}";
                logger.LogInformation("Creando hold paquete (SOAP): {Uri}", uri);
                (holdId, _) = await PaqueteConnector.CrearHoldAsync(uri, item.IdProducto, bookingUserId, fechaInicio, personas, 300, forceSoap: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SOAP tambien fallo para hold paquete");
                ultimoError = ex;
            }
        }

        if (string.IsNullOrEmpty(holdId))
        {
            reservaResult.Error = $"No se pudo crear prerreserva: {ultimoError?.Message ?? "Servicio no disponible"}";
            return;
        }

        logger.LogInformation("Hold paquete creado ({Protocolo}): {HoldId}", usandoRest ? "REST" : "SOAP", holdId);

        // 2. Crear reserva
        var turistas = new (string, string, DateTime?, string, string)[]
        {
            (cliente.Nombre, cliente.Apellido, null, cliente.TipoIdentificacion, cliente.DocumentoIdentidad)
        };

        var detalle = usandoRest ? detalleRest! : detalleSoap!;
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";

        var reserva = await PaqueteConnector.CrearReservaAsync(
            uriReserva, item.IdProducto, holdId, bookingUserId, "TransferenciaBancaria", turistas, forceSoap: !usandoRest);

        logger.LogInformation("Reserva paquete creada: {IdReserva}", reserva.IdReserva);
        reservaResult.CodigoReserva = reserva.CodigoReserva;

        // 3. Emitir factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            decimal subtotal = item.PrecioFinal;
            decimal iva = subtotal * 0.12m;
            decimal total = subtotal + iva;

            var facturaUrl = await PaqueteConnector.EmitirFacturaAsync(uriFactura, reserva.IdReserva, subtotal, iva, total, forceSoap: !usandoRest);
            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo emitir factura paquete");
        }

        await PagarProveedorAsync(servicio, item.PrecioFinal);
        await RegistrarReservaEnDbAsync(item.ServicioId, reserva.CodigoReserva, reservaResult.FacturaProveedorUrl, compra);

        reservaResult.Exitoso = true;
    }

    private async Task CrearClienteExternoPaqueteAsync(DetalleServicio? rest, DetalleServicio? soap, Cliente cliente, string bookingUserId)
    {
        try
        {
            if (rest != null && !string.IsNullOrEmpty(rest.RegistrarClienteEndpoint))
            {
                try
                {
                    var uri = $"{rest.UriBase}{rest.RegistrarClienteEndpoint}";
                    await PaqueteConnector.CrearUsuarioExternoAsync(uri, bookingUserId, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico);
                    return;
                }
                catch { }
            }

            if (soap != null && !string.IsNullOrEmpty(soap.RegistrarClienteEndpoint))
            {
                var uri = $"{soap.UriBase}{soap.RegistrarClienteEndpoint}";
                await PaqueteConnector.CrearUsuarioExternoAsync(uri, bookingUserId, cliente.Nombre, cliente.Apellido, cliente.CorreoElectronico, forceSoap: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo paquete");
        }
    }

    private async Task PagarProveedorAsync(Servicio servicio, decimal precioFinal)
    {
        if (int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            var montoProveedor = precioFinal * (1 - COMISION_TRAVELIO);
            var pagoExitoso = await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: cuentaProveedor,
                monto: montoProveedor,
                cuentaOrigen: TransferirClass.cuentaDefaultTravelio
            );

            if (pagoExitoso)
                logger.LogInformation("Pago de ${Monto} realizado a {Servicio}", montoProveedor, servicio.Nombre);
            else
                logger.LogWarning("No se pudo pagar a {Servicio}", servicio.Nombre);
        }
    }

    private async Task RegistrarReservaEnDbAsync(int servicioId, string codigoReserva, string? facturaUrl, Compra compra)
    {
        var reservaDb = new DbReserva
        {
            ServicioId = servicioId,
            CodigoReserva = codigoReserva,
            FacturaUrl = facturaUrl
        };
        dbContext.Reservas.Add(reservaDb);
        await dbContext.SaveChangesAsync();

        dbContext.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });
    }

    public async Task<CancelacionResult> CancelarReservaAsync(int reservaId, int clienteId, int cuentaBancariaCliente)
    {
        var resultado = new CancelacionResult();

        try
        {
            // Obtener la reserva con su servicio
            var reserva = await dbContext.Reservas
                .Include(r => r.Servicio)
                .FirstOrDefaultAsync(r => r.Id == reservaId);

            if (reserva == null)
            {
                resultado.Mensaje = "Reserva no encontrada.";
                return resultado;
            }

            // Verificar que la reserva pertenece al cliente
            var reservaCompra = await dbContext.ReservasCompra
                .Include(rc => rc.Compra)
                .FirstOrDefaultAsync(rc => rc.ReservaId == reservaId && rc.Compra.ClienteId == clienteId);

            if (reservaCompra == null)
            {
                resultado.Mensaje = "No tienes permiso para cancelar esta reserva.";
                return resultado;
            }

            var servicio = reserva.Servicio;
            var tipoServicio = servicio.TipoServicio;

            // Obtener detalle REST (cancelacion solo funciona con REST)
            var detalleRest = await dbContext.DetallesServicio
                .FirstOrDefaultAsync(d => d.ServicioId == servicio.Id && d.TipoProtocolo == TipoProtocolo.Rest);

            if (detalleRest == null || string.IsNullOrEmpty(detalleRest.CancelarReservaEndpoint))
            {
                resultado.Mensaje = "Este proveedor no soporta cancelaciones (solo disponible via REST).";
                return resultado;
            }

            var uriCancelar = $"{detalleRest.UriBase}{detalleRest.CancelarReservaEndpoint}";
            bool exito = false;
            decimal valorReembolsado = 0;

            logger.LogInformation("Cancelando reserva {ReservaId} en {Servicio} via REST", reservaId, servicio.Nombre);

            // Llamar al endpoint de cancelacion segun el tipo de servicio
            switch (tipoServicio)
            {
                case TipoServicio.RentaVehiculos:
                    var (exitoAuto, valorAuto) = await AutoConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    exito = exitoAuto;
                    valorReembolsado = valorAuto;
                    break;

                case TipoServicio.Hotel:
                    var (exitoHotel, valorHotel) = await HotelConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    exito = exitoHotel;
                    valorReembolsado = valorHotel;
                    break;

                case TipoServicio.Aerolinea:
                    var (exitoVuelo, valorVuelo) = await VueloConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    exito = exitoVuelo;
                    valorReembolsado = valorVuelo;
                    break;

                case TipoServicio.Restaurante:
                    var (exitoMesa, valorMesa) = await MesaConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    exito = exitoMesa;
                    valorReembolsado = valorMesa;
                    break;

                case TipoServicio.PaquetesTuristicos:
                    var (exitoPaquete, valorPaquete) = await PaqueteConnector.CancelarReservaAsync(uriCancelar, reserva.CodigoReserva);
                    exito = exitoPaquete;
                    valorReembolsado = valorPaquete;
                    break;

                default:
                    resultado.Mensaje = "Tipo de servicio no soportado para cancelacion.";
                    return resultado;
            }

            if (exito)
            {
                // Reembolsar al cliente
                if (valorReembolsado > 0)
                {
                    var reembolsoExitoso = await TransferirClass.RealizarTransferenciaAsync(
                        cuentaDestino: cuentaBancariaCliente,
                        monto: valorReembolsado,
                        cuentaOrigen: TransferirClass.cuentaDefaultTravelio
                    );

                    if (reembolsoExitoso)
                    {
                        logger.LogInformation("Reembolso de ${Monto} realizado al cliente", valorReembolsado);
                    }
                }

                // Marcar reserva como cancelada en la base de datos
                reserva.Activa = false;
                await dbContext.SaveChangesAsync();

                resultado.Exitoso = true;
                resultado.MontoReembolsado = valorReembolsado;
                resultado.Mensaje = $"Reserva cancelada exitosamente. Se reembolsaron ${valorReembolsado:N2}";
            }
            else
            {
                resultado.Mensaje = "No se pudo cancelar la reserva en el proveedor.";
            }

            return resultado;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelando reserva {ReservaId}", reservaId);
            resultado.Mensaje = "Error al cancelar la reserva.";
            return resultado;
        }
    }
}
