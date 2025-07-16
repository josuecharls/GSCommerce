using System.Net.Http.Json;
using GSCommerce.Client.Models;
using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerce.Client.Models.SUNAT;
using static System.Net.WebRequestMethods;

namespace GSCommerce.Client.Services
{
    public class VentaService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        public VentaService(HttpClient httpClient, AuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task<List<VentaDiariaAlmacenDTO>> ObtenerVentasPorAlmacenDiaAsync(DateOnly fecha)
        {
            string fechaStr = fecha.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetFromJsonAsync<List<VentaDiariaAlmacenDTO>>($"api/ventas/ventas-almacenes-dia?fecha={fechaStr}");
            return response ?? new();
        }


        public async Task<int?> RegistrarVentaYObtenerNumeroAsync(VentaRegistroDTO venta)
        {
            var response = await _httpClient.PostAsJsonAsync("api/ventas", venta);

            if (!response.IsSuccessStatusCode)
                return null;

            // ✅ Espera un JSON como { "numero": 1234 }
            var resultado = await response.Content.ReadFromJsonAsync<NumeroVentaResponse>();

            return resultado?.Numero;
        }
        public async Task<(bool Success, string? ErrorMessage)> RegistrarVentaAsync(VentaRegistroDTO venta)
        {
            var response = await _httpClient.PostAsJsonAsync("api/ventas", venta);
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                return (false, msg);
            }

            var resultado = await response.Content.ReadFromJsonAsync<VentaResponseDTO>();
            if (resultado == null)
                return (false, "Respuesta inválida");

            venta.Cabecera.IdComprobante = resultado.IdComprobante;
            venta.Cabecera.Numero = resultado.Numero;

            return (true, null);
        }

        public async Task<List<ReporteVentasVendedorDTO>> ObtenerReportePorVendedor(DateTime desde, DateTime hasta)
        {
            var response = await _httpClient.GetFromJsonAsync<List<ReporteVentasVendedorDTO>>($"api/ventas/reporte-vendedor?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}");
            return response ?? new();
        }

