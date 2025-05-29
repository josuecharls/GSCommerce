using System.Net.Http.Json;
using GSCommerce.Client.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace GSCommerce.Client.Services
{
    public class CajaService
    {
        private readonly HttpClient _http;

        public CajaService(HttpClient http)
        {
            _http = http;
        }

        // Verificar si hay apertura pendiente
        public async Task<List<AperturaCierreCajaDTO>?> VerificarAperturaAsync(int idUsuario, int idAlmacen)
        {
            return await _http.GetFromJsonAsync<List<AperturaCierreCajaDTO>>($"api/caja/verificar/{idUsuario}/{idAlmacen}");
        }

        // Obtener apertura por fecha
        public async Task<AperturaCierreCajaDTO?> ObtenerAperturaAsync(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            try
            {
                string fechaStr = fecha.ToString("yyyy-MM-dd");
                var response = await _http.GetAsync($"api/caja/apertura?idUsuario={idUsuario}&idAlmacen={idAlmacen}&fecha={fechaStr}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AperturaCierreCajaDTO>();
                }

                // Manejo específico de errores
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null; // No se encontró apertura
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error al obtener apertura: {errorContent}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción al obtener apertura: {ex.Message}");
                return null;
            }
        }

        // Última apertura anterior
        public async Task<AperturaCierreCajaDTO?> ObtenerAperturaAnteriorAsync(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            return await _http.GetFromJsonAsync<AperturaCierreCajaDTO?>($"api/caja/anterior/{idUsuario}/{idAlmacen}/{fecha}");
        }

        // Siguiente apertura
        public async Task<AperturaCierreCajaDTO?> ObtenerAperturaSiguienteAsync(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            return await _http.GetFromJsonAsync<AperturaCierreCajaDTO?>($"api/caja/siguiente/{idUsuario}/{idAlmacen}/{fecha}");
        }

        // Registrar apertura
        public async Task<bool> RegistrarAperturaAsync(AperturaCierreCajaDTO apertura)
        {
            try
            {
                // Asegurar que los valores requeridos estén presentes
                if (apertura.FondoFijo <= 0 || apertura.IdUsuario <= 0 || apertura.IdAlmacen <= 0)
                    return false;

                var response = await _http.PostAsJsonAsync("api/caja/apertura", apertura);

                if (!response.IsSuccessStatusCode)
                {
                    // Opcional: Leer el mensaje de error
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error al registrar apertura: {error}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción al registrar apertura: {ex.Message}");
                return false;
            }
        }

        // Obtener resumen 1
        public async Task<List<ResumenCierreDeCajaDTO>?> ObtenerResumen1Async(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            return await _http.GetFromJsonAsync<List<ResumenCierreDeCajaDTO>>($"api/caja/resumen1/{idUsuario}/{idAlmacen}/{fecha}");
        }

        // Obtener resumen 2
        public async Task<List<VResumenCierreDeCaja2DTO>?> ObtenerResumen2Async(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            return await _http.GetFromJsonAsync<List<VResumenCierreDeCaja2DTO>>($"api/caja/resumen2/{idUsuario}/{idAlmacen}/{fecha}");
        }

        // Registrar cierre
        public async Task<bool> RegistrarCierreAsync(int id, AperturaCierreCajaDTO cierre)
        {
            /*
            if (string.IsNullOrWhiteSpace(cierre.ObservacionCierre))
            {
                Console.WriteLine("❌ Observación de cierre requerida.");
                return false;
            }
            if (cierre.SaldoFinal == 0)
            {
                Console.WriteLine("❌ Saldo final no puede ser cero.");
                return false;
            }
            */
            var response = await _http.PutAsJsonAsync($"api/caja/cierre/{id}", cierre);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error al cerrar caja: {error}");
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<AperturaCierreCajaDTO?> ObtenerEstadoCajaActual(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            return await ObtenerAperturaAsync(idUsuario, idAlmacen, fecha);
        }

        public async Task<bool> AperturarCaja(AperturaCierreCajaDTO data)
        {
            return await RegistrarAperturaAsync(data);
        }

        public async Task<bool> CerrarCaja(int id, AperturaCierreCajaDTO data)
        {
            return await RegistrarCierreAsync(id, data);
        }

        // Obtener ventas del día
        public async Task<List<VCierreVentaDiaria1DTO>?> ObtenerVentasDiariasAsync(int idAlmacen, DateOnly fecha)
        {
            string fechaStr = fecha.ToString("yyyy-MM-dd");
            return await _http.GetFromJsonAsync<List<VCierreVentaDiaria1DTO>>(
                $"api/caja/ventas/{idAlmacen}/{fechaStr}");
        }

        // Liquidar caja
        public async Task<bool> LiquidarCajaAsync(LiquidacionVentaDTO liquidacion)
        {
            var response = await _http.PostAsJsonAsync("api/caja/liquidar", liquidacion);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Error al liquidar: {error}");
            }
            return response.IsSuccessStatusCode;
        }
    }
}