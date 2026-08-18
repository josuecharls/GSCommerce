-- Replica el tipo de cambio del último día registrado (normalmente ayer) hacia la fecha indicada (hoy).
-- Inserta si no existe y actualiza si ya hay un registro (p. ej. mes precargado).
-- Ejecutar una vez en SYSCHARLES antes de habilitar el job de las 6:00 a.m.
IF OBJECT_ID(N'Maestros.usp_InsUpd_TipoCambioDiario', N'P') IS NOT NULL
    DROP PROCEDURE Maestros.usp_InsUpd_TipoCambioDiario;
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Maestros].[usp_InsUpd_TipoCambioDiario]
    @Fecha DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Hoy DATE = ISNULL(@Fecha, CAST(GETDATE() AS DATE));
    DECLARE @FechaOrigen DATE;
    DECLARE @Compra DECIMAL(18, 3);
    DECLARE @Venta DECIMAL(18, 3);

    SELECT TOP (1)
        @FechaOrigen = Fecha,
        @Compra = Compra,
        @Venta = Venta
    FROM Maestros.TipoDeCambio
    WHERE Fecha < @Hoy
    ORDER BY Fecha DESC;

    IF @FechaOrigen IS NULL
    BEGIN
        SELECT
            CAST(N'SIN_ORIGEN' AS VARCHAR(20)) AS Accion,
            CAST(NULL AS DATE) AS FechaOrigen,
            @Hoy AS FechaDestino,
            CAST(NULL AS DECIMAL(18, 3)) AS Compra,
            CAST(NULL AS DECIMAL(18, 3)) AS Venta;
        RETURN;
    END

    DECLARE @Accion TABLE (Accion NVARCHAR(10) NOT NULL);

    MERGE Maestros.TipoDeCambio AS destino
    USING (
        SELECT
            @Hoy AS Fecha,
            @Compra AS Compra,
            @Venta AS Venta
    ) AS origen
        ON destino.Fecha = origen.Fecha
    WHEN MATCHED THEN
        UPDATE SET
            Compra = origen.Compra,
            Venta = origen.Venta
    WHEN NOT MATCHED THEN
        INSERT (Fecha, Compra, Venta)
        VALUES (origen.Fecha, origen.Compra, origen.Venta)
    OUTPUT $action INTO @Accion (Accion);

    SELECT
        CAST(
            CASE
                WHEN Accion = N'INSERT' THEN N'INSERTADO'
                ELSE N'ACTUALIZADO'
            END AS VARCHAR(20)
        ) AS Accion,
        @FechaOrigen AS FechaOrigen,
        @Hoy AS FechaDestino,
        @Compra AS Compra,
        @Venta AS Venta
    FROM @Accion;
END
GO
