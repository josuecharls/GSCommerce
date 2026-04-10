-- Stock independiente por depósito (subalmacén). No modifica Movimientos.StockAlmacen.
IF OBJECT_ID(N'Almacen.StockAlmacenRepuesto', N'U') IS NULL
BEGIN
    CREATE TABLE Almacen.StockAlmacenRepuesto (
        IdStockAlmacenRepuesto INT IDENTITY(1,1) NOT NULL,
        IdAlmacenRepuesto INT NOT NULL,
        IdArticulo CHAR(6) NOT NULL,
        Cantidad INT NOT NULL CONSTRAINT DF_StockAlmacenRepuesto_Cantidad DEFAULT (0),
        CONSTRAINT PK_StockAlmacenRepuesto PRIMARY KEY (IdStockAlmacenRepuesto),
        CONSTRAINT CK_StockAlmacenRepuesto_Cantidad CHECK (Cantidad >= 0),
        CONSTRAINT FK_StockAlmacenRepuesto_AlmacenRepuesto
            FOREIGN KEY (IdAlmacenRepuesto) REFERENCES Almacen.AlmacenRepuesto (IdAlmacenRepuesto),
        CONSTRAINT FK_StockAlmacenRepuesto_Articulo
            FOREIGN KEY (IdArticulo) REFERENCES Maestros.Articulo (IdArticulo)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_StockAlmacenRepuesto_Compuesto
        ON Almacen.StockAlmacenRepuesto (IdAlmacenRepuesto, IdArticulo);
END
GO
