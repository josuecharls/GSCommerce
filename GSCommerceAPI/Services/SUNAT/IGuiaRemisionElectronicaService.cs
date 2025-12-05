using GSCommerceAPI.Models.SUNAT.DTOs;

namespace GSCommerceAPI.Services.SUNAT
{
    public interface IGuiaRemisionElectronicaService
    {
        Task<(bool exito, string mensaje, string hash)> GenerarYFirmarGuiaAsync(GuiaRemisionDTO guia, string rutaXml);
        Task<(bool exito, string ticket, string mensaje)> EnviarGuiaAsync(string rutaXmlZip, string usuarioSOL, string claveSOL);
        Task<string> ValidarTicketGuiaAsync(string ticket, string rutaArchivo, string usuarioSOL, string claveSOL);
        Task<(bool exito, string codigoRespuesta, string mensaje)> LeerCdrGuiaAsync(string rutaZipRespuesta);
        string ComprimirArchivo(string rutaXml);
        Task<(bool exito, string mensaje)> EnviarGuiaCompletaAsync(GuiaRemisionDTO guia);
        Task ActualizarEstadoSunatGuiaAsync(int idGuiaRemision, string codigoRespuesta, string mensajeRespuesta, bool aceptado);
    }
}
