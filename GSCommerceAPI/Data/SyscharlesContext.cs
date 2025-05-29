using System;
using System.Collections.Generic;
using GSCommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Data;

public partial class SyscharlesContext : DbContext
{
    public SyscharlesContext()
    {
    }

    public SyscharlesContext(DbContextOptions<SyscharlesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Abc> Abcs { get; set; }

    public virtual DbSet<Almacen> Almacens { get; set; }

    public virtual DbSet<AlmacenCuentum> AlmacenCuenta { get; set; }

    public virtual DbSet<AperturaCierreCaja> AperturaCierreCajas { get; set; }

    public virtual DbSet<Articulo> Articulos { get; set; }

    public virtual DbSet<ArticuloVariante> ArticuloVariantes { get; set; }

    public virtual DbSet<ArticulosBk> ArticulosBks { get; set; }

    public virtual DbSet<AsignacionSerieCajero> AsignacionSerieCajeros { get; set; }

    public virtual DbSet<AuxTiprincipal> AuxTiprincipals { get; set; }

    public virtual DbSet<AuxiliarVario> AuxiliarVarios { get; set; }

    public virtual DbSet<Av> Avs { get; set; }

    public virtual DbSet<Av08022022> Av08022022s { get; set; }

    public virtual DbSet<Bakfebolsinenvio> Bakfebolsinenvios { get; set; }

    public virtual DbSet<Bakfequenoaparecenensunat> Bakfequenoaparecenensunats { get; set; }

    public virtual DbSet<BkMovimientosDescuento> BkMovimientosDescuentos { get; set; }

    public virtual DbSet<BkStocks25022020> BkStocks25022020s { get; set; }

    public virtual DbSet<Cliente> Clientes { get; set; }

    public virtual DbSet<ComplementoArticulo> ComplementoArticulos { get; set; }

    public virtual DbSet<Comprobante> Comprobantes { get; set; }

    public virtual DbSet<ComprobanteDeVentaCabecera> ComprobanteDeVentaCabeceras { get; set; }

    public virtual DbSet<ComprobanteDeVentaDetalle> ComprobanteDeVentaDetalles { get; set; }

    public virtual DbSet<ComprobanteDeVentaDetalleTemporal> ComprobanteDeVentaDetalleTemporals { get; set; }

    public virtual DbSet<ComprobantesDeclaradosNoSunat> ComprobantesDeclaradosNoSunats { get; set; }

    public virtual DbSet<Configuracion> Configuracions { get; set; }

    public virtual DbSet<Descuento> Descuentos { get; set; }

    public virtual DbSet<Descuentosbk09022022> Descuentosbk09022022s { get; set; }

    public virtual DbSet<Descuentosbk25032022> Descuentosbk25032022s { get; set; }

    public virtual DbSet<DetallePagoVentum> DetallePagoVenta { get; set; }

    public virtual DbSet<Evento> Eventos { get; set; }

    public virtual DbSet<GuiaRemisionCabecera> GuiaRemisionCabeceras { get; set; }

    public virtual DbSet<GuiaRemisionDetalle> GuiaRemisionDetalles { get; set; }

    public virtual DbSet<IngresosEgresosCabecera> IngresosEgresosCabeceras { get; set; }

    public virtual DbSet<IngresosEgresosDetalle> IngresosEgresosDetalles { get; set; }

    public virtual DbSet<Inventario> Inventarios { get; set; }

    public virtual DbSet<Kardex> Kardices { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<MaestrosFijos2022> MaestrosFijos2022s { get; set; }

    public virtual DbSet<Maquina> Maquinas { get; set; }

    public virtual DbSet<MovimientosCabecera> MovimientosCabeceras { get; set; }

    public virtual DbSet<MovimientosDetalle> MovimientosDetalles { get; set; }

    public virtual DbSet<NotaDeCreditoCabecera> NotaDeCreditoCabeceras { get; set; }

    public virtual DbSet<NotaDeCreditoDetalle> NotaDeCreditoDetalles { get; set; }

    public virtual DbSet<OrdenDeCompraCabecera> OrdenDeCompraCabeceras { get; set; }

    public virtual DbSet<OrdenDeCompraDetalle> OrdenDeCompraDetalles { get; set; }

    public virtual DbSet<Personal> Personals { get; set; }

    public virtual DbSet<PlanDeCuenta> PlanDeCuentas { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<Resuman> Resumen { get; set; }

    public virtual DbSet<ResumenCierreDeCaja> ResumenCierreDeCajas { get; set; }

    public virtual DbSet<SerieCorrelativo> SerieCorrelativos { get; set; }

    public virtual DbSet<StockAlmacen> StockAlmacens { get; set; }

    public virtual DbSet<StockIcabk> StockIcabks { get; set; }

    public virtual DbSet<Sunat> Sunats { get; set; }

    public virtual DbSet<TipoDeCambio> TipoDeCambios { get; set; }

    public virtual DbSet<TipoDocumentoVentum> TipoDocumentoVenta { get; set; }

    public virtual DbSet<TipoPagoVentum> TipoPagoVenta { get; set; }

    public virtual DbSet<Todosalmacenes25032022> Todosalmacenes25032022s { get; set; }

    public virtual DbSet<TomaInventario> TomaInventarios { get; set; }

    public virtual DbSet<TomaInventarioDetalle> TomaInventarioDetalles { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VAlmacen1> VAlmacen1s { get; set; }

    public virtual DbSet<VAlmacenesAperturaFecha1> VAlmacenesAperturaFecha1s { get; set; }

    public virtual DbSet<VArticulo> VArticulos { get; set; }

    public virtual DbSet<VArticulos1> VArticulos1s { get; set; }

    public virtual DbSet<VArticulos2> VArticulos2s { get; set; }

    public virtual DbSet<VArticulosBusqueda1> VArticulosBusqueda1s { get; set; }

    public virtual DbSet<VCierreEnLinea1> VCierreEnLinea1s { get; set; }

    public virtual DbSet<VCierreVentaDiaria1> VCierreVentaDiaria1s { get; set; }

    public virtual DbSet<VClientes1> VClientes1s { get; set; }

    public virtual DbSet<VComprobante> VComprobantes { get; set; }

    public virtual DbSet<VConsultasArticulo> VConsultasArticulos { get; set; }

    public virtual DbSet<VDescuento> VDescuentos { get; set; }

    public virtual DbSet<VDetalleGuiaRemision1> VDetalleGuiaRemision1s { get; set; }

    public virtual DbSet<VDetalleOcparaIngreso1> VDetalleOcparaIngreso1s { get; set; }

    public virtual DbSet<VDetallePagoVenta1> VDetallePagoVenta1s { get; set; }

    public virtual DbSet<VDetallePagoVenta2> VDetallePagoVenta2s { get; set; }

    public virtual DbSet<VDetallesVenta> VDetallesVentas { get; set; }

    public virtual DbSet<VDocumentosEnviadosSunat> VDocumentosEnviadosSunats { get; set; }

    public virtual DbSet<VHistoricoVenta> VHistoricoVentas { get; set; }

    public virtual DbSet<VKardex1> VKardex1s { get; set; }

    public virtual DbSet<VKardex2> VKardex2s { get; set; }

    public virtual DbSet<VKardex3> VKardex3s { get; set; }

    public virtual DbSet<VKardexGeneral> VKardexGenerals { get; set; }

    public virtual DbSet<VListadoAperturaCierre1> VListadoAperturaCierre1s { get; set; }

    public virtual DbSet<VListadoComprasAproveedores1> VListadoComprasAproveedores1s { get; set; }

    public virtual DbSet<VListadoGr1> VListadoGr1s { get; set; }

    public virtual DbSet<VListadoIngresosEgresos1> VListadoIngresosEgresos1s { get; set; }

    public virtual DbSet<VListadoIngresosEgresosAlmacen1> VListadoIngresosEgresosAlmacen1s { get; set; }

    public virtual DbSet<VListadoNotaCredito1> VListadoNotaCredito1s { get; set; }

    public virtual DbSet<VListadoOc1> VListadoOc1s { get; set; }

    public virtual DbSet<VListadoPagosAproveedores1> VListadoPagosAproveedores1s { get; set; }

    public virtual DbSet<VListadoVentaDiaria1> VListadoVentaDiaria1s { get; set; }

    public virtual DbSet<VPersonal1> VPersonal1s { get; set; }

    public virtual DbSet<VPersonal2> VPersonal2s { get; set; }

    public virtual DbSet<VPersonal3> VPersonal3s { get; set; }

    public virtual DbSet<VProveedores1> VProveedores1s { get; set; }

    public virtual DbSet<VRecaudacion3> VRecaudacion3s { get; set; }

    public virtual DbSet<VResumenCierreDeCaja1> VResumenCierreDeCaja1s { get; set; }

    public virtual DbSet<VResumenCierreDeCaja2> VResumenCierreDeCaja2s { get; set; }

    public virtual DbSet<VSerieCorrelativo1> VSerieCorrelativo1s { get; set; }

    public virtual DbSet<VSerieCorrelativo2> VSerieCorrelativo2s { get; set; }

    public virtual DbSet<VSeriesXalmacen1> VSeriesXalmacen1s { get; set; }

    public virtual DbSet<VSeriesXcajero1> VSeriesXcajero1s { get; set; }

    public virtual DbSet<VStockXalmacen1> VStockXalmacen1s { get; set; }

    public virtual DbSet<VUsuarioAperturaAlmacen1> VUsuarioAperturaAlmacen1s { get; set; }

    public virtual DbSet<VVenta1> VVenta1s { get; set; }

    public virtual DbSet<VVentasDw> VVentasDws { get; set; }

    public virtual DbSet<VVentasXalmacenXmesXanoXproveedor1> VVentasXalmacenXmesXanoXproveedor1s { get; set; }

    public virtual DbSet<VentasBak09112020> VentasBak09112020s { get; set; }

    public virtual DbSet<VentasBk01012019> VentasBk01012019s { get; set; }

    public virtual DbSet<Xyz> Xyzs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=localhost;Database=SYSCHARLES;User Id=sa;Password=4pfzqo0yE@;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Abc>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("abc");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Almacen>(entity =>
        {
            entity.HasKey(e => e.IdAlmacen);

            entity.ToTable("Almacen", "Maestros");

            entity.Property(e => e.Abreviacion)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AfectoIgv)
                .HasDefaultValue(true)
                .HasColumnName("AfectoIGV");
            entity.Property(e => e.Celular)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Certificado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ClaveSol)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ClaveSOL");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Dpd)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPD");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordCertificado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.Telefono)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Ubigeo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UsuarioSol)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("UsuarioSOL");
        });

        modelBuilder.Entity<AlmacenCuentum>(entity =>
        {
            entity.HasKey(e => e.IdAlmacenCuenta).HasName("PK_CuentaAlmacen");

            entity.ToTable("AlmacenCuenta", "Maestros");

            entity.Property(e => e.Banco)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Cci)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CCI");
            entity.Property(e => e.Cuenta)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.AlmacenCuenta)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentaAlmacen_Almacen");
        });

        modelBuilder.Entity<AperturaCierreCaja>(entity =>
        {
            entity.HasKey(e => e.IdAperturaCierre);

            entity.ToTable("AperturaCierreCaja", "Movimientos");

            entity.Property(e => e.Egresos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.FondoFijo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Ingresos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ObservacionApertura)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ObservacionCierre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SaldoFinal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SaldoInicial).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VentaDia).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.AperturaCierreCajas)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AperturaCierreCaja_Almacen");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.AperturaCierreCajas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AperturaCierreCaja_Usuario");
        });

        modelBuilder.Entity<Articulo>(entity =>
        {
            entity.HasKey(e => e.IdArticulo);

            entity.ToTable("Articulo", "Maestros");

            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CodigoBarra).HasColumnType("image");
            entity.Property(e => e.Color)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.DescripcionCorta)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Detalle)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Estacion)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Familia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Foto).HasColumnType("image");
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MonedaCosteo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Talla)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UnidadAlmacen)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdProveedorNavigation).WithMany(p => p.Articulos)
                .HasForeignKey(d => d.IdProveedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Articulo_Proveedor");
        });

        modelBuilder.Entity<ArticuloVariante>(entity =>
        {
            entity.HasKey(e => e.IdVariante).HasName("PK__Articulo__4ACF8F0FE1C1A42D");

            entity.ToTable("ArticuloVariante", "Maestros");

            entity.Property(e => e.Color)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Talla)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.ArticuloVariantes)
                .HasForeignKey(d => d.IdArticulo)
                .HasConstraintName("FK_ArticuloVariante_Articulo");
        });

        modelBuilder.Entity<ArticulosBk>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ArticulosBK");

            entity.Property(e => e.CodigoBarra).HasColumnType("image");
            entity.Property(e => e.Color)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.DescripcionCorta)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Detalle)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Familia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Foto).HasColumnType("image");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MonedaCosteo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Talla)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UnidadAlmacen)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AsignacionSerieCajero>(entity =>
        {
            entity.HasKey(e => e.IdAsignacion);

            entity.ToTable("AsignacionSerieCajero", "Maestros");
        });

        modelBuilder.Entity<AuxTiprincipal>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AuxTIPrincipal");

            entity.Property(e => e.Codigo).HasColumnName("codigo");
            entity.Property(e => e.Fisico).HasColumnName("fisico");
        });

        modelBuilder.Entity<AuxiliarVario>(entity =>
        {
            entity.HasKey(e => e.IdAuxiliar);

            entity.ToTable("AuxiliarVarios", "Maestros");

            entity.Property(e => e.Alias)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Auxiliar)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Av>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AV");

            entity.Property(e => e.Almacén).HasMaxLength(255);
            entity.Property(e => e.CódigoArtículo).HasColumnName("Código Artículo");
            entity.Property(e => e.DescripciónArtículo)
                .HasMaxLength(255)
                .HasColumnName("Descripción Artículo");
            entity.Property(e => e.Familia).HasMaxLength(255);
            entity.Property(e => e.Línea).HasMaxLength(255);
            entity.Property(e => e.Precio).HasColumnName("PRECIO $");
            entity.Property(e => e.PrecioS).HasColumnName("PRECIO S/");
            entity.Property(e => e.PrecioVenta).HasColumnName("Precio Venta");
        });

        modelBuilder.Entity<Av08022022>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AV08022022");

            entity.Property(e => e.Almacén).HasMaxLength(255);
            entity.Property(e => e.CódigoArtículo).HasColumnName("Código Artículo");
            entity.Property(e => e.DescripciónArtículo)
                .HasMaxLength(255)
                .HasColumnName("Descripción Artículo");
            entity.Property(e => e.Familia).HasMaxLength(255);
            entity.Property(e => e.Línea).HasMaxLength(255);
            entity.Property(e => e.Precio).HasColumnName("PRECIO $");
            entity.Property(e => e.PrecioS).HasColumnName("PRECIO S/");
            entity.Property(e => e.PrecioVenta).HasColumnName("Precio Venta");
            entity.Property(e => e.TipoDeCambio).HasColumnName("Tipo de Cambio");
        });

        modelBuilder.Entity<Bakfebolsinenvio>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("bakfebolsinenvio");

            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaRespuestaSunat)
                .HasColumnType("datetime")
                .HasColumnName("FechaRespuestaSUNAT");
            entity.Property(e => e.Hash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(5000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Xml).IsUnicode(false);
        });

        modelBuilder.Entity<Bakfequenoaparecenensunat>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("bakfequenoaparecenensunat");

            entity.Property(e => e.EnviadoSunat).HasColumnName("EnviadoSUNAT");
            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaRespuestaSunat)
                .HasColumnType("datetime")
                .HasColumnName("FechaRespuestaSUNAT");
            entity.Property(e => e.Hash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IdFe)
                .ValueGeneratedOnAdd()
                .HasColumnName("IdFE");
            entity.Property(e => e.Qr)
                .HasColumnType("image")
                .HasColumnName("QR");
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(5000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Xml).IsUnicode(false);
        });

        modelBuilder.Entity<BkMovimientosDescuento>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("bkMovimientosDescuentos");

            entity.Property(e => e.IdAlmacen).HasColumnName("idAlmacen");
            entity.Property(e => e.IdArticulo).HasColumnName("idArticulo");
            entity.Property(e => e.IdDescuento)
                .ValueGeneratedOnAdd()
                .HasColumnName("idDescuento");
        });

        modelBuilder.Entity<BkStocks25022020>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("bkStocks25022020");

            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente);

            entity.ToTable("Cliente", "Maestros");

            entity.Property(e => e.Celular)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Dpd)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPD");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ComplementoArticulo>(entity =>
        {
            entity.HasKey(e => e.IdComplemento);

            entity.ToTable("ComplementoArticulo", "Maestros");

            entity.Property(e => e.Alias)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Complemento)
                .HasMaxLength(20)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Comprobante>(entity =>
        {
            entity.HasKey(e => e.IdFe);

            entity.ToTable("Comprobantes", "FE");

            entity.Property(e => e.IdFe).HasColumnName("IdFE");
            entity.Property(e => e.EnviadoSunat).HasColumnName("EnviadoSUNAT");
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaRespuestaSunat)
                .HasColumnType("datetime")
                .HasColumnName("FechaRespuestaSUNAT");
            entity.Property(e => e.Hash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Qr)
                .HasColumnType("image")
                .HasColumnName("QR");
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(5000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Xml).IsUnicode(false);
        });

        modelBuilder.Entity<ComprobanteDeVentaCabecera>(entity =>
        {
            entity.HasKey(e => e.IdComprobante);

            entity.ToTable("ComprobanteDeVentaCabecera", "Movimientos");

            entity.Property(e => e.Apagar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("APagar");
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraRegistro).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraUsuarioAnula).HasColumnType("datetime");
            entity.Property(e => e.GeneroNc)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("GeneroNC");
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.ComprobanteDeVentaCabeceras)
                .HasForeignKey(d => d.IdCliente)
                .HasConstraintName("FK_ComprobanteDeVentaCabecera_Cliente");

            entity.HasOne(d => d.IdTipoDocumentoNavigation).WithMany(p => p.ComprobanteDeVentaCabeceras)
                .HasForeignKey(d => d.IdTipoDocumento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComprobanteDeVentaCabecera_TipoDocumentoVenta");
        });

        modelBuilder.Entity<ComprobanteDeVentaDetalle>(entity =>
        {
            entity.HasKey(e => new { e.IdComprobante, e.Item });

            entity.ToTable("ComprobanteDeVentaDetalle", "Movimientos");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.ComprobanteDeVentaDetalles)
                .HasForeignKey(d => d.IdArticulo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComprobanteDeVentaDetalle_Articulo");

            entity.HasOne(d => d.IdComprobanteNavigation).WithMany(p => p.ComprobanteDeVentaDetalles)
                .HasForeignKey(d => d.IdComprobante)
                .HasConstraintName("FK_ComprobanteDeVentaDetalle_ComprobanteDeVentaCabecera");
        });

        modelBuilder.Entity<ComprobanteDeVentaDetalleTemporal>(entity =>
        {
            entity.HasKey(e => new { e.IdComprobante, e.Item });

            entity.ToTable("ComprobanteDeVentaDetalleTemporal", "Auxiliares");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ComprobantesDeclaradosNoSunat>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ComprobantesDeclaradosNoSUNAT");

            entity.Property(e => e.EnviadoSunat).HasColumnName("EnviadoSUNAT");
            entity.Property(e => e.EstadoComprobante)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaRespuestaSunat)
                .HasColumnType("datetime")
                .HasColumnName("FechaRespuestaSUNAT");
            entity.Property(e => e.Hash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IdFe).HasColumnName("IdFE");
            entity.Property(e => e.Qr)
                .HasColumnType("image")
                .HasColumnName("QR");
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(5000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Xml).IsUnicode(false);
        });

        modelBuilder.Entity<Configuracion>(entity =>
        {
            entity.HasKey(e => e.Configuracion1);

            entity.ToTable("Configuracion", "Maestros");

            entity.Property(e => e.Configuracion1)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("Configuracion");
            entity.Property(e => e.Descripcion).IsUnicode(false);
            entity.Property(e => e.Valor)
                .HasMaxLength(250)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Descuento>(entity =>
        {
            entity.HasKey(e => e.IdDescuento);

            entity.ToTable("Descuentos", "Movimientos");

            entity.Property(e => e.IdDescuento).HasColumnName("idDescuento");
            entity.Property(e => e.Descuento1).HasColumnName("Descuento");
            entity.Property(e => e.IdAlmacen).HasColumnName("idAlmacen");
            entity.Property(e => e.IdArticulo).HasColumnName("idArticulo");
        });

        modelBuilder.Entity<Descuentosbk09022022>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Descuentosbk09022022", "Movimientos");

            entity.Property(e => e.IdAlmacen).HasColumnName("idAlmacen");
            entity.Property(e => e.IdArticulo).HasColumnName("idArticulo");
            entity.Property(e => e.IdDescuento)
                .ValueGeneratedOnAdd()
                .HasColumnName("idDescuento");
        });

        modelBuilder.Entity<Descuentosbk25032022>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Descuentosbk25032022");

            entity.Property(e => e.IdAlmacen).HasColumnName("idAlmacen");
            entity.Property(e => e.IdArticulo).HasColumnName("idArticulo");
            entity.Property(e => e.IdDescuento)
                .ValueGeneratedOnAdd()
                .HasColumnName("idDescuento");
        });

        modelBuilder.Entity<DetallePagoVentum>(entity =>
        {
            entity.HasKey(e => e.IdDetallePagoVenta);

            entity.ToTable("DetallePagoVenta", "Movimientos");

            entity.Property(e => e.Datos)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Dolares).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PorcentajeTarjetaDolares).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PorcentajeTarjetaSoles).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Soles).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vuelto)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdComprobanteNavigation).WithMany(p => p.DetallePagoVenta)
                .HasForeignKey(d => d.IdComprobante)
                .HasConstraintName("FK_DetallePagoVenta_ComprobanteDeVentaCabecera");

            entity.HasOne(d => d.IdTipoPagoVentaNavigation).WithMany(p => p.DetallePagoVenta)
                .HasForeignKey(d => d.IdTipoPagoVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DetallePagoVenta_TipoPagoVenta");
        });

        modelBuilder.Entity<Evento>(entity =>
        {
            entity.ToTable("Eventos", "Auditoria", tb => tb.HasComment("Eventos CRUD"));

            entity.Property(e => e.Crud)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("CRUD");
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FilasAfectadas).HasColumnType("xml");
            entity.Property(e => e.Glosa)
                .HasMaxLength(5000)
                .IsUnicode(false);
        });

        modelBuilder.Entity<GuiaRemisionCabecera>(entity =>
        {
            entity.HasKey(e => e.IdGuiaRemision);

            entity.ToTable("GuiaRemisionCabecera", "Almacen");

            entity.Property(e => e.DireccionDestino)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DireccionOrigen)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Dpddestino)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPDDestino");
            entity.Property(e => e.Dpdorigen)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPDOrigen");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocialNombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Rucdnice)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUCDNICE");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.TipoDestinatario)
                .HasMaxLength(9)
                .IsUnicode(false);

            entity.HasOne(d => d.IdMovimientoNavigation).WithMany(p => p.GuiaRemisionCabeceras)
                .HasForeignKey(d => d.IdMovimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GuiaRemisionCabecera_MovimientosCabecera");
        });

        modelBuilder.Entity<GuiaRemisionDetalle>(entity =>
        {
            entity.HasKey(e => new { e.IdGuiaRemision, e.Item });

            entity.ToTable("GuiaRemisionDetalle", "Almacen");

            entity.Property(e => e.DescripcionArticulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdGuiaRemisionNavigation).WithMany(p => p.GuiaRemisionDetalles)
                .HasForeignKey(d => d.IdGuiaRemision)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GuiaRemisionDetalle_GuiaRemisionCabecera");
        });

        modelBuilder.Entity<IngresosEgresosCabecera>(entity =>
        {
            entity.HasKey(e => e.IdIngresoEgreso).HasName("PK_IngresosEgresos");

            entity.ToTable("IngresosEgresosCabecera", "Movimientos");

            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.Glosa)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Naturaleza)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.IngresosEgresosCabeceras)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IngresosEgresos_Almacen");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.IngresosEgresosCabeceras)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IngresosEgresos_Usuario");
        });

        modelBuilder.Entity<IngresosEgresosDetalle>(entity =>
        {
            entity.HasKey(e => e.IdDetalleIngresoEgreso).HasName("PK_DetalleIngresosEgresos");

            entity.ToTable("IngresosEgresosDetalle", "Movimientos");

            entity.Property(e => e.Banco)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Cuenta)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Detalle)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Forma)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Imagen).HasColumnType("image");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdIngresoEgresoNavigation).WithMany(p => p.IngresosEgresosDetalles)
                .HasForeignKey(d => d.IdIngresoEgreso)
                .HasConstraintName("FK_DetalleIngresosEgresos_IngresosEgresos");
        });

        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.HasKey(e => e.InventarioId).HasName("PK__Inventar__FB8A24D785E18B49");

            entity.ToTable("Inventario", "Almacen");

            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Kardex>(entity =>
        {
            entity.HasKey(e => e.IdKardex);

            entity.ToTable("Kardex", "Movimientos");

            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NoKardexGeneral).HasDefaultValue(false);
            entity.Property(e => e.Origen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.Kardices)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Almacen");

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.Kardices)
                .HasForeignKey(d => d.IdArticulo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Articulo");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.ToTable("Log", "Auditoria");

            entity.Property(e => e.Error).IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Modulo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Usuario)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<MaestrosFijos2022>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Maestros.Fijos2022");

            entity.Property(e => e.AguasVerdesAfp).HasColumnName("AGUAS VERDES AFP ");
            entity.Property(e => e.AguasVerdesAlquiler).HasColumnName("AGUAS VERDES ALQUILER ");
            entity.Property(e => e.AguasVerdesSunat).HasColumnName("AGUAS VERDES SUNAT");
            entity.Property(e => e.ChiclayoAfp).HasColumnName("CHICLAYO AFP ");
            entity.Property(e => e.ChiclayoAlquiler).HasColumnName("CHICLAYO ALQUILER ");
            entity.Property(e => e.ChiclayoSunat).HasColumnName("CHICLAYO SUNAT");
            entity.Property(e => e.IquitosAfp).HasColumnName("IQUITOS AFP ");
            entity.Property(e => e.IquitosAlquiler).HasColumnName("IQUITOS ALQUILER ");
            entity.Property(e => e.IquitosSunat).HasColumnName("IQUITOS SUNAT");
            entity.Property(e => e.Mes).HasMaxLength(255);
            entity.Property(e => e.PiuraAfp).HasColumnName("PIURA AFP ");
            entity.Property(e => e.PiuraAlquiler).HasColumnName("PIURA ALQUILER ");
            entity.Property(e => e.PiuraSunat).HasColumnName("PIURA SUNAT");
            entity.Property(e => e.SullanaAfp).HasColumnName("SULLANA AFP ");
            entity.Property(e => e.SullanaAlquiler).HasColumnName("SULLANA ALQUILER ");
            entity.Property(e => e.SullanaSunat).HasColumnName("SULLANA SUNAT");
            entity.Property(e => e.TrujilloAfp).HasColumnName("TRUJILLO AFP ");
            entity.Property(e => e.TrujilloAlquiler).HasColumnName("TRUJILLO ALQUILER ");
            entity.Property(e => e.TrujilloSunat).HasColumnName("TRUJILLO SUNAT");
        });

        modelBuilder.Entity<Maquina>(entity =>
        {
            entity.HasKey(e => e.IdPc);

            entity.ToTable("Maquinas", "Maestros");

            entity.Property(e => e.IdPc).HasColumnName("IdPC");
            entity.Property(e => e.Anydesk)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IdUsuario).HasDefaultValue(1);
            entity.Property(e => e.Mac)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("MAC");
        });

        modelBuilder.Entity<MovimientosCabecera>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento).HasName("PK_IngresosEgresosCabecera");

            entity.ToTable("MovimientosCabecera", "Almacen");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.FechaHoraConfirma).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraRegistro).HasColumnType("datetime");
            entity.Property(e => e.Motivo)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Tipo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.MovimientosCabeceras)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosCabecera_Almacen");

            entity.HasOne(d => d.IdOcNavigation).WithMany(p => p.MovimientosCabeceras)
                .HasForeignKey(d => d.IdOc)
                .HasConstraintName("FK_MovimientosCabecera_OrdenDeCompraCabecera");
        });

        modelBuilder.Entity<MovimientosDetalle>(entity =>
        {
            entity.HasKey(e => new { e.IdMovimiento, e.Item }).HasName("PK_IngresosEgresosDetalle");

            entity.ToTable("MovimientosDetalle", "Almacen");

            entity.Property(e => e.DescripcionArticulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.MovimientosDetalles)
                .HasForeignKey(d => d.IdArticulo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosDetalle_Articulo");

            entity.HasOne(d => d.IdMovimientoNavigation).WithMany(p => p.MovimientosDetalles)
                .HasForeignKey(d => d.IdMovimiento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosDetalle_MovimientosCabecera");
        });

        modelBuilder.Entity<NotaDeCreditoCabecera>(entity =>
        {
            entity.HasKey(e => e.IdNc);

            entity.ToTable("NotaDeCreditoCabecera", "Movimientos");

            entity.Property(e => e.IdNc).HasColumnName("IdNC");
            entity.Property(e => e.Afavor)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("AFavor");
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Empleada).HasDefaultValue(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraRegistro).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraUsuarioAnula).HasColumnType("datetime");
            entity.Property(e => e.IdMotivo)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Referencia)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdClienteNavigation).WithMany(p => p.NotaDeCreditoCabeceras)
                .HasForeignKey(d => d.IdCliente)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotaDeCreditoCabecera_Cliente");

            entity.HasOne(d => d.IdTipoDocumentoNavigation).WithMany(p => p.NotaDeCreditoCabeceras)
                .HasForeignKey(d => d.IdTipoDocumento)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotaDeCreditoCabecera_TipoDocumentoVenta");
        });

        modelBuilder.Entity<NotaDeCreditoDetalle>(entity =>
        {
            entity.HasKey(e => new { e.IdNc, e.Item });

            entity.ToTable("NotaDeCreditoDetalle", "Movimientos");

            entity.Property(e => e.IdNc).HasColumnName("IdNC");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdNcNavigation).WithMany(p => p.NotaDeCreditoDetalles)
                .HasForeignKey(d => d.IdNc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotaDeCreditoDetalle_NotaDeCreditoCabecera");
        });

        modelBuilder.Entity<OrdenDeCompraCabecera>(entity =>
        {
            entity.HasKey(e => e.IdOc);

            entity.ToTable("OrdenDeCompraCabecera", "Movimientos");

            entity.Property(e => e.Atencion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DireccionProveedor)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EstadoAtencion)
                .HasMaxLength(2)
                .IsUnicode(false)
                .HasDefaultValue("P")
                .IsFixedLength();
            entity.Property(e => e.FechaAnulado).HasColumnType("datetime");
            entity.Property(e => e.FechaAtencionTotal).HasColumnType("datetime");
            entity.Property(e => e.FechaCierre).HasColumnType("datetime");
            entity.Property(e => e.FechaEmision).HasColumnType("datetime");
            entity.Property(e => e.FechaEntrega).HasColumnType("datetime");
            entity.Property(e => e.FechaOc)
                .HasColumnType("datetime")
                .HasColumnName("FechaOC");
            entity.Property(e => e.FechaRegistra).HasColumnType("datetime");
            entity.Property(e => e.FormaPago)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Glosa)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ImporteIgv)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("ImporteIGV");
            entity.Property(e => e.ImporteSubTotal).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.ImporteTotal).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.Moneda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.NombreProveedor)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOc)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NumeroOC");
            entity.Property(e => e.Rucproveedor)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUCProveedor");
            entity.Property(e => e.SinIgv).HasColumnName("SinIGV");
            entity.Property(e => e.TipoCambio).HasColumnType("numeric(8, 4)");

            entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.OrdenDeCompraCabeceras)
                .HasForeignKey(d => d.IdUsuarioRegistra)
                .HasConstraintName("FK_OrdenDeCompraCabecera_Usuario");
        });

        modelBuilder.Entity<OrdenDeCompraDetalle>(entity =>
        {
            entity.HasKey(e => new { e.IdOc, e.Item });

            entity.ToTable("OrdenDeCompraDetalle", "Movimientos");

            entity.Property(e => e.CostoUnitario).HasColumnType("numeric(14, 2)");
            entity.Property(e => e.DescripcionArticulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Total).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.OrdenDeCompraDetalles)
                .HasForeignKey(d => d.IdArticulo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenDeCompraDetalle_Articulo");

            entity.HasOne(d => d.IdOcNavigation).WithMany(p => p.OrdenDeCompraDetalles)
                .HasForeignKey(d => d.IdOc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenDeCompraDetalle_OrdenDeCompraCabecera");
        });

        modelBuilder.Entity<Personal>(entity =>
        {
            entity.HasKey(e => e.IdPersonal);

            entity.ToTable("Personal", "Maestros");

            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Cargo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Celular)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DocIdentidad)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Especialidad)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EstadoCivil)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Foto).HasColumnType("image");
            entity.Property(e => e.GradoInstruccion)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LugarNacimiento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nacionalidad)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NumeroDocIdentidad)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.Sexo)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TipoEmpleado)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.Personals)
                .HasForeignKey(d => d.IdAlmacen)
                .HasConstraintName("FK_Personal_Almacen");
        });

        modelBuilder.Entity<PlanDeCuenta>(entity =>
        {
            entity.HasKey(e => e.Cuenta).HasName("PK_Maestro.PlanDeCuentas");

            entity.ToTable("PlanDeCuentas", "Maestros");

            entity.Property(e => e.Cuenta)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.AfectaResultado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoCuenta)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor);

            entity.ToTable("Proveedor", "Maestros");

            entity.Property(e => e.Banco)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Cci)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("CCI");
            entity.Property(e => e.Celular)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Contacto)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Cuenta)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dpd)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPD");
            entity.Property(e => e.FormaPago)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NombreComercial)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Pais)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.Telefono)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TipoDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TipoPersona)
                .HasMaxLength(15)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Resuman>(entity =>
        {
            entity.HasKey(e => e.IdResumenFe);

            entity.ToTable("Resumen", "FE");

            entity.Property(e => e.IdResumenFe).HasColumnName("IdResumenFE");
            entity.Property(e => e.DocFin)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DocInicio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.EnvioSunat).HasColumnName("EnvioSUNAT");
            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaReferencia).HasColumnType("datetime");
            entity.Property(e => e.FechaRespuestaSunat)
                .HasColumnType("datetime")
                .HasColumnName("FechaRespuestaSUNAT");
            entity.Property(e => e.Hash)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(8000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Tienda)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ResumenCierreDeCaja>(entity =>
        {
            entity.HasKey(e => e.IdResumen);

            entity.ToTable("ResumenCierreDeCaja", "Movimientos");

            entity.Property(e => e.Detalle).IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.Grupo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.ResumenCierreDeCajas)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResumenCierreDeCaja_Almacen");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ResumenCierreDeCajas)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ResumenCierreDeCaja_Usuario");
        });

        modelBuilder.Entity<SerieCorrelativo>(entity =>
        {
            entity.HasKey(e => new { e.IdSerieCorrelativo, e.IdAlmacen, e.IdTipoDocumentoVenta, e.Serie });

            entity.ToTable("SerieCorrelativo", "Maestros");

            entity.Property(e => e.IdSerieCorrelativo).ValueGeneratedOnAdd();
            entity.Property(e => e.Serie)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.SerieCorrelativos)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SerieCorrelativo_Almacen");

            entity.HasOne(d => d.IdTipoDocumentoVentaNavigation).WithMany(p => p.SerieCorrelativos)
                .HasForeignKey(d => d.IdTipoDocumentoVenta)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SerieCorrelativo_TipoDocumentoVenta");
        });

        modelBuilder.Entity<StockAlmacen>(entity =>
        {
            entity.HasKey(e => new { e.IdAlmacen, e.IdArticulo }).HasName("PK_Stock");

            entity.ToTable("StockAlmacen", "Movimientos");

            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.StockAlmacens)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stock_Almacen");

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.StockAlmacens)
                .HasForeignKey(d => d.IdArticulo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Stock_Articulo");
        });

        modelBuilder.Entity<StockIcabk>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("StockICAbk");

            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<Sunat>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Sunat", "FE");

            entity.Property(e => e.Numero)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TipoDeCambio>(entity =>
        {
            entity.HasKey(e => e.Fecha);

            entity.ToTable("TipoDeCambio", "Maestros");

            entity.Property(e => e.Compra).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Venta).HasColumnType("decimal(18, 3)");
        });

        modelBuilder.Entity<TipoDocumentoVentum>(entity =>
        {
            entity.HasKey(e => e.IdTipoDocumentoVenta);

            entity.ToTable("TipoDocumentoVenta", "Maestros");

            entity.Property(e => e.Abreviatura)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(14)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TipoPagoVentum>(entity =>
        {
            entity.HasKey(e => e.IdTipoPagoVenta);

            entity.ToTable("TipoPagoVenta", "Maestros");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Tipo)
                .HasMaxLength(12)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Todosalmacenes25032022>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("TODOSALMACENES25032022");

            entity.Property(e => e.Codigo).HasColumnName("CODIGO");
            entity.Property(e => e.Precioultimo).HasColumnName("PRECIOULTIMO");
        });

        modelBuilder.Entity<TomaInventario>(entity =>
        {
            entity.HasKey(e => e.IdTomaInventario);

            entity.ToTable("TomaInventario", "Almacen");

            entity.Property(e => e.EstadoToma)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Intervienen)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.IdAlmacenNavigation).WithMany(p => p.TomaInventarios)
                .HasForeignKey(d => d.IdAlmacen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TomaInventario_Almacen");
        });

        modelBuilder.Entity<TomaInventarioDetalle>(entity =>
        {
            entity.HasKey(e => e.IdTomaInventarioDetalle);

            entity.ToTable("TomaInventarioDetalle", "Almacen");

            entity.Property(e => e.IdTomaInventarioDetalle).HasColumnName("idTomaInventarioDetalle");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("idArticulo");
            entity.Property(e => e.IdTomaInventario).HasColumnName("idTomaInventario");

            entity.HasOne(d => d.IdArticuloNavigation).WithMany(p => p.TomaInventarioDetalles)
                .HasForeignKey(d => d.IdArticulo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TomaInventarioDetalle_Articulo");

            entity.HasOne(d => d.IdTomaInventarioNavigation).WithMany(p => p.TomaInventarioDetalles)
                .HasForeignKey(d => d.IdTomaInventario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TomaInventarioDetalle_TomaInventario");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);

            entity.ToTable("Usuario", "Maestros");

            entity.Property(e => e.Clave).HasMaxLength(128);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdPersonalNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdPersonal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuario_Personal");
        });

        modelBuilder.Entity<VAlmacen1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Almacen_1", "Maestros");

            entity.Property(e => e.Abreviacion)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AfectoIgv).HasColumnName("AfectoIGV");
            entity.Property(e => e.Celular)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.Certificado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ClaveSol)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("ClaveSOL");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Dpd)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPD");
            entity.Property(e => e.IdAlmacen).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PasswordCertificado)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.Telefono)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Ubigeo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UsuarioSol)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("UsuarioSOL");
        });

        modelBuilder.Entity<VAlmacenesAperturaFecha1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_AlmacenesAperturaFecha_1", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<VArticulo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Articulos", "Maestros");

            entity.Property(e => e.Color)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Familia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Marca)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Material)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MonedaCosteo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Talla)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.UnidadAlmacen)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VArticulos1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Articulos_1", "Maestros");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnidadAlmacen)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VArticulos2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Articulos_2", "Maestros");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VArticulosBusqueda1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Articulos_Busqueda_1", "Maestros");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Proveedor)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VCierreEnLinea1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_CierreEnLinea_1", "Movimientos");

            entity.Property(e => e.Categoria)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VCierreVentaDiaria1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_CierreVentaDiaria_1", "Movimientos");

            entity.Property(e => e.Datos)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Dolares).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Soles).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vuelto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VClientes1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Clientes_1", "Maestros");

            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.IdCliente).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VComprobante>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Comprobantes", "FE");

            entity.Property(e => e.Apagar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("APagar");
            entity.Property(e => e.EnviadoSunat).HasColumnName("EnviadoSUNAT");
            entity.Property(e => e.Fecha)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaRespuestaSunat)
                .HasColumnType("datetime")
                .HasColumnName("FechaRespuestaSUNAT");
            entity.Property(e => e.Hash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IdFe).HasColumnName("IdFE");
            entity.Property(e => e.Numero)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(5000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Tienda)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoDoc)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.Xml)
                .HasColumnType("xml")
                .HasColumnName("XML");
        });

        modelBuilder.Entity<VConsultasArticulo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ConsultasArticulos");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.FechaVenta)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Monto).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVentaMedio).HasColumnType("decimal(38, 6)");
        });

        modelBuilder.Entity<VDescuento>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Descuentos", "Movimientos");

            entity.Property(e => e.DescripcionCorta)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DescuentoPorc).HasColumnName("Descuento_Porc");
            entity.Property(e => e.IdAlmacen).HasColumnName("idAlmacen");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioFinal).HasColumnName("Precio Final");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VDetalleGuiaRemision1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_DetalleGuiaRemision_1", "Almacen");

            entity.Property(e => e.DescripcionArticulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.UnidadAlmacen)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VDetalleOcparaIngreso1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_DetalleOCParaIngreso_1", "Almacen");

            entity.Property(e => e.DescripcionArticulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Valor).HasColumnType("numeric(14, 2)");
        });

        modelBuilder.Entity<VDetallePagoVenta1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_DetallePagoVenta_1", "Movimientos");

            entity.Property(e => e.Datos)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Dolares).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PorcentajeTarjetaDolares).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PorcentajeTarjetaSoles).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Soles).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vuelto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VDetallePagoVenta2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_DetallePagoVenta_2", "Movimientos");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Dolares).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Soles).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vuelto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VDetallesVenta>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_DetallesVentas", "DW");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Costo).HasColumnType("decimal(31, 2)");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Proveedor)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUC");
            entity.Property(e => e.Total).HasColumnType("decimal(20, 2)");
            entity.Property(e => e.UnidadMedida)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VDocumentosEnviadosSunat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_DocumentosEnviadosSUNAT", "FE");

            entity.Property(e => e.DocFin)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DocInicio)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaEnvio).HasColumnType("datetime");
            entity.Property(e => e.FechaReferencia).HasColumnType("datetime");
            entity.Property(e => e.Hash)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.RespuestaSunat)
                .HasMaxLength(8000)
                .IsUnicode(false)
                .HasColumnName("RespuestaSUNAT");
            entity.Property(e => e.TicketSunat)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("TicketSUNAT");
            entity.Property(e => e.Tienda)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VHistoricoVenta>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_HistoricoVentas", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fecha)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VKardex1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Kardex_1", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Articulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Codigo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Origen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TipoMovimiento)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Valor).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Valorizado).HasColumnType("decimal(31, 2)");
            entity.Property(e => e.ValorizadoFinal).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoInicial).HasColumnType("decimal(29, 2)");
        });

        modelBuilder.Entity<VKardex2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Kardex_2", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Articulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Codigo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Valorizado).HasColumnType("decimal(31, 2)");
            entity.Property(e => e.ValorizadoFinal).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoInicial).HasColumnType("decimal(29, 2)");
        });

        modelBuilder.Entity<VKardex3>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Kardex_3", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Articulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Codigo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Familia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Operacion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorizadoEntrada).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoFinal).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoFinalPc)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("ValorizadoFinalPC");
            entity.Property(e => e.ValorizadoFinalPv)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("ValorizadoFinalPV");
            entity.Property(e => e.ValorizadoInicial).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoSalida).HasColumnType("decimal(29, 2)");
        });

        modelBuilder.Entity<VKardexGeneral>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_KardexGeneral", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Articulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Codigo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Familia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Operacion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorizadoEntrada).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoFinal).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoFinalPc)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("ValorizadoFinalPC");
            entity.Property(e => e.ValorizadoFinalPv)
                .HasColumnType("decimal(29, 2)")
                .HasColumnName("ValorizadoFinalPV");
            entity.Property(e => e.ValorizadoInicial).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.ValorizadoSalida).HasColumnType("decimal(31, 2)");
        });

        modelBuilder.Entity<VListadoAperturaCierre1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoAperturaCierre_1", "Movimientos");

            entity.Property(e => e.Cajero)
                .HasMaxLength(201)
                .IsUnicode(false);
            entity.Property(e => e.Egresos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Ingresos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ObservacionApertura)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.ObservacionCierre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.SaldoFinal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SaldoInicial).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VentaDia).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VListadoComprasAproveedores1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoComprasAProveedores_1", "Movimientos");

            entity.Property(e => e.CostoUnitario).HasColumnType("numeric(14, 2)");
            entity.Property(e => e.DescripcionArticulo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.FechaOc)
                .HasColumnType("datetime")
                .HasColumnName("FechaOC");
            entity.Property(e => e.Glosa)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOc)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NumeroOC");
            entity.Property(e => e.Total).HasColumnType("numeric(12, 2)");
        });

        modelBuilder.Entity<VListadoGr1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoGR_1", "Almacen");

            entity.Property(e => e.AlmacenDestino)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.AlmacenOrigen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DireccionDestino)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DireccionOrigen)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Dpddestino)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPDDestino");
            entity.Property(e => e.Dpdorigen)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("DPDOrigen");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Numero)
                .HasMaxLength(8000)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocialNombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Rucdnice)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUCDNICE");
            entity.Property(e => e.TipoDestinatario)
                .HasMaxLength(9)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VListadoIngresosEgresos1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoIngresosEgresos_1", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Cajero)
                .HasMaxLength(201)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.Glosa)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Naturaleza)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VListadoIngresosEgresosAlmacen1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoIngresosEgresosAlmacen_1", "Almacen");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Motivo)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.Tipo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<VListadoNotaCredito1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoNotaCredito_1", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Comprobante)
                .HasMaxLength(8000)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IdMotivo)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.IdNc).HasColumnName("IdNC");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Referencia)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VListadoOc1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoOC_1", "Movimientos");

            entity.Property(e => e.Estado)
                .HasMaxLength(9)
                .IsUnicode(false);
            entity.Property(e => e.FechaAnulado).HasColumnType("datetime");
            entity.Property(e => e.FechaAtencionTotal).HasColumnType("datetime");
            entity.Property(e => e.FechaEntrega).HasColumnType("datetime");
            entity.Property(e => e.FechaOc)
                .HasColumnType("datetime")
                .HasColumnName("FechaOC");
            entity.Property(e => e.Glosa)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ImporteIgv)
                .HasColumnType("numeric(12, 2)")
                .HasColumnName("ImporteIGV");
            entity.Property(e => e.ImporteSubTotal).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.ImporteTotal).HasColumnType("numeric(12, 2)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NumeroOc)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("NumeroOC");
        });

        modelBuilder.Entity<VListadoPagosAproveedores1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoPagosAProveedores_1", "Movimientos");

            entity.Property(e => e.Almacén)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Banco)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Cuenta)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Detalle)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.Forma)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Glosa)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Imagen).HasColumnType("image");
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VListadoVentaDiaria1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ListadoVentaDiaria_1", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.GeneroNc)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("GeneroNC");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Pago)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VPersonal1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Personal_1", "Maestros");

            entity.Property(e => e.Cargo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Local)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Personal)
                .HasMaxLength(201)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VPersonal2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Personal_2", "Maestros");

            entity.Property(e => e.Cargo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Local)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Personal)
                .HasMaxLength(201)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VPersonal3>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Personal_3", "Maestros");

            entity.Property(e => e.Local)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Personal)
                .HasMaxLength(201)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VProveedores1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Proveedores_1", "Maestros");

            entity.Property(e => e.IdProveedor).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Ruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("RUC");
        });

        modelBuilder.Entity<VRecaudacion3>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Recaudacion_3", "Movimientos");

            entity.Property(e => e.Descripcion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VResumenCierreDeCaja1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ResumenCierreDeCaja_1", "Movimientos");

            entity.Property(e => e.Detalle)
                .HasMaxLength(301)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.Grupo)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VResumenCierreDeCaja2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_ResumenCierreDeCaja_2", "Movimientos");

            entity.Property(e => e.Detalle)
                .HasMaxLength(301)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasColumnType("datetime");
            entity.Property(e => e.Grupo)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.Monto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VSerieCorrelativo1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_SerieCorrelativo_1", "Maestros");

            entity.Property(e => e.DescripcionTipoDoc)
                .HasMaxLength(14)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<VSerieCorrelativo2>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_SerieCorrelativo_2", "Maestros");

            entity.Property(e => e.Serie)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<VSeriesXalmacen1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_SeriesXALmacen_1", "Maestros");

            entity.Property(e => e.DocumentoSerie)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VSeriesXcajero1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_SeriesXCajero_1", "Maestros");

            entity.Property(e => e.DocumentoSerie)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VStockXalmacen1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_StockXAlmacen_1", "Movimientos");

            entity.Property(e => e.Almacen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Familia)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCompra).HasColumnType("decimal(29, 2)");
        });

        modelBuilder.Entity<VUsuarioAperturaAlmacen1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_UsuarioAperturaAlmacen_1", "Movimientos");

            entity.Property(e => e.Cajero)
                .HasMaxLength(201)
                .IsUnicode(false);
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<VVenta1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Venta_1", "Movimientos");

            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.IdComprobante).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VVentasDw>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_VentasDW");

            entity.Property(e => e.Apagar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("APagar");
            entity.Property(e => e.Cliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.DocIdentidad)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.IdArticulo)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Linea)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Tienda)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalProducto).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VVentasXalmacenXmesXanoXproveedor1>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_VentasXAlmacenXMesXAnoXProveedor_1", "Movimientos");

            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.IdComprobante).ValueGeneratedOnAdd();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VentasBak09112020>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("VentasBak09112020");

            entity.Property(e => e.Apagar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("APagar");
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraRegistro).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraUsuarioAnula).HasColumnType("datetime");
            entity.Property(e => e.GeneroNc)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("GeneroNC");
            entity.Property(e => e.IdComprobante).ValueGeneratedOnAdd();
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VentasBk01012019>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("VentasBk01012019");

            entity.Property(e => e.Apagar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("APagar");
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraRegistro).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraUsuarioAnula).HasColumnType("datetime");
            entity.Property(e => e.GeneroNc)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("GeneroNC");
            entity.Property(e => e.IdComprobante).ValueGeneratedOnAdd();
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Xyz>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("xyz");

            entity.Property(e => e.Apagar)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("APagar");
            entity.Property(e => e.Direccion)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Dniruc)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("DNIRUC");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Fecha).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraRegistro).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraUsuarioAnula).HasColumnType("datetime");
            entity.Property(e => e.GeneroNc)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("GeneroNC");
            entity.Property(e => e.IdComprobante).ValueGeneratedOnAdd();
            entity.Property(e => e.Igv)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IGV");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Redondeo).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Serie)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
