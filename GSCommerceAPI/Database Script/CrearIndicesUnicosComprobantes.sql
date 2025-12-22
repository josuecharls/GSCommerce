-- Script para crear índices únicos filtrados que previenen comprobantes duplicados
-- Estos índices garantizan que no existan dos comprobantes activos con la misma serie y número en un almacén
-- Los comprobantes anulados (Estado = 'A') pueden tener números duplicados

-- Índice único para ComprobanteDeVentaCabecera
-- Previene duplicados de serie y número por almacén para comprobantes activos
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ComprobanteDeVentaCabecera_Serie_Numero_Almacen' AND object_id = OBJECT_ID('Movimientos.ComprobanteDeVentaCabecera'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_ComprobanteDeVentaCabecera_Serie_Numero_Almacen
    ON Movimientos.ComprobanteDeVentaCabecera (IdAlmacen, Serie, Numero)
    WHERE Estado != 'A'; -- Solo para comprobantes no anulados
    PRINT 'Índice IX_ComprobanteDeVentaCabecera_Serie_Numero_Almacen creado exitosamente.';
END
ELSE
BEGIN
    PRINT 'El índice IX_ComprobanteDeVentaCabecera_Serie_Numero_Almacen ya existe.';
END
GO

-- Índice único para NotaDeCreditoCabecera
-- Previene duplicados de serie y número por almacén y tipo de documento para notas de crédito activas
-- Permite que boletas y facturas compartan la misma serie y número en el mismo almacén
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NotaDeCreditoCabecera_Serie_Numero_Almacen' AND object_id = OBJECT_ID('Movimientos.NotaDeCreditoCabecera'))
BEGIN
    DROP INDEX IX_NotaDeCreditoCabecera_Serie_Numero_Almacen ON Movimientos.NotaDeCreditoCabecera;
    PRINT 'Índice anterior IX_NotaDeCreditoCabecera_Serie_Numero_Almacen eliminado.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NotaDeCreditoCabecera_Serie_Numero_Almacen_TipoDoc' AND object_id = OBJECT_ID('Movimientos.NotaDeCreditoCabecera'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_NotaDeCreditoCabecera_Serie_Numero_Almacen_TipoDoc
    ON Movimientos.NotaDeCreditoCabecera (IdAlmacen, IdTipoDocumento, Serie, Numero)
    WHERE Estado != 'A'; -- Solo para notas no anuladas
    PRINT 'Índice IX_NotaDeCreditoCabecera_Serie_Numero_Almacen_TipoDoc creado exitosamente.';
END
ELSE
BEGIN
    PRINT 'El índice IX_NotaDeCreditoCabecera_Serie_Numero_Almacen_TipoDoc ya existe.';
END
GO
