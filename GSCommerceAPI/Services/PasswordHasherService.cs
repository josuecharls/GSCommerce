using BCrypt.Net;

namespace GSCommerceAPI.Services
{
    public class PasswordHasherService
    {
        /// <summary>
        /// Genera un hash de la contraseña usando BCrypt
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        /// <summary>
        /// Verifica si la contraseña proporcionada coincide con el hash almacenado
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                // Si el hash no es válido (formato incorrecto), retornar false
                return false;
            }
        }

        /// <summary>
        /// Verifica si una contraseña necesita ser rehasheada (por ejemplo, si se usó un algoritmo antiguo)
        /// </summary>
        public bool NeedsRehash(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
                return true;

            // Verificar si el hash es de BCrypt (empieza con $2a$, $2b$, $2y$)
            if (!hashedPassword.StartsWith("$2"))
                return true;

            // Extraer el work factor del hash manualmente
            // Formato: $2a$12$... donde 12 es el work factor
            try
            {
                var parts = hashedPassword.Split('$');
                if (parts.Length >= 3 && int.TryParse(parts[2], out int workFactor))
                {
                    return workFactor < 12;
                }
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
