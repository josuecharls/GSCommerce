-- Script para actualizar el stored procedure usp_set_password para aceptar hashes BCrypt (hasta 200 caracteres)
-- Los hashes BCrypt tienen aproximadamente 60 caracteres, pero dejamos margen

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_set_password]') AND type in (N'P', N'PC'))
BEGIN
    DROP PROCEDURE [dbo].[usp_set_password]
END
GO

CREATE PROC [dbo].[usp_set_password]
@password		varchar(200),  -- Aumentado de 50 a 200 para soportar hashes BCrypt
@IdUsuario		int
AS

OPEN SYMMETRIC KEY SYS_Key_01  
   DECRYPTION BY CERTIFICATE Usuarios001;  

UPDATE [Maestros].[Usuario] 
SET Clave = EncryptByKey(Key_GUID('SYS_Key_01')  
    , @password, 1, HashBytes('SHA2_256', CONVERT( varbinary  
    , IdUsuario)))
WHERE IdUsuario = @IdUsuario;
GO














