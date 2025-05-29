using GSCommerceAPI.Models.SUNAT.DTOs;
using System.Threading.Tasks;

namespace GSCommerceAPI.Services.SUNAT
{
    public interface IFacturacionElectronicaService
    {
        Task<(bool exito, string mensaje)> EnviarComprobante(ComprobanteCabeceraDTO comprobante);
        Task<(bool exito, string mensaje)> EnviarResumenDiario(List<ComprobanteCabeceraDTO> comprobantes);
        Task<(bool exito, string mensaje, string hash)> GenerarYFirmarFacturaAsync(ComprobanteCabeceraDTO comprobante, string rutaXml);
        Task<(bool exito, string ticket, string mensaje)> EnviarFacturaAsync(string rutaXmlZip, string usuarioSOL, string claveSOL);
        Task<(bool exito, string ticket, string mensaje)> EnviarResumenDiarioAsync(string rutaXmlZip, string usuarioSOL, string claveSOL);
        Task<(bool exito, string codigoRespuesta, string mensaje)> LeerCdrAsync(string rutaZipRespuesta);
        Task<string> ValidarTicketSunatAsync(string ticket, string rutaArchivo, string usuarioSOL, string claveSOL);
        string ComprimirArchivo(string rutaXml);
    }
}

