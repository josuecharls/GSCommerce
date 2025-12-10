-- Script de migración para hashear contraseñas existentes con BCrypt
-- IMPORTANTE: Este script debe ejecutarse DESPUÉS de desplegar el código que usa BCrypt
-- Las contraseñas se hashean desde la aplicación, no desde SQL Server
-- Este script es solo una referencia - la migración real debe hacerse desde la aplicación

-- NOTA: Este proceso debe hacerse desde la aplicación C# porque:
-- 1. BCrypt es una librería .NET, no está disponible en SQL Server
-- 2. Necesitamos desencriptar las contraseñas actuales, hashearlas con BCrypt, y guardarlas

-- Proceso recomendado:
-- 1. Crear un endpoint temporal [Authorize(Roles = "ADMINISTRADOR")] para migración
-- 2. Leer todas las contraseñas desencriptadas usando usp_get_password
-- 3. Hashear cada una con BCrypt
-- 4. Guardar el hash usando usp_set_password actualizado
-- 5. Eliminar el endpoint de migración después de completar

-- Ejemplo de código C# para migración (ejecutar una sola vez):
/*
var usuarios = await _context.Usuarios.ToListAsync();
foreach (var usuario in usuarios)
{
    var passwordActual = await GetPasswordFromDB(usuario.Nombre);
    if (!string.IsNullOrEmpty(passwordActual))
    {
        // Verificar si ya es un hash BCrypt (empieza con $2)
        if (!passwordActual.StartsWith("$2"))
        {
            var hashedPassword = _passwordHasher.HashPassword(passwordActual);
            // Guardar el hash
            await SetPasswordHash(usuario.IdUsuario, hashedPassword);
        }
    }
}
*/







