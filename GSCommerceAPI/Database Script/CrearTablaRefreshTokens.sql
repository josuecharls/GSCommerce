-- Script para crear la tabla de RefreshTokens
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RefreshTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [UserId] INT NOT NULL,
        [Token] NVARCHAR(500) NOT NULL,
        [ExpiresAt] DATETIME2 NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [FK_RefreshTokens_Usuarios] FOREIGN KEY ([UserId]) REFERENCES [Maestros].[Usuario]([IdUsuario]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens]([UserId]);
    CREATE INDEX [IX_RefreshTokens_Token] ON [dbo].[RefreshTokens]([Token]);
    CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [dbo].[RefreshTokens]([ExpiresAt]);
END
GO

-- Limpiar tokens expirados (ejecutar periódicamente)
DELETE FROM [dbo].[RefreshTokens] WHERE [ExpiresAt] < GETUTCDATE();
GO