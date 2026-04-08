USE SYSCHARLESTEST;
GO

ALTER VIEW [DW].[v_DetallesVentas]
AS
SELECT 
    alm.IdAlmacen, 
    alm.Nombre AS Almacen, 
    c.Fecha, 
    d.IdComprobante,
    d.Item,
    d.IdArticulo,
    d.IdVariante,
    d.Descripcion,
    d.UnidadMedida,
    d.Cantidad,
    d.Precio,
    d.PorcentajeDescuento,
    d.Total,
    a.PrecioCompra, 
    d.Cantidad * a.PrecioCompra AS Costo, 
    p.RUC, 
    p.Nombre AS Proveedor
FROM Movimientos.ComprobanteDeVentaCabecera c 
INNER JOIN Movimientos.ComprobanteDeVentaDetalle d ON c.IdComprobante = d.IdComprobante 
INNER JOIN Maestros.Articulo a ON d.IdArticulo = a.IdArticulo 
INNER JOIN Maestros.Proveedor p ON a.[IdProveedor] = p.IdProveedor 
INNER JOIN Maestros.Almacen alm ON c.idAlmacen = alm.idAlmacen
WHERE c.Estado = 'E'

UNION ALL

SELECT 
    alm.IdAlmacen, 
    alm.Nombre AS Almacen, 
    c.Fecha, 
    c.IdNC, 
    d.Item, 
    a.IdArticulo,
    d.IdVariante,
    a.Descripcion, 
    d.UnidadMedida, 
    d.Cantidad, 
    d.Precio, 
    d.PorcentajeDescuento, 
	d.Total * -1 AS Total, 
    a.PrecioCompra, 
    d.Cantidad * a.PrecioCompra * -1 AS Costo, 
    p.RUC, 
    p.Nombre AS Proveedor
FROM Movimientos.NotaDeCreditoCabecera c 
INNER JOIN Movimientos.NotaDeCreditoDetalle d ON c.IdNC = d.IdNC 
INNER JOIN Maestros.Articulo a ON d.IdArticulo = a.IdArticulo 
INNER JOIN Maestros.Proveedor p ON a.[IdProveedor] = p.IdProveedor 
INNER JOIN Maestros.Almacen alm ON c.idAlmacen = alm.idAlmacen
WHERE c.Estado = 'E';
GO
