-- Script para crear la tabla de Notificaciones
-- Ejecutar este script en la base de datos

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Maestros].[Notificacion]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Maestros].[Notificacion] (
        [IdNotificacion] INT IDENTITY(1,1) PRIMARY KEY,
        [IdUsuario] INT NOT NULL,
        [IdMovimiento] INT NOT NULL,
        [Tipo] NVARCHAR(50) NOT NULL DEFAULT 'TRANSFERENCIA',
        [Mensaje] NVARCHAR(500) NOT NULL,
        [Leida] BIT NOT NULL DEFAULT 0,
        [FechaCreacion] DATETIME NOT NULL DEFAULT GETDATE(),
        [FechaLectura] DATETIME NULL,
        
        CONSTRAINT [FK_Notificacion_Usuario] 
            FOREIGN KEY ([IdUsuario]) 
            REFERENCES [Maestros].[Usuario]([IdUsuario]),
        CONSTRAINT [FK_Notificacion_Movimiento] 
            FOREIGN KEY ([IdMovimiento]) 
            REFERENCES [Almacen].[MovimientosCabecera]([IdMovimiento])
    );

    -- Crear índice para mejorar las consultas
    CREATE INDEX IX_Notificacion_IdUsuario_Leida 
        ON [Maestros].[Notificacion]([IdUsuario], [Leida]);
    
    CREATE INDEX IX_Notificacion_FechaCreacion 
        ON [Maestros].[Notificacion]([FechaCreacion] DESC);

    PRINT 'Tabla Notificacion creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla Notificacion ya existe.';
END


