using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class AlmacenRepuestoService
    {
        private readonly HttpClient _httpClient;

        public AlmacenRepuestoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AlmacenRepuestoDTO>> GetAlmacenRepuestos()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<AlmacenRepuestoDTO>>("almacenrepuesto");
                return result ?? new List<AlmacenRepuestoDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAlmacenRepuestos(): {ex.Message}");
                return new List<AlmacenRepuestoDTO>();
            }
        }

        public async Task<AlmacenRepuestoDTO?> GetAlmacenRepuestoById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AlmacenRepuestoDTO>($"almacenrepuesto/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<AlmacenRepuestoDTO>> GetPorAlmacen(int idAlmacen)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<AlmacenRepuestoDTO>>($"almacenrepuesto/poralmacen/{idAlmacen}");
                return result ?? new List<AlmacenRepuestoDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetPorAlmacen(): {ex.Message}");
                return new List<AlmacenRepuestoDTO>();
            }
        }

        public async Task<bool> CreateAlmacenRepuesto(AlmacenRepuestoDTO dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("almacenrepuesto", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear almacén repuesto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAlmacenRepuesto(int id, AlmacenRepuestoDTO dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"almacenrepuesto/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando almacén repuesto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAlmacenRepuesto(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"almacenrepuesto/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando almacén repuesto: {ex.Message}");
                return false;
            }
        }
    }
}
