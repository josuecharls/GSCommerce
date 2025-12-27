using GSCommerceAPI.Models.SUNAT.DTOs;

namespace GSCommerceAPI.Services.SUNAT;

public interface IGuiaRemisionElectronicaService
{
    Task<(bool exito, string mensaje, int idGuiaRemision)> CrearGuiaRemisionAsync(GuiaRemisionCabeceraDTO guia);
    Task<GuiaRemisionCabeceraDTO?> ObtenerGuiaRemisionAsync(int idGuiaRemision);
    Task<List<GuiaRemisionCabeceraDTO>> ListarGuiasRemisionAsync(int? idAlmacen, DateTime? fechaDesde, DateTime? fechaHasta, string? estadoSunat, int page, int pageSize);
    Task<int> ContarGuiasRemisionAsync(int? idAlmacen, DateTime? fechaDesde, DateTime? fechaHasta, string? estadoSunat);
    Task<(bool exito, string mensaje)> ActualizarGuiaRemisionAsync(GuiaRemisionCabeceraDTO guia);
    Task<(bool exito, string mensaje)> AnularGuiaRemisionAsync(int idGuiaRemision);
    Task<(bool exito, string mensaje, string hash)> GenerarYFirmarGuiaAsync(int idGuiaRemision);
    Task<(bool exito, string ticket, string mensaje)> EnviarGuiaRemisionSunatAsync(int idGuiaRemision);
    Task<(bool exito, string codigo, string mensaje)> ConsultarEstadoSunatAsync(int idGuiaRemision);
    Task<byte[]?> GenerarPdfGuiaAsync(int idGuiaRemision);
    
    /// <summary>
    /// Anula una GRE ya aceptada en SUNAT mediante Comunicación de Baja
    /// </summary>
    Task<(bool exito, string ticket, string mensaje)> AnularGuiaRemisionSunatAsync(int idGuiaRemision, string motivoBaja);
}


