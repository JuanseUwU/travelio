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
/// Implementación del servicio de checkout que integra:
/// - API del Banco para cobros
/// - Servicios SOAP de proveedores para reservas
/// - Base de datos TravelioDb para registro
/// </summary>
public class CheckoutService(TravelioDbContext dbContext, ILogger<CheckoutService> logger) : ICheckoutService
{
    // Comisión de Travelio (10%)
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
            // 1. Validar que el cliente existe
            var cliente = await dbContext.Clientes.FindAsync(clienteId);
            if (cliente == null)
            {
                resultado.Mensaje = "Cliente no encontrado.";
                return resultado;
            }

            // 2. Calcular el total a cobrar
            decimal totalCarrito = items.Sum(i => i.PrecioFinal * i.Cantidad);
            decimal iva = totalCarrito * 0.12m; // 12% IVA Ecuador
            decimal totalConIva = totalCarrito + iva;

            logger.LogInformation("Procesando checkout para cliente {ClienteId}. Total: ${Total}", 
                clienteId, totalConIva);

            // 3. Cobrar al cliente usando la API del banco
            // Transferir desde la cuenta del cliente a la cuenta de Travelio
            var cobroExitoso = await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: TransferirClass.cuentaDefaultTravelio,
                monto: totalConIva,
                cuentaOrigen: cuentaBancariaCliente
            );

            if (!cobroExitoso)
            {
                logger.LogWarning("Falló el cobro al cliente {ClienteId}", clienteId);
                resultado.Mensaje = "No se pudo procesar el pago. Verifica tu saldo o cuenta bancaria.";
                return resultado;
            }

            logger.LogInformation("Cobro exitoso de ${Monto} al cliente {ClienteId}", totalConIva, clienteId);

            // 4. Crear registro de Compra en TravelioDb
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

            // 5. Procesar cada item del carrito
            foreach (var item in items)
            {
                var reservaResult = new ReservaResult
                {
                    Tipo = item.Tipo,
                    Titulo = item.Titulo
                };

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

            // 6. Verificar si todas las reservas fueron exitosas
            var todasExitosas = resultado.Reservas.All(r => r.Exitoso);
            var algunaExitosa = resultado.Reservas.Any(r => r.Exitoso);

            if (todasExitosas)
            {
                resultado.Exitoso = true;
                resultado.Mensaje = "¡Compra realizada con éxito! Tus reservas han sido confirmadas.";
            }
            else if (algunaExitosa)
            {
                resultado.Exitoso = true;
                resultado.Mensaje = "Compra parcialmente exitosa. Algunas reservas no pudieron procesarse.";
            }
            else
            {
                // Si ninguna reserva fue exitosa, intentar devolver el dinero
                resultado.Mensaje = "No se pudieron procesar las reservas. Se intentará reembolsar el pago.";
                
                // Intentar reembolso
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

    /// <summary>
    /// Procesa la reserva de un auto:
    /// 1. Crea prerreserva (hold)
    /// 2. Crea la reserva definitiva
    /// 3. Genera factura del proveedor
    /// 4. Paga al proveedor (90%)
    /// 5. Registra en TravelioDb
    /// </summary>
    private async Task ProcesarReservaAutoAsync(
        CartItemViewModel item,
        Cliente cliente,
        DatosFacturacion datosFacturacion,
        Compra compra,
        ReservaResult reservaResult)
    {
        // Obtener los detalles del servicio SOAP
        var detalle = await dbContext.DetallesServicio
            .Include(d => d.Servicio)
            .Where(d => d.ServicioId == item.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap)
            .FirstOrDefaultAsync();

        if (detalle == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var servicio = detalle.Servicio;

        // Validar fechas
        if (!item.FechaInicio.HasValue || !item.FechaFin.HasValue)
        {
            reservaResult.Error = "Fechas de reserva no válidas";
            return;
        }

        var fechaInicio = item.FechaInicio.Value;
        var fechaFin = item.FechaFin.Value;

        logger.LogInformation("Procesando reserva de auto {IdAuto} en {Servicio}", 
            item.IdProducto, servicio.Nombre);

        // 0. Primero crear/registrar usuario externo en el proveedor (requerido por algunos proveedores)
        try
        {
            if (!string.IsNullOrEmpty(detalle.RegistrarClienteEndpoint))
            {
                var uriCliente = $"{detalle.UriBase}{detalle.RegistrarClienteEndpoint}";
                var clienteExternoId = await AutoConnector.CrearClienteExternoAsync(
                    uriCliente,
                    cliente.Nombre,
                    cliente.Apellido,
                    cliente.CorreoElectronico
                );
                logger.LogInformation("Cliente externo creado/encontrado en proveedor: {ClienteId}", clienteExternoId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo (puede que ya exista o no sea requerido)");
            // Continuamos, algunos proveedores no requieren esto
        }

        // 1. Crear prerreserva (hold)
        var uriPrerreserva = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";
        var (holdId, holdExpira) = await AutoConnector.CrearPrerreservaAsync(
            uriPrerreserva, 
            item.IdProducto, 
            fechaInicio, 
            fechaFin
        );

        logger.LogInformation("Prerreserva creada: {HoldId}, expira: {Expira}", holdId, holdExpira);

        // 2. Crear reserva definitiva
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        var reservaId = await AutoConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            cliente.Nombre,
            cliente.Apellido,
            cliente.TipoIdentificacion,
            cliente.DocumentoIdentidad,
            cliente.CorreoElectronico,
            fechaInicio,
            fechaFin
        );

        logger.LogInformation("Reserva creada en proveedor: {ReservaId}", reservaId);
        reservaResult.CodigoReserva = reservaId.ToString();

        // 3. Generar factura del proveedor
        var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
        decimal subtotal = item.PrecioFinal;
        decimal iva = subtotal * 0.12m;
        decimal total = subtotal + iva;

        try
        {
            var facturaUrl = await AutoConnector.GenerarFacturaAsync(
                uriFactura,
                reservaId,
                subtotal,
                iva,
                total,
                (datosFacturacion.NombreCompleto, datosFacturacion.TipoDocumento, 
                 datosFacturacion.NumeroDocumento, datosFacturacion.Correo)
            );

            reservaResult.FacturaProveedorUrl = facturaUrl;
            logger.LogInformation("Factura generada: {FacturaUrl}", facturaUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura del proveedor para reserva {ReservaId}", reservaId);
            // Continuamos aunque falle la factura
        }

        // 4. Pagar al proveedor (90% del monto, Travelio se queda 10%)
        if (int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            var montoProveedor = item.PrecioFinal * (1 - COMISION_TRAVELIO);
            
            var pagoExitoso = await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: cuentaProveedor,
                monto: montoProveedor,
                cuentaOrigen: TransferirClass.cuentaDefaultTravelio
            );

            if (pagoExitoso)
            {
                logger.LogInformation("Pago de ${Monto} realizado al proveedor {Servicio} (cuenta {Cuenta})", 
                    montoProveedor, servicio.Nombre, cuentaProveedor);
            }
            else
            {
                logger.LogWarning("No se pudo pagar al proveedor {Servicio}", servicio.Nombre);
            }
        }

        // 5. Registrar reserva en TravelioDb
        var reservaDb = new DbReserva
        {
            ServicioId = item.ServicioId,
            CodigoReserva = reservaId.ToString(),
            FacturaUrl = reservaResult.FacturaProveedorUrl
        };
        dbContext.Reservas.Add(reservaDb);
        await dbContext.SaveChangesAsync();

        // 6. Vincular reserva con la compra
        dbContext.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });

        reservaResult.Exitoso = true;
        logger.LogInformation("Reserva {ReservaId} registrada en TravelioDb", reservaDb.Id);
    }

    /// <summary>
    /// Procesa la reserva de una habitación de hotel:
    /// 1. Crea prerreserva (hold)
    /// 2. Crea la reserva definitiva
    /// 3. Genera factura del proveedor
    /// 4. Paga al proveedor (90%)
    /// 5. Registra en TravelioDb
    /// </summary>
    private async Task ProcesarReservaHotelAsync(
        CartItemViewModel item,
        Cliente cliente,
        DatosFacturacion datosFacturacion,
        Compra compra,
        ReservaResult reservaResult)
    {
        var detalle = await dbContext.DetallesServicio
            .Include(d => d.Servicio)
            .Where(d => d.ServicioId == item.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap)
            .FirstOrDefaultAsync();

        if (detalle == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var servicio = detalle.Servicio;

        if (!item.FechaInicio.HasValue || !item.FechaFin.HasValue)
        {
            reservaResult.Error = "Fechas de reserva no válidas";
            return;
        }

        var fechaInicio = item.FechaInicio.Value;
        var fechaFin = item.FechaFin.Value;
        var numeroHuespedes = item.NumeroPersonas ?? 2;

        logger.LogInformation("Procesando reserva de habitación {IdHabitacion} en {Servicio}", 
            item.IdProducto, servicio.Nombre);

        // 0. Crear usuario externo en el proveedor si es necesario
        try
        {
            if (!string.IsNullOrEmpty(detalle.RegistrarClienteEndpoint))
            {
                var uriCliente = $"{detalle.UriBase}{detalle.RegistrarClienteEndpoint}";
                var clienteExternoId = await HotelConnector.CrearUsuarioExternoAsync(
                    uriCliente,
                    cliente.CorreoElectronico,
                    cliente.Nombre,
                    cliente.Apellido
                );
                logger.LogInformation("Cliente externo hotel creado: {ClienteId}", clienteExternoId);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo en hotel");
        }

        // 1. Crear prerreserva (hold)
        var uriPrerreserva = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";
        var holdId = await HotelConnector.CrearPrerreservaAsync(
            uriPrerreserva, 
            item.IdProducto, 
            fechaInicio, 
            fechaFin,
            numeroHuespedes,
            300, // 5 minutos
            item.PrecioUnitario
        );

        logger.LogInformation("Prerreserva de hotel creada: {HoldId}", holdId);

        // 2. Crear reserva definitiva
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        var reservaId = await HotelConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            cliente.Nombre,
            cliente.Apellido,
            cliente.CorreoElectronico,
            cliente.TipoIdentificacion,
            cliente.DocumentoIdentidad,
            fechaInicio,
            fechaFin,
            numeroHuespedes
        );

        logger.LogInformation("Reserva de hotel creada en proveedor: {ReservaId}", reservaId);
        reservaResult.CodigoReserva = reservaId.ToString();

        // 3. Generar factura del proveedor
        var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";

        try
        {
            var facturaUrl = await HotelConnector.EmitirFacturaAsync(
                uriFactura,
                reservaId,
                datosFacturacion.NombreCompleto.Split(' ').FirstOrDefault() ?? cliente.Nombre,
                datosFacturacion.NombreCompleto.Split(' ').Skip(1).FirstOrDefault() ?? cliente.Apellido,
                datosFacturacion.TipoDocumento,
                datosFacturacion.NumeroDocumento,
                datosFacturacion.Correo
            );

            reservaResult.FacturaProveedorUrl = facturaUrl;
            logger.LogInformation("Factura de hotel generada: {FacturaUrl}", facturaUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura del hotel para reserva {ReservaId}", reservaId);
        }

        // 4. Pagar al proveedor (90%)
        if (int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            var montoProveedor = item.PrecioFinal * (1 - COMISION_TRAVELIO);
            
            var pagoExitoso = await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: cuentaProveedor,
                monto: montoProveedor,
                cuentaOrigen: TransferirClass.cuentaDefaultTravelio
            );

            if (pagoExitoso)
            {
                logger.LogInformation("Pago de ${Monto} realizado al hotel {Servicio}", 
                    montoProveedor, servicio.Nombre);
            }
        }

        // 5. Registrar reserva en TravelioDb
        var reservaDb = new DbReserva
        {
            ServicioId = item.ServicioId,
            CodigoReserva = reservaId.ToString(),
            FacturaUrl = reservaResult.FacturaProveedorUrl
        };
        dbContext.Reservas.Add(reservaDb);
        await dbContext.SaveChangesAsync();

        // 6. Vincular reserva con la compra
        dbContext.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });

        reservaResult.Exitoso = true;
        logger.LogInformation("Reserva de hotel {ReservaId} registrada en TravelioDb", reservaDb.Id);
    }

    /// <summary>
    /// Procesa la reserva de un vuelo.
    /// </summary>
    private async Task ProcesarReservaVueloAsync(
        CartItemViewModel item,
        Cliente cliente,
        DatosFacturacion datosFacturacion,
        Compra compra,
        ReservaResult reservaResult)
    {
        var detalle = await dbContext.DetallesServicio
            .Include(d => d.Servicio)
            .Where(d => d.ServicioId == item.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap)
            .FirstOrDefaultAsync();

        if (detalle == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var servicio = detalle.Servicio;
        var numeroPasajeros = item.NumeroPersonas ?? 1;

        logger.LogInformation("Procesando reserva de vuelo {IdVuelo} en {Servicio}", 
            item.IdProducto, servicio.Nombre);

        // Crear array de pasajeros (por ahora solo el cliente principal)
        var pasajeros = new (string nombre, string apellido, string tipoIdentificacion, string identificacion, DateTime fechaNacimiento)[]
        {
            (cliente.Nombre, cliente.Apellido, cliente.TipoIdentificacion, cliente.DocumentoIdentidad, DateTime.Now.AddYears(-30))
        };

        // 0. Crear usuario externo
        try
        {
            if (!string.IsNullOrEmpty(detalle.RegistrarClienteEndpoint))
            {
                var uriCliente = $"{detalle.UriBase}{detalle.RegistrarClienteEndpoint}";
                await VueloConnector.CrearClienteExternoAsync(
                    uriCliente,
                    cliente.CorreoElectronico,
                    cliente.Nombre,
                    cliente.Apellido,
                    DateTime.Now.AddYears(-30),
                    cliente.TipoIdentificacion,
                    cliente.DocumentoIdentidad
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear cliente externo en aerolínea");
        }

        // 1. Crear prerreserva
        var uriPrerreserva = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";
        var (holdId, expira) = await VueloConnector.CrearPrerreservaVueloAsync(
            uriPrerreserva,
            item.IdProducto,
            pasajeros,
            300
        );

        logger.LogInformation("Prerreserva de vuelo creada: {HoldId}", holdId);

        // 2. Crear reserva
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        var (idReserva, codigoReserva, mensaje) = await VueloConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            cliente.CorreoElectronico,
            pasajeros
        );

        logger.LogInformation("Reserva de vuelo creada: {IdReserva} - {Codigo}", idReserva, codigoReserva);
        reservaResult.CodigoReserva = codigoReserva;

        // 3. Generar factura
        var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
        decimal subtotal = item.PrecioFinal;
        decimal iva = subtotal * 0.12m;
        decimal total = subtotal + iva;

        try
        {
            var facturaUrl = await VueloConnector.GenerarFacturaAsync(
                uriFactura,
                idReserva,
                subtotal,
                iva,
                total,
                (datosFacturacion.NombreCompleto, datosFacturacion.TipoDocumento, 
                 datosFacturacion.NumeroDocumento, datosFacturacion.Correo)
            );

            reservaResult.FacturaProveedorUrl = facturaUrl;
            logger.LogInformation("Factura de vuelo generada: {FacturaUrl}", facturaUrl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura del vuelo");
        }

        // 4. Pagar al proveedor
        if (int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            var montoProveedor = item.PrecioFinal * (1 - COMISION_TRAVELIO);
            
            await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: cuentaProveedor,
                monto: montoProveedor,
                cuentaOrigen: TransferirClass.cuentaDefaultTravelio
            );
        }

        // 5. Registrar en TravelioDb
        var reservaDb = new DbReserva
        {
            ServicioId = item.ServicioId,
            CodigoReserva = codigoReserva,
            FacturaUrl = reservaResult.FacturaProveedorUrl
        };
        dbContext.Reservas.Add(reservaDb);
        await dbContext.SaveChangesAsync();

        dbContext.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });

        reservaResult.Exitoso = true;
        logger.LogInformation("Reserva de vuelo registrada en TravelioDb");
    }

    /// <summary>
    /// Procesa la reserva de una mesa de restaurante.
    /// </summary>
    private async Task ProcesarReservaMesaAsync(
        CartItemViewModel item,
        Cliente cliente,
        DatosFacturacion datosFacturacion,
        Compra compra,
        ReservaResult reservaResult)
    {
        var detalle = await dbContext.DetallesServicio
            .Include(d => d.Servicio)
            .Where(d => d.ServicioId == item.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap)
            .FirstOrDefaultAsync();

        if (detalle == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var servicio = detalle.Servicio;
        var fecha = item.FechaInicio ?? DateTime.Today;
        var personas = item.NumeroPersonas ?? 2;
        var idMesa = int.Parse(item.IdProducto);

        logger.LogInformation("Procesando reserva de mesa {IdMesa} en {Servicio}", idMesa, servicio.Nombre);

        // 0. Crear usuario externo
        try
        {
            if (!string.IsNullOrEmpty(detalle.RegistrarClienteEndpoint))
            {
                var uriCliente = $"{detalle.UriBase}{detalle.RegistrarClienteEndpoint}";
                await MesaConnector.CrearUsuarioAsync(
                    uriCliente,
                    cliente.Nombre,
                    cliente.Apellido,
                    cliente.CorreoElectronico,
                    cliente.TipoIdentificacion,
                    cliente.DocumentoIdentidad
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear usuario en restaurante");
        }

        // 1. Crear prerreserva
        var uriPrerreserva = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";
        var (holdId, expira) = await MesaConnector.CrearPreReservaAsync(
            uriPrerreserva,
            idMesa,
            fecha,
            personas,
            300
        );

        logger.LogInformation("Prerreserva de mesa creada: {HoldId}", holdId);

        // 2. Confirmar reserva
        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        var reserva = await MesaConnector.ConfirmarReservaAsync(
            uriReserva,
            idMesa,
            holdId,
            cliente.Nombre,
            cliente.Apellido,
            cliente.CorreoElectronico,
            cliente.TipoIdentificacion,
            cliente.DocumentoIdentidad,
            fecha,
            personas
        );

        logger.LogInformation("Reserva de mesa confirmada: {IdReserva}", reserva.IdReserva);
        reservaResult.CodigoReserva = reserva.IdReserva;

        // 3. Generar factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            var facturaUrl = await MesaConnector.GenerarFacturaAsync(
                uriFactura,
                reserva.IdReserva,
                datosFacturacion.Correo,
                datosFacturacion.NombreCompleto,
                datosFacturacion.TipoDocumento,
                datosFacturacion.NumeroDocumento,
                item.PrecioFinal
            );

            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo generar factura de restaurante");
        }

        // 4. Pagar al proveedor
        if (int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            var montoProveedor = item.PrecioFinal * (1 - COMISION_TRAVELIO);
            await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: cuentaProveedor,
                monto: montoProveedor,
                cuentaOrigen: TransferirClass.cuentaDefaultTravelio
            );
        }

        // 5. Registrar en DB
        var reservaDb = new DbReserva
        {
            ServicioId = item.ServicioId,
            CodigoReserva = reserva.IdReserva,
            FacturaUrl = reservaResult.FacturaProveedorUrl
        };
        dbContext.Reservas.Add(reservaDb);
        await dbContext.SaveChangesAsync();

        dbContext.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });

        reservaResult.Exitoso = true;
        logger.LogInformation("Reserva de mesa registrada en TravelioDb");
    }

    /// <summary>
    /// Procesa la reserva de un paquete turístico.
    /// </summary>
    private async Task ProcesarReservaPaqueteAsync(
        CartItemViewModel item,
        Cliente cliente,
        DatosFacturacion datosFacturacion,
        Compra compra,
        ReservaResult reservaResult)
    {
        var detalle = await dbContext.DetallesServicio
            .Include(d => d.Servicio)
            .Where(d => d.ServicioId == item.ServicioId && d.TipoProtocolo == TipoProtocolo.Soap)
            .FirstOrDefaultAsync();

        if (detalle == null)
        {
            reservaResult.Error = "Servicio no encontrado";
            return;
        }

        var servicio = detalle.Servicio;
        var fechaInicio = item.FechaInicio ?? DateTime.Today;
        var personas = item.NumeroPersonas ?? 1;
        var bookingUserId = cliente.Id.ToString();

        logger.LogInformation("Procesando reserva de paquete {IdPaquete} en {Servicio}", 
            item.IdProducto, servicio.Nombre);

        // 0. Crear usuario externo
        try
        {
            if (!string.IsNullOrEmpty(detalle.RegistrarClienteEndpoint))
            {
                var uriCliente = $"{detalle.UriBase}{detalle.RegistrarClienteEndpoint}";
                await PaqueteConnector.CrearUsuarioExternoAsync(
                    uriCliente,
                    bookingUserId,
                    cliente.Nombre,
                    cliente.Apellido,
                    cliente.CorreoElectronico
                );
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo crear usuario externo en paquetes");
        }

        // 1. Crear hold
        var uriPrerreserva = $"{detalle.UriBase}{detalle.CrearPrerreservaEndpoint}";
        var (holdId, expira) = await PaqueteConnector.CrearHoldAsync(
            uriPrerreserva,
            item.IdProducto,
            bookingUserId,
            fechaInicio,
            personas,
            300
        );

        logger.LogInformation("Hold de paquete creado: {HoldId}", holdId);

        // 2. Crear reserva
        var turistas = new (string nombre, string apellido, DateTime? fechaNacimiento, string tipoIdentificacion, string identificacion)[]
        {
            (cliente.Nombre, cliente.Apellido, null, cliente.TipoIdentificacion, cliente.DocumentoIdentidad)
        };

        var uriReserva = $"{detalle.UriBase}{detalle.CrearReservaEndpoint}";
        var reserva = await PaqueteConnector.CrearReservaAsync(
            uriReserva,
            item.IdProducto,
            holdId,
            bookingUserId,
            "TransferenciaBancaria",
            turistas
        );

        logger.LogInformation("Reserva de paquete creada: {IdReserva}", reserva.IdReserva);
        reservaResult.CodigoReserva = reserva.CodigoReserva;

        // 3. Emitir factura
        try
        {
            var uriFactura = $"{detalle.UriBase}{detalle.GenerarFacturaEndpoint}";
            decimal subtotal = item.PrecioFinal;
            decimal iva = subtotal * 0.12m;
            decimal total = subtotal + iva;

            var facturaUrl = await PaqueteConnector.EmitirFacturaAsync(
                uriFactura,
                reserva.IdReserva,
                subtotal,
                iva,
                total
            );

            reservaResult.FacturaProveedorUrl = facturaUrl;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo emitir factura de paquete");
        }

        // 4. Pagar al proveedor
        if (int.TryParse(servicio.NumeroCuenta, out var cuentaProveedor))
        {
            var montoProveedor = item.PrecioFinal * (1 - COMISION_TRAVELIO);
            await TransferirClass.RealizarTransferenciaAsync(
                cuentaDestino: cuentaProveedor,
                monto: montoProveedor,
                cuentaOrigen: TransferirClass.cuentaDefaultTravelio
            );
        }

        // 5. Registrar en DB
        var reservaDb = new DbReserva
        {
            ServicioId = item.ServicioId,
            CodigoReserva = reserva.CodigoReserva,
            FacturaUrl = reservaResult.FacturaProveedorUrl
        };
        dbContext.Reservas.Add(reservaDb);
        await dbContext.SaveChangesAsync();

        dbContext.ReservasCompra.Add(new ReservaCompra
        {
            CompraId = compra.Id,
            ReservaId = reservaDb.Id
        });

        reservaResult.Exitoso = true;
        logger.LogInformation("Reserva de paquete registrada en TravelioDb");
    }
}