        public async Task<List<VentaConsultaDTO>> ObtenerVentasAsync(DateTime desde, DateTime hasta)
        {
            try
            {
                string url = $"api/ventas/list?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
                var response = await _httpClient.GetFromJsonAsync<List<VentaConsultaDTO>>(url);
                return response ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar ventas: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> RegistrarYEnviarVentaAsync(VentaRegistroDTO venta)
        {
            var response = await _httpClient.PostAsJsonAsync("api/ventas", venta);

            if (!response.IsSuccessStatusCode)
                return false;

            var resultado = await response.Content.ReadFromJsonAsync<VentaResponseDTO>();

            if (resultado == null)
                return false;

            // ✅ Asignamos el IdComprobante real generado en la BD
            venta.Cabecera.IdComprobante = resultado.IdComprobante;
            venta.Cabecera.Numero = resultado.Numero;

            // ❌ No enviar TICKET a SUNAT
            if (!venta.TipoDocumento.Abreviatura.Equals("TICKET", StringComparison.OrdinalIgnoreCase))
            {
                return await EnviarASUNATAsync(venta.Cabecera, venta.Detalles, venta.TipoDocumento);
            }

            return true;
        }

        public async Task<bool> EnviarASUNATAsync(VentaCabeceraDTO cabecera, List<VentaDetalleDTO> detalles, TipoDocumentoVentaDTO tipoDoc)
        {
            if (_authService == null)
            {
                Console.WriteLine("❌ _authService es NULL en VentaService.");
                throw new Exception("_authService no está inyectado.");
            }
            int? idAlmacen = await _authService.GetUserAlmacenId();
            if (idAlmacen == null)
                return false;

            var almacen = await _httpClient.GetFromJsonAsync<AlmacenDTO>($"api/almacen/{idAlmacen}");

            if (almacen == null)
                return false;

            var comprobante = new ComprobanteCabeceraDTO
            {
                IdComprobante = cabecera.IdComprobante,
                TipoDocumento = tipoDoc.Abreviatura,
                Serie = cabecera.Serie,
                Numero = cabecera.Numero,
                FechaEmision = cabecera.FechaEmision,
                HoraEmision = cabecera.FechaEmision.TimeOfDay,
                Moneda = almacen.Moneda,
                RucEmisor = almacen.Ruc ?? "",
                RazonSocialEmisor = almacen.RazonSocial ?? "",
                DireccionEmisor = almacen.Direccion ?? "",
                UbigeoEmisor = almacen.Ubigeo ?? "",
                DocumentoCliente = cabecera.DocumentoCliente,
                TipoDocumentoCliente = cabecera.DocumentoCliente.Length == 11 ? "6" : "1",
                NombreCliente = cabecera.NombreCliente,
                DireccionCliente = cabecera.DireccionCliente,
                SubTotal = cabecera.SubTotal,
                Igv = cabecera.Igv,
                Total = cabecera.Total,
                MontoLetras = ConvertirMontoALetras(cabecera.Total, almacen.Moneda),

                Detalles = detalles.Select(d => new ComprobanteDetalleDTO
                {
                    Item = d.Item,
                    CodigoItem = d.CodigoItem,
                    DescripcionItem = d.DescripcionItem,
                    Cantidad = d.Cantidad,
                    PrecioUnitarioConIGV = d.PrecioUnitario,
                    PrecioUnitarioSinIGV = Math.Round(d.PrecioUnitario / 1.18m, 2),
                    IGV = Math.Round((d.PrecioUnitario * d.Cantidad) - (d.PrecioUnitario * d.Cantidad / 1.18m), 2),
                }).ToList()
            };

            var response = await _httpClient.PostAsJsonAsync("api/ventas/enviar-sunat", comprobante);
            return response.IsSuccessStatusCode;
        }

        public async Task<VentaDTO?> ObtenerVentaPorId(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<VentaDTO>($"api/ventas/{id}");
            }
            catch
            {
                return null;
            }
        }

        public static string ConvertirMontoALetras(decimal monto, string moneda)
        {
            var enteros = (long)Math.Floor(monto);
            var decimales = (int)Math.Round((monto - enteros) * 100);

            string letras = NumeroALetras(enteros).ToUpper();
            var sufijo = moneda == "USD" ? "DOLARES" : "SOLES";

            return $"{letras} Y {decimales:D2}/100 {sufijo}";
        }

        public static string NumeroALetras(long numero)
        {
            if (numero == 0) return "cero";

            if (numero < 0) return "menos " + NumeroALetras(Math.Abs(numero));

            string[] unidades = { "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" };
            string[] decenas = { "", "diez", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa" };
            string[] centenas = { "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos" };

            if (numero == 100) return "cien";

            string letras = "";

            if ((numero / 1000000) > 0)
            {
                letras += NumeroALetras(numero / 1000000) + ((numero / 1000000) == 1 ? " millón " : " millones ");
                numero %= 1000000;
            }

            if ((numero / 1000) > 0)
            {
                if ((numero / 1000) == 1)
                    letras += "mil ";
                else
                    letras += NumeroALetras(numero / 1000) + " mil ";
                numero %= 1000;
            }

            if ((numero / 100) > 0)
            {
                letras += centenas[numero / 100] + " ";
                numero %= 100;
            }

            if (numero > 0)
            {
                if (numero < 10)
                    letras += unidades[numero];
                else if (numero < 20)
                {
                    string[] especiales = { "diez", "once", "doce", "trece", "catorce", "quince",
                                    "dieciséis", "diecisiete", "dieciocho", "diecinueve" };
                    letras += especiales[numero - 10];
                }
                else
                {
                    letras += decenas[numero / 10];
                    if ((numero % 10) > 0)
                        letras += " y " + unidades[numero % 10];
                }
            }

            return letras.Trim();
        }


        private class NumeroVentaResponse
        {
            public int IdComprobante { get; set; }
            public int Numero { get; set; }
        }
    }
}