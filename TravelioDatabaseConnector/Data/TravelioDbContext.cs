using Microsoft.EntityFrameworkCore;
using TravelioDatabaseConnector.Enums;
using TravelioDatabaseConnector.Models;
using TravelioDatabaseConnector.Models.Carrito;

namespace TravelioDatabaseConnector.Data;

public class TravelioDbContext(DbContextOptions<TravelioDbContext> options) : DbContext(options)
{

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<ReservaCompra> ReservasCompra => Set<ReservaCompra>();
    public DbSet<DetalleServicio> DetallesServicio => Set<DetalleServicio>();
    public DbSet<CarritoAerolineaItem> CarritosAerolinea => Set<CarritoAerolineaItem>();
    public DbSet<CarritoAerolineaPasajero> CarritoAerolineaPasajeros => Set<CarritoAerolineaPasajero>();
    public DbSet<CarritoHabitacionItem> CarritosHabitaciones => Set<CarritoHabitacionItem>();
    public DbSet<CarritoAutoItem> CarritosAutos => Set<CarritoAutoItem>();
    public DbSet<CarritoPaqueteItem> CarritosPaquetes => Set<CarritoPaqueteItem>();
    public DbSet<CarritoPaqueteTurista> CarritoPaqueteTuristas => Set<CarritoPaqueteTurista>();
    public DbSet<CarritoMesaItem> CarritosMesas => Set<CarritoMesaItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCliente(modelBuilder);
        ConfigureServicio(modelBuilder);
        ConfigureReserva(modelBuilder);
        ConfigureCompra(modelBuilder);
        ConfigureReservaCompra(modelBuilder);
        ConfigureDetalleServicio(modelBuilder);
        ConfigureCarritoAerolinea(modelBuilder);
        ConfigureCarritoAerolineaPasajero(modelBuilder);
        ConfigureCarritoHabitacion(modelBuilder);
        ConfigureCarritoAuto(modelBuilder);
        ConfigureCarritoPaquete(modelBuilder);
        ConfigureCarritoPaqueteTurista(modelBuilder);
        ConfigureCarritoMesa(modelBuilder);

        SeedServicios(modelBuilder);
        SeedDetallesServicio(modelBuilder);
    }

    private static void ConfigureCliente(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.CorreoElectronico).IsRequired().HasMaxLength(256);
            builder.HasIndex(c => c.CorreoElectronico).IsUnique();
            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Apellido).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Pais).HasMaxLength(100);
            builder.Property(c => c.FechaNacimiento).HasColumnType("date");
            builder.Property(c => c.Telefono).HasMaxLength(50);
            builder.Property(c => c.TipoIdentificacion).IsRequired().HasMaxLength(60);
            builder.Property(c => c.DocumentoIdentidad).IsRequired().HasMaxLength(120);
            builder.Property(c => c.PasswordHash).IsRequired().HasMaxLength(256);
            builder.Property(c => c.PasswordSalt).IsRequired().HasMaxLength(256);
            builder.Property(c => c.Rol)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue(RolUsuario.Cliente);
        });
    }

    private static void ConfigureServicio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Servicio>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Nombre).IsRequired().HasMaxLength(200);
            builder.Property(s => s.NumeroCuenta).IsRequired().HasMaxLength(100);
            builder.Property(s => s.Activo).IsRequired();
            builder.Property(s => s.TipoServicio)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(60);
        });
    }

    private static void ConfigureReserva(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reserva>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.CodigoReserva).IsRequired().HasMaxLength(120);
            builder.Property(r => r.FacturaUrl).HasMaxLength(1024);
            builder.Property(r => r.Activa)
                .IsRequired()
                .HasDefaultValue(true);
            builder.Property(r => r.ValorPagadoNegocio).HasPrecision(18, 2);
            builder.Property(r => r.ComisionAgencia).HasPrecision(18, 2);
            builder.HasOne(r => r.Servicio)
                .WithMany(s => s.Reservas)
                .HasForeignKey(r => r.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompra(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Compra>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.FechaCompra).IsRequired();
            builder.Property(c => c.ValorPagado).IsRequired().HasPrecision(18, 2);
            builder.Property(c => c.FacturaUrl).HasMaxLength(1024);
            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.Compras)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReservaCompra(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReservaCompra>(builder =>
        {
            builder.HasKey(rc => new { rc.CompraId, rc.ReservaId });
            builder.HasOne(rc => rc.Compra)
                .WithMany(c => c.ReservasCompra)
                .HasForeignKey(rc => rc.CompraId);
            builder.HasOne(rc => rc.Reserva)
                .WithMany(r => r.ReservasCompra)
                .HasForeignKey(rc => rc.ReservaId);
        });
    }

    private static void ConfigureDetalleServicio(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DetalleServicio>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.UriBase).HasMaxLength(512);
            builder.Property(d => d.ObtenerProductosEndpoint).HasMaxLength(512);
            builder.Property(d => d.RegistrarClienteEndpoint).HasMaxLength(512);
            builder.Property(d => d.ConfirmarProductoEndpoint).HasMaxLength(512);
            builder.Property(d => d.CrearPrerreservaEndpoint).HasMaxLength(512);
            builder.Property(d => d.CrearReservaEndpoint).HasMaxLength(512);
            builder.Property(d => d.GenerarFacturaEndpoint).HasMaxLength(512);
            builder.Property(d => d.ObtenerReservaEndpoint).HasMaxLength(512);
            builder.Property(d => d.CancelarReservaEndpoint).HasMaxLength(512);
            builder.Property(d => d.TipoProtocolo)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);
            // Cambiado de WithOne a WithMany para soportar múltiples detalles por servicio (SOAP y REST)
            builder.HasOne(d => d.Servicio)
                .WithMany(s => s.DetallesServicio)
                .HasForeignKey(d => d.ServicioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCarritoAerolinea(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoAerolineaItem>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdVueloProveedor).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Origen).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Destino).IsRequired().HasMaxLength(100);
            builder.Property(c => c.TipoCabina).IsRequired().HasMaxLength(80);
            builder.Property(c => c.NombreAerolinea).IsRequired().HasMaxLength(150);
            builder.Property(c => c.PrecioNormal).HasPrecision(18, 2);
            builder.Property(c => c.PrecioActual).HasPrecision(18, 2);
            builder.Property(c => c.DescuentoPorcentaje).HasPrecision(6, 2);
            builder.Property(c => c.HoldId).HasMaxLength(120);
            builder.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.CarritoAerolineaItems)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Servicio)
                .WithMany(s => s.CarritoAerolineaItems)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCarritoAerolineaPasajero(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoAerolineaPasajero>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Apellido).IsRequired().HasMaxLength(150);
            builder.Property(p => p.TipoIdentificacion).IsRequired().HasMaxLength(60);
            builder.Property(p => p.Identificacion).IsRequired().HasMaxLength(120);
            builder.Property(p => p.FechaNacimiento).HasColumnType("date");

            builder.HasOne(p => p.Carrito)
                .WithMany(c => c.Pasajeros)
                .HasForeignKey(p => p.CarritoAerolineaItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCarritoHabitacion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoHabitacionItem>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdHabitacionProveedor).IsRequired().HasMaxLength(120);
            builder.Property(c => c.NombreHabitacion).IsRequired().HasMaxLength(200);
            builder.Property(c => c.TipoHabitacion).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Hotel).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Ciudad).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Pais).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Amenidades).HasMaxLength(1024);
            builder.Property(c => c.Imagenes).HasMaxLength(2048);
            builder.Property(c => c.PrecioNormal).HasPrecision(18, 2);
            builder.Property(c => c.PrecioActual).HasPrecision(18, 2);
            builder.Property(c => c.PrecioVigente).HasPrecision(18, 2);
            builder.Property(c => c.HoldId).HasMaxLength(120);
            builder.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.CarritoHabitacionItems)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Servicio)
                .WithMany(s => s.CarritoHabitacionItems)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCarritoAuto(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoAutoItem>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdAutoProveedor).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Tipo).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Categoria).HasMaxLength(80);
            builder.Property(c => c.Transmision).HasMaxLength(80);
            builder.Property(c => c.Ciudad).HasMaxLength(120);
            builder.Property(c => c.Pais).HasMaxLength(120);
            builder.Property(c => c.UriImagen).HasMaxLength(512);
            builder.Property(c => c.PrecioNormalPorDia).HasPrecision(18, 2);
            builder.Property(c => c.PrecioActualPorDia).HasPrecision(18, 2);
            builder.Property(c => c.DescuentoPorcentaje).HasPrecision(6, 2);
            builder.Property(c => c.HoldId).HasMaxLength(120);
            builder.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.CarritoAutoItems)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Servicio)
                .WithMany(s => s.CarritoAutoItems)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCarritoPaquete(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoPaqueteItem>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.IdPaqueteProveedor).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Ciudad).IsRequired().HasMaxLength(120);
            builder.Property(c => c.Pais).IsRequired().HasMaxLength(120);
            builder.Property(c => c.TipoActividad).IsRequired().HasMaxLength(120);
            builder.Property(c => c.ImagenUrl).HasMaxLength(512);
            builder.Property(c => c.PrecioNormal).HasPrecision(18, 2);
            builder.Property(c => c.PrecioActual).HasPrecision(18, 2);
            builder.Property(c => c.BookingUserId).IsRequired().HasMaxLength(120);
            builder.Property(c => c.HoldId).HasMaxLength(120);
            builder.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.CarritoPaqueteItems)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Servicio)
                .WithMany(s => s.CarritoPaqueteItems)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCarritoPaqueteTurista(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoPaqueteTurista>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(t => t.Apellido).IsRequired().HasMaxLength(150);
            builder.Property(t => t.TipoIdentificacion).IsRequired().HasMaxLength(60);
            builder.Property(t => t.Identificacion).IsRequired().HasMaxLength(120);
            builder.Property(t => t.FechaNacimiento).HasColumnType("date");

            builder.HasOne(t => t.Carrito)
                .WithMany(c => c.Turistas)
                .HasForeignKey(t => t.CarritoPaqueteItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCarritoMesa(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CarritoMesaItem>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.TipoMesa).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Precio).HasPrecision(18, 2);
            builder.Property(c => c.ImagenUrl).HasMaxLength(512);
            builder.Property(c => c.EstadoMesa).IsRequired().HasMaxLength(80);
            builder.Property(c => c.HoldId).HasMaxLength(120);
            builder.Property(c => c.FechaCreacion).HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(c => c.Cliente)
                .WithMany(cl => cl.CarritoMesaItems)
                .HasForeignKey(c => c.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Servicio)
                .WithMany(s => s.CarritoMesaItems)
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void SeedServicios(ModelBuilder modelBuilder)
    {
        var servicios = new[]
        {

            // Aerolínea

            // Isaac Yánez
            new Servicio
            {
                Id = 1,
                Nombre = "Withfly",
                TipoServicio = TipoServicio.Aerolinea,
                NumeroCuenta = "265",
                Activo = true
            },



            // Henry Cruz
            new Servicio
            {
                Id = 2,
                Nombre = "Astrawings",
                TipoServicio = TipoServicio.Aerolinea,
                NumeroCuenta = "192",
                Activo = true
            },



            // Marlon Tomalá
            new Servicio
            {
                Id = 3,
                Nombre = "SkaywardAir",
                TipoServicio = TipoServicio.Aerolinea,
                NumeroCuenta = "247",
                Activo = true
            },



            // Justin Baño

            new Servicio
            {
                Id = 4,
                Nombre = "SkyAndes",
                TipoServicio = TipoServicio.Aerolinea,
                NumeroCuenta = "160",
                Activo = true
            },



            // Michael Barriga

            // Enlaces incorrectos REST

            new Servicio
            {
                Id = 5,
                Nombre = "Caribbean Skyways",
                TipoServicio = TipoServicio.Aerolinea,
                NumeroCuenta = "244",
                Activo = true
            },



            // Habitaciones

            // Pierre Montenegro
            new Servicio
            {
                Id = 101,
                Nombre = "Hotel Campestre",
                TipoServicio = TipoServicio.Hotel,
                NumeroCuenta = "285",
                Activo = true
            },



            // Daniel Carranza
            new Servicio
            {
                Id = 102,
                Nombre = "AllpahouseNYC",
                TipoServicio = TipoServicio.Hotel,
                NumeroCuenta = "275",
                Activo = true
            },



            // Carlos Constante
            new Servicio
            {
                Id = 103,
                Nombre = "Reca",
                TipoServicio = TipoServicio.Hotel,
                NumeroCuenta = "261",
                Activo = true
            },



            // David Ocampo
            new Servicio
            {
                Id = 104,
                Nombre = "Brisamar",
                TipoServicio = TipoServicio.Hotel,
                NumeroCuenta = "280",
                Activo = true
            },



            // Alejandro Gómez
            new Servicio
            {
                Id = 105,
                Nombre = "Hotel Andino",
                TipoServicio = TipoServicio.Hotel,
                NumeroCuenta = "297",
                Activo = true
            },



            // Jossue Gallardo
            new Servicio
            {
                Id = 106,
                Nombre = "Aureacuen",
                TipoServicio = TipoServicio.Hotel,
                NumeroCuenta = "256",
                Activo = true
            },



            // Autos

            // Shirley Pilataxi
            new Servicio
            {
                Id = 201,
                Nombre = "Cuenca Wheels",
                TipoServicio = TipoServicio.RentaVehiculos,
                NumeroCuenta = "174",
                Activo = true
            },



            // Marco Benítez

            new Servicio
            {
                Id = 202,
                Nombre = "LojitaGO",
                TipoServicio = TipoServicio.RentaVehiculos,
                NumeroCuenta = "185",
                Activo = true
            },



            // Joel Tupiza

            new Servicio
            {
                Id = 203,
                Nombre = "EasyCar",
                TipoServicio = TipoServicio.RentaVehiculos,
                NumeroCuenta = "221",
                Activo = true
            },



            // Mateo Sánchez

            new Servicio
            {
                Id = 204,
                Nombre = "Auto Car Rent",
                TipoServicio = TipoServicio.RentaVehiculos,
                NumeroCuenta = "230",
                Activo = true
            },



            // Gabriel Naranjo

            new Servicio
            {
                Id = 205,
                Nombre = "RentaAutosGYE",
                TipoServicio = TipoServicio.RentaVehiculos,
                NumeroCuenta = "225",
                Activo = true
            },



            // Alex Vivanco

            new Servicio
            {
                Id = 206,
                Nombre = "UrbanDrive NY",
                TipoServicio = TipoServicio.RentaVehiculos,
                NumeroCuenta = "227",
                Activo = true
            },



            // Paquetes

            // Christian Coba
            new Servicio
            {
                Id = 301,
                Nombre = "World Agency",
                TipoServicio = TipoServicio.PaquetesTuristicos,
                NumeroCuenta = "198",
                Activo = true
            },



            // Jordi Nogales
            new Servicio
            {
                Id = 302,
                Nombre = "Cuenca Travel",
                TipoServicio = TipoServicio.PaquetesTuristicos,
                NumeroCuenta = "299",
                Activo = true
            },



            // Daniel Valenzuela
            new Servicio
            {
                Id = 303,
                Nombre = "Paquetes Turísticos Web",
                TipoServicio = TipoServicio.PaquetesTuristicos,
                NumeroCuenta = "220",
                Activo = true
            },



            // Restaurantes

            // Allisson Barros
            new Servicio
            {
                Id = 401,
                Nombre = "Cangrejitos Felices",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "218",
                Activo = true
            },



            // Nick Romero

            new Servicio
            {
                Id = 402,
                Nombre = "Dragón Rojo",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "216",
                Activo = true
            },



            // Arturo Albuja

            // No hay número de cuenta

            new Servicio
            {
                Id = 403,
                Nombre = "Café San Juan",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "1",
                Activo = false
            },



            // Melany Acosta

            new Servicio
            {
                Id = 404,
                Nombre = "Sánctum",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "215",
                Activo = true
            },



            // Emilia Lara

            new Servicio
            {
                Id = 405,
                Nombre = "Sabor Andino",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "210",
                Activo = true
            },



            // Jordy Morales

            new Servicio
            {
                Id = 406,
                Nombre = "Bar Sinson",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "167",
                Activo = true
            },



            // Esteban Singo

            new Servicio
            {
                Id = 407,
                Nombre = "7 Mares",
                TipoServicio = TipoServicio.Restaurante,
                NumeroCuenta = "171",
                Activo = true
            },
        };

        modelBuilder.Entity<Servicio>().HasData(servicios);
    }

    private static void SeedDetallesServicio(ModelBuilder modelBuilder)
    {
        var detalles = new[]
        {

            // Aerolínea



            // Isaac Yánez

            new DetalleServicio
            {
                Id = 1,
                ServicioId = 1,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://withflysoaint.runasp.net/IntegracionService.asmx?wsdl",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },
            new DetalleServicio
            {
                Id = 501,
                ServicioId = 1,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://aerolineaintrest.runasp.net/api/v1/integracion/aerolinea",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva", // Estas (API de Aerolínea) se obtienen añadiendo ?idReserva={el número consultado} al final el endpoint, como http://skyandes.runasp.net/api/integracion/aerolinea/reserva?idReserva=1
                CancelarReservaEndpoint = "/cancel"
            },



            // Henry Cruz

            new DetalleServicio
            {
                Id = 2,
                ServicioId = 2,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://astrawingss.runasp.net/IntegracionService.asmx?wsdl",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },
            new DetalleServicio
            {
                Id = 502,
                ServicioId = 2,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://astrawings.runasp.net/api/v1/integracion/aerolinea",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Marlon Tomalá

            new DetalleServicio
            {
                Id = 3,
                ServicioId = 3,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://skaywardairsoap.runasp.net/IntegracionService.asmx?WSDL",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },
            new DetalleServicio
            {
                Id = 503,
                ServicioId = 3,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://skaywardair.runasp.net/api/v1/integracion/aerolinea",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Justin Baño

            new DetalleServicio
            {
                Id = 4,
                ServicioId = 4,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "https://skyandesairlines-ws.runasp.net/SkyAndes_Integracion/WS_Integracion.asmx?WSDL",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },
            new DetalleServicio
            {
                Id = 504,
                ServicioId = 4,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://skyandes.runasp.net/api/integracion/aerolinea",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Michael Barriga

            new DetalleServicio
            {
                Id = 5,
                ServicioId = 5,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://caribbeanskyways.runasp.net/WS_Integracion.asmx?WSDL",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },

            // Enlaces incorrectos
            new DetalleServicio
            {
                Id = 505,
                ServicioId = 5,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://skyandes.runasp.net/api/integracion/aerolinea",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Habitaciones

            // Pierre Montenegro

            new DetalleServicio
            {
                Id = 101,
                ServicioId = 101,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://hotelecampestresoap.runasp.net",
                ObtenerProductosEndpoint = "/BuscarHabitacionesWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/CrearUsuarioExternoWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/ValidarDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/CrearPreReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/ReservarHabitacionWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/EmitirFacturaHotelWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/BuscarDatosReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 601,
                ServicioId = 101,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://hotelcampestrerest.runasp.net/api/v1/hoteles",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva", // Estas (API de Habitaciones de hotel) se obtienen añadiendo ?idReserva={el número consultado} al final el endpoint, como http://hotelcampestrerest.runasp.net/api/v1/hoteles/reserva?idReserva=1
                CancelarReservaEndpoint = "/cancel"
            },



            // Daniel Carranza

            new DetalleServicio
            {
                Id = 102,
                ServicioId = 102,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://allpahousenyc.runasp.net",
                ObtenerProductosEndpoint = "/BuscarHabitacionesWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/CrearUsuarioExternoWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/ValidarDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/CrearPreReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/ReservarHabitacionWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/EmitirFacturaHotelWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/BuscarDatosReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 602,
                ServicioId = 102,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://restallpahousenyc.runasp.net/api/v1/hoteles",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Carlos Constante

            new DetalleServicio
            {
                Id = 103,
                ServicioId = 103,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "https://intehoca-eheqd8h6bvdyfqfy.canadacentral-01.azurewebsites.net",
                ObtenerProductosEndpoint = "/buscarHabitacionesWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/CrearUsuarioExternoWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/ValidarDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/CrearPreReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/ReservarHabitacionWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/EmitirFacturaHotelWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/buscarDatosReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 603,
                ServicioId = 103,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "https://reca.azurewebsites.net/api/v1/hoteles",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // David Ocampo

            new DetalleServicio
            {
                Id = 104,
                ServicioId = 104,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://soapbrisamar.runasp.net",
                ObtenerProductosEndpoint = "/buscarHabitacionesWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/CrearUsuarioExternoWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/ValidarDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/CrearPreReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/ReservarHabitacionWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/EmitirFacturaHotelWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/buscarDatosReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 604,
                ServicioId = 104,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://restbrisamar.runasp.net/api/v1/hoteles",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Alejandro Gómez

            new DetalleServicio
            {
                Id = 105,
                ServicioId = 105,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://hotelandinosoap.runasp.net",
                ObtenerProductosEndpoint = "/BuscarHabitacionesWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/CrearUsuarioExternoWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/ValidarDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/CrearPreReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/ReservarHabitacionWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/EmitirFacturaHotelWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/BuscarDatosReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 605,
                ServicioId = 105,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://restallpahousenyc.runasp.net/api/v1/hoteles",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Jossue Gallardo

            new DetalleServicio
            {
                Id = 106,
                ServicioId = 106,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://aureacuen.runasp.net",
                ObtenerProductosEndpoint = "/buscarHabitacionesWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/CrearUsuarioExternoWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/ValidarDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/CrearPreReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/ReservarHabitacionWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/EmitirFacturaHotelWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/buscarDatosReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 606,
                ServicioId = 106,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://aureacuenrest.runasp.net/api/v1/hoteles",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reserva",
                CancelarReservaEndpoint = "/cancel"
            },



            // Autos

            // Shirley Pilataxi

            new DetalleServicio
            {
                Id = 201,
                ServicioId = 201,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://cuencautosrenta.runasp.net",
                ObtenerProductosEndpoint = "/WS_BuscarAutos.asmx?WSDL",
                RegistrarClienteEndpoint = "/WS_UsuarioExterno.asmx?WSDL",
                ConfirmarProductoEndpoint = "/WS_DisponibilidadAutos.asmx?WSDL",
                CrearPrerreservaEndpoint = "/WS_PreReserva.asmx?WSDL",
                CrearReservaEndpoint = "/WS_ReservarAutos.asmx?WSDL",
                GenerarFacturaEndpoint = "/WS_FacturaIntegracion.asmx?WSDL",
                ObtenerReservaEndpoint = "/WS_BuscarDatos.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 701,
                ServicioId = 201,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://cuencautosinte.runasp.net/api/v1",
                ObtenerProductosEndpoint = "/integracion/autos/search",
                RegistrarClienteEndpoint = "/integracion/autos/usuarios/externo",
                ConfirmarProductoEndpoint = "/integracion/autos/availability",
                CrearPrerreservaEndpoint = "/integracion/autos/hold",
                CrearReservaEndpoint = "/integracion/autos/book",
                GenerarFacturaEndpoint = "/integracion/autos/invoices",
                ObtenerReservaEndpoint = "/integracion/autos/reserva", // Estas (API de Renta de Autos) se obtienen añadiendo el número consultado al final el endpoint, como http://cuencautosinte.runasp.net/api/v1/integracion/autos/reservas/1
                CancelarReservaEndpoint = "/CancelarAuto"
            },



            // Marco Benítez

            new DetalleServicio
            {
                Id = 202,
                ServicioId = 202,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://integsoap.runasp.net",
                ObtenerProductosEndpoint = "/WS_BuscarAutos.asmx?WSDL",
                RegistrarClienteEndpoint = "/WS_UsuarioExterno.asmx?WSDL",
                ConfirmarProductoEndpoint = "/WS_DisponibilidadAutos.asmx?WSDL",
                CrearPrerreservaEndpoint = "/WS_PreReserva.asmx?WSDL",
                CrearReservaEndpoint = "/WS_ReservarAutos.asmx?WSDL",
                GenerarFacturaEndpoint = "/WS_FacturaIntegracion.asmx?WSDL",
                ObtenerReservaEndpoint = "/WS_BuscarDatos.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 702,
                ServicioId = 202,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://restintegracin.runasp.net/api/v1",
                ObtenerProductosEndpoint = "/integracion/autos/search",
                RegistrarClienteEndpoint = "/integracion/autos/usuarios/externo",
                ConfirmarProductoEndpoint = "/integracion/autos/availability",
                CrearPrerreservaEndpoint = "/integracion/autos/hold",
                CrearReservaEndpoint = "/integracion/autos/book",
                GenerarFacturaEndpoint = "/integracion/autos/invoices",
                ObtenerReservaEndpoint = "/integracion/autos/reserva",
                CancelarReservaEndpoint = "/CancelarAuto"
            },



            // Joel Tupiza

            new DetalleServicio
            {
                Id = 203,
                ServicioId = 203,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://gestintsoa.runasp.net",
                ObtenerProductosEndpoint = "/WS_BuscarAutos.asmx?WSDL",
                RegistrarClienteEndpoint = "/WS_UsuarioExterno.asmx?WSDL",
                ConfirmarProductoEndpoint = "/WS_DisponibilidadAutos.asmx?WSDL",
                CrearPrerreservaEndpoint = "/WS_PreReserva.asmx?WSDL",
                CrearReservaEndpoint = "/WS_ReservarAutos.asmx?WSDL",
                GenerarFacturaEndpoint = "/WS_FacturaIntegracion.asmx?WSDL",
                ObtenerReservaEndpoint = "/WS_BuscarDatos.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 703,
                ServicioId = 203,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://integracionrest.runasp.net/api/v1",
                ObtenerProductosEndpoint = "/integracion/autos/search",
                RegistrarClienteEndpoint = "/integracion/autos/usuarios/externo",
                ConfirmarProductoEndpoint = "/integracion/autos/availability",
                CrearPrerreservaEndpoint = "/integracion/autos/hold",
                CrearReservaEndpoint = "/integracion/autos/book",
                GenerarFacturaEndpoint = "/integracion/autos/invoices",
                ObtenerReservaEndpoint = "/integracion/autos/reserva",
                CancelarReservaEndpoint = "/CancelarAuto"
            },



            // Mateo Sánchez

            new DetalleServicio
            {
                Id = 204,
                ServicioId = 204,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://autocarrent.runasp.net",
                ObtenerProductosEndpoint = "/WS_BuscarAutos.asmx?WSDL",
                RegistrarClienteEndpoint = "/WS_UsuarioExterno.asmx?WSDL",
                ConfirmarProductoEndpoint = "/WS_DisponibilidadAutos.asmx?WSDL",
                CrearPrerreservaEndpoint = "/WS_PreReserva.asmx?WSDL",
                CrearReservaEndpoint = "/WS_ReservarAutos.asmx?WSDL",
                GenerarFacturaEndpoint = "/WS_FacturaIntegracion.asmx?WSDL",
                ObtenerReservaEndpoint = "/WS_BuscarDatos.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 704,
                ServicioId = 204,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://autocarent.runasp.net/api/v1",
                ObtenerProductosEndpoint = "/integracion/autos/search",
                RegistrarClienteEndpoint = "/integracion/autos/usuarios/externo",
                ConfirmarProductoEndpoint = "/integracion/autos/availability",
                CrearPrerreservaEndpoint = "/integracion/autos/hold",
                CrearReservaEndpoint = "/integracion/autos/book",
                GenerarFacturaEndpoint = "/integracion/autos/invoices",
                ObtenerReservaEndpoint = "/integracion/autos/reserva",
                CancelarReservaEndpoint = "/CancelarAuto"
            },



            // Gabriel Naranjo

            new DetalleServicio
            {
                Id = 205,
                ServicioId = 205,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://autocarrent.runasp.net",
                ObtenerProductosEndpoint = "/WS_BuscarAutos.asmx?WSDL",
                RegistrarClienteEndpoint = "/WS_UsuarioExterno.asmx?WSDL",
                ConfirmarProductoEndpoint = "/WS_DisponibilidadAutos.asmx?WSDL",
                CrearPrerreservaEndpoint = "/WS_PreReserva.asmx?WSDL",
                CrearReservaEndpoint = "/WS_ReservarAutos.asmx?WSDL",
                GenerarFacturaEndpoint = "/WS_FacturaIntegracion.asmx?WSDL",
                ObtenerReservaEndpoint = "/WS_BuscarDatos.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 705,
                ServicioId = 205,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://restinte.runasp.net/api",
                ObtenerProductosEndpoint = "/v2/integracion/autos/search",
                RegistrarClienteEndpoint = "/v1/integracion/autos/usuarios/externo",
                ConfirmarProductoEndpoint = "/v2/integracion/autos/availability",
                CrearPrerreservaEndpoint = "/v1/integracion/autos/hold",
                CrearReservaEndpoint = "/v1/integracion/autos/book",
                GenerarFacturaEndpoint = "/v1/integracion/autos/invoices",
                ObtenerReservaEndpoint = "/v2/prereserva/auto",
                CancelarReservaEndpoint = "v1/CancelarAuto"
            },



            // Alex Vivanco

            new DetalleServicio
            {
                Id = 206,
                ServicioId = 206,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://autocarrent.runasp.net",
                ObtenerProductosEndpoint = "/WS_BuscarAutos.asmx?WSDL",
                RegistrarClienteEndpoint = "/WS_UsuarioExterno.asmx?WSDL",
                ConfirmarProductoEndpoint = "/WS_DisponibilidadAutos.asmx?WSDL",
                CrearPrerreservaEndpoint = "/WS_PreReserva.asmx?WSDL",
                CrearReservaEndpoint = "/WS_ReservarAutos.asmx?WSDL",
                GenerarFacturaEndpoint = "/WS_FacturaIntegracion.asmx?WSDL",
                ObtenerReservaEndpoint = "/WS_BuscarDatos.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 706,
                ServicioId = 206,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://urbandriveinterest.runasp.net/api/v1",
                ObtenerProductosEndpoint = "/integracion/autos/search",
                RegistrarClienteEndpoint = "/integracion/autos/usuarios/externo",
                ConfirmarProductoEndpoint = "/integracion/autos/availability",
                CrearPrerreservaEndpoint = "/integracion/autos/hold",
                CrearReservaEndpoint = "/integracion/autos/book",
                GenerarFacturaEndpoint = "/integracion/autos/invoices",
                ObtenerReservaEndpoint = "/prereserva/auto",
                CancelarReservaEndpoint = "/CancelarAuto"
            },



            // Paquetes

            // Christian Coba

            new DetalleServicio
            {
                Id = 301,
                ServicioId = 301,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "https://worldagencysoa.runasp.net/PaquetesService.asmx?WSDL",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },
            new DetalleServicio
            {
                Id = 801,
                ServicioId = 301,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "https://worldagencybk.runasp.net/api/v2/paquetes",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/pre-reserva",
                CrearReservaEndpoint = "/reserva",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/{id}/reserva", // Estas (API de Paquetes turísticos) se obtienen reemplazando {id} por el ID de reserva en el enlace, como https://worldagencybk.runasp.net/api/v2/paquetes/1/reserva
                CancelarReservaEndpoint = "/cancelar"
            },



            // Jordi Nogales

            new DetalleServicio
            {
                Id = 302,
                ServicioId = 302,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "https://backend-cuenca.onrender.com/WS_Integracion.asmx?wsdl",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },

            new DetalleServicio
            {
                Id = 802,
                ServicioId = 302,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "https://rest-back-xnjm.onrender.com/api/v2/paquetes",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/pre-reserva",
                CrearReservaEndpoint = "/reserva",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/{id}/reserva",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Daniel Valenzuela

            new DetalleServicio
            {
                Id = 303,
                ServicioId = 303,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://paquetesturisticosweb.runasp.net/Soap/PaquetesService.asmx?WSDL",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "",
                ConfirmarProductoEndpoint = "",
                CrearPrerreservaEndpoint = "",
                CrearReservaEndpoint = "",
                GenerarFacturaEndpoint = "",
                ObtenerReservaEndpoint = ""
            },

            new DetalleServicio
            {
                Id = 803,
                ServicioId = 303,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://paquetesturisticosweb-bk.runasp.net/api/v2/paquetes",
                ObtenerProductosEndpoint = "",
                RegistrarClienteEndpoint = "/usuarios/externo",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/pre-reserva",
                CrearReservaEndpoint = "/reserva",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/{id}/reserva",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Mesas

            // Allison Barros

            new DetalleServicio
            {
                Id = 401,
                ServicioId = 401,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://cangrejitosfelicessoa.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/BusReservaWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 901,
                ServicioId = 401,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://cangrejitosfelices.runasp.net/api/v1/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas", // Estas (API de Mesas) se obtienen añadiendo el número consultado al final el endpoint, como http://cangrejitosfelices.runasp.net/api/v1/integracion/restaurantes/reservas/9
                CancelarReservaEndpoint = "/cancelar"
            },



            // Nick Romero

            new DetalleServicio
            {
                Id = 402,
                ServicioId = 402,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://dragonrojosoap.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/BusReservaWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 902,
                ServicioId = 402,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://dragonrojobus.runasp.net/api/v1/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Arturo Albuja

            new DetalleServicio
            {
                Id = 403,
                ServicioId = 403,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://cafesanjuansoap.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?WSDL",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?WSDL",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?WSDL",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?WSDL",
                CrearReservaEndpoint = "/BusReservaWS.asmx?WSDL",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?WSDL",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?WSDL"
            },
            new DetalleServicio
            {
                Id = 903,
                ServicioId = 403,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://cafesanjuan.runasp.net/api/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Melany Acosta

            new DetalleServicio
            {
                Id = 404,
                ServicioId = 404,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://sanctum-soap.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?wsdl",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?wsdl",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?wsdl",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?wsdl",
                CrearReservaEndpoint = "/BusReservaWS.asmx?wsdl",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?wsdl",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?wsdl"
            },
            new DetalleServicio
            {
                Id = 904,
                ServicioId = 404,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://sanctum.runasp.net/api/v1/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Emilia Lara

            new DetalleServicio
            {
                Id = 405,
                ServicioId = 405,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://saborandinosoa.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?wsdl",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?wsdl",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?wsdl",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?wsdl",
                CrearReservaEndpoint = "/BusReservaWS.asmx?wsdl",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?wsdl",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?wsdl"
            },
            new DetalleServicio
            {
                Id = 905,
                ServicioId = 405,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://saborandino.runasp.net/api/v1/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Jordy Morales

            new DetalleServicio
            {
                Id = 406,
                ServicioId = 406,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://ingtegracion-bar-sinson-soap.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?wsdl",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?wsdl",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?wsdl",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?wsdl",
                CrearReservaEndpoint = "/BusReservaWS.asmx?wsdl",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?wsdl",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?wsdl"
            },
            new DetalleServicio
            {
                Id = 906,
                ServicioId = 406,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://ingtegracion-bar-sinson.runasp.net/api/v1/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas",
                CancelarReservaEndpoint = "/cancelar"
            },



            // Esteban Singo

            new DetalleServicio
            {
                Id = 407,
                ServicioId = 407,
                TipoProtocolo = TipoProtocolo.Soap,
                UriBase = "http://7maresbacksoap.runasp.net",
                ObtenerProductosEndpoint = "/BusBusquedaWS.asmx?wsdl",
                RegistrarClienteEndpoint = "/BusUsuarioWS.asmx?wsdl",
                ConfirmarProductoEndpoint = "/BusDisponibilidadWS.asmx?wsdl",
                CrearPrerreservaEndpoint = "/BusReservaWS.asmx?wsdl",
                CrearReservaEndpoint = "/BusReservaWS.asmx?wsdl",
                GenerarFacturaEndpoint = "/BusFacturaWS.asmx?wsdl",
                ObtenerReservaEndpoint = "/BusReservaWS.asmx?wsdl"
            },
            new DetalleServicio
            {
                Id = 907,
                ServicioId = 407,
                TipoProtocolo = TipoProtocolo.Rest,
                UriBase = "http://7maresback.runasp.net/api/v1/integracion/restaurantes",
                ObtenerProductosEndpoint = "/search",
                RegistrarClienteEndpoint = "/usuarios",
                ConfirmarProductoEndpoint = "/availability",
                CrearPrerreservaEndpoint = "/hold",
                CrearReservaEndpoint = "/book",
                GenerarFacturaEndpoint = "/invoices",
                ObtenerReservaEndpoint = "/reservas",
                CancelarReservaEndpoint = "/cancelar"
            },
        };

        //foreach (var detalle in detalles)
        //{
        //    detalle.CancelarReservaEndpoint = detalle.TipoProtocolo == TipoProtocolo.Rest
        //        ? "/cancel"
        //        : null;
        //}

        modelBuilder.Entity<DetalleServicio>().HasData(detalles);
    }
}
