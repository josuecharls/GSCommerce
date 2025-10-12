using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;
using static System.Net.WebRequestMethods;

namespace GSCommerce.Client.Services
{
    public class PersonalService
    {
        private readonly HttpClient _httpClient;

        public PersonalService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PersonalResponse> GetPersonalList(int page, int pageSize, string search = "", bool incluirInactivos = false)
        {
            var url = $"api/personal?page={page}&pageSize={pageSize}&search={search}";
            if (incluirInactivos)
            {
                url += "&incluirInactivos=true";
            }
            
            var response = await _httpClient.GetFromJsonAsync<PersonalResponse>(url);

            return response ?? new PersonalResponse
            {
                TotalItems = 0,
                TotalPages = 0,
                Data = []
            };
        }

        public async Task<PersonalDTO?> GetPersonalById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<PersonalDTO>($"api/personal/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<bool> CreatePersonal(PersonalDTO personal)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(personal);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/personal", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<PersonalDTO>> ObtenerVendedoresPorAlmacen(int idAlmacen)
        {
            return await _httpClient.GetFromJsonAsync<List<PersonalDTO>>($"api/personal/vendedores-por-almacen/{idAlmacen}")
                ?? new List<PersonalDTO>();
        }

        public async Task<bool> UpdatePersonal(int id, PersonalDTO personal)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/personal/{id}", personal);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando personal: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeletePersonal(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/personal/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> HabilitarPersonal(int id)
        {
            var response = await _httpClient.PutAsync($"api/personal/{id}/habilitar", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdatePhoto(int id, PersonalDTO personal)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/personal/UploadFoto/{id}", personal);
            return response.IsSuccessStatusCode;
        }
    }

    public class PersonalResponse
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public required List<PersonalDTO> Data { get; set; }
    }
}