using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GSCommerceAPI.Services.SUNAT;
using GSCommerceAPI.Models.SUNAT.DTOs;
using QuestPDF.Fluent;

namespace GSCommerceAPI.Controllers;

[ApiController]
[Route("api/guias-remision-sunat")]
[Authorize(Roles = "ADMINISTRADOR,ALMACEN")]
public class GuiaRemisionSunatController : ControllerBase
{
    private readonly IGuiaRemisionElectronicaService _guiaService;

    public GuiaRemisionSunatController(IGuiaRemisionElectronicaService guiaService)
    {
        _guiaService = guiaService;
    }

    /// <summary>
    /// Listar guías de remisión con filtros y paginación
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarGuias(
        [FromQuery] int? idAlmacen,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] string? estadoSunat,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var guias = await _guiaService.ListarGuiasRemisionAsync(
            idAlmacen, fechaDesde, fechaHasta, estadoSunat, page, pageSize);

        var total = await _guiaService.ContarGuiasRemisionAsync(
            idAlmacen, fechaDesde, fechaHasta, estadoSunat);

        return Ok(new
        {
            Data = guias,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Obtener guía de remisión por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerGuia(int id)
    {
        var guia = await _guiaService.ObtenerGuiaRemisionAsync(id);

        if (guia == null)
            return NotFound(new { mensaje = "Guía de remisión no encontrada" });

        return Ok(guia);
    }

    /// <summary>
    /// Crear nueva guía de remisión
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CrearGuia([FromBody] GuiaRemisionCabeceraDTO dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (exito, mensaje, idGuiaRemision) = await _guiaService.CrearGuiaRemisionAsync(dto);

        if (!exito)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje, idGuiaRemision });
    }

    /// <summary>
    /// Actualizar guía de remisión existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarGuia(int id, [FromBody] GuiaRemisionCabeceraDTO dto)
    {
        if (id != dto.IdGuiaRemision)
            return BadRequest(new { mensaje = "ID no coincide" });

        var (exito, mensaje) = await _guiaService.ActualizarGuiaRemisionAsync(dto);

        if (!exito)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje });
    }

    /// <summary>
    /// Anular guía de remisión (local, no enviada a SUNAT)
    /// </summary>
    [HttpPut("{id}/anular")]
    public async Task<IActionResult> AnularGuia(int id)
    {
        var (exito, mensaje) = await _guiaService.AnularGuiaRemisionAsync(id);

        if (!exito)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje });
    }

    /// <summary>
    /// Anular guía de remisión en SUNAT (Comunicación de Baja)
    /// Para guías ya aceptadas por SUNAT
    /// </summary>
    [HttpPost("{id}/anular-sunat")]
    public async Task<IActionResult> AnularGuiaSunat(int id, [FromBody] AnularGuiaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.MotivoBaja))
            return BadRequest(new { mensaje = "Debe indicar el motivo de la baja" });

        var (exito, ticket, mensaje) = await _guiaService.AnularGuiaRemisionSunatAsync(id, request.MotivoBaja);

        if (!exito)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje, ticket });
    }

    /// <summary>
    /// Generar XML y firmar digitalmente
    /// </summary>
    [HttpPost("{id}/generar-xml")]
    public async Task<IActionResult> GenerarXml(int id)
    {
        var (exito, mensaje, hash) = await _guiaService.GenerarYFirmarGuiaAsync(id);

        if (!exito)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje, hash });
    }

    /// <summary>
    /// Enviar guía a SUNAT
    /// </summary>
    [HttpPost("{id}/enviar-sunat")]
    public async Task<IActionResult> EnviarSunat(int id)
    {
        var (exito, ticket, mensaje) = await _guiaService.EnviarGuiaRemisionSunatAsync(id);

        if (!exito)
            return BadRequest(new { mensaje });

        return Ok(new { mensaje, ticket });
    }

    /// <summary>
    /// Consultar estado en SUNAT
    /// </summary>
    [HttpGet("{id}/estado-sunat")]
    public async Task<IActionResult> ConsultarEstado(int id)
    {
        var (exito, codigo, mensaje) = await _guiaService.ConsultarEstadoSunatAsync(id);

        return Ok(new { exito, codigo, mensaje });
    }

    /// <summary>
    /// Obtener catálogos SUNAT
    /// </summary>
    [HttpGet("catalogos/motivos-traslado")]
    [AllowAnonymous]
    public IActionResult ObtenerMotivosTraslado()
    {
        return Ok(CatalogoMotivoTraslado.Motivos.Select(m => new { codigo = m.Key, descripcion = m.Value }));
    }

    [HttpGet("catalogos/modalidades-transporte")]
    [AllowAnonymous]
    public IActionResult ObtenerModalidadesTransporte()
    {
        return Ok(CatalogoModalidadTransporte.Modalidades.Select(m => new { codigo = m.Key, descripcion = m.Value }));
    }

    [HttpGet("catalogos/tipos-documento")]
    [AllowAnonymous]
    public IActionResult ObtenerTiposDocumento()
    {
        return Ok(CatalogoTipoDocumentoIdentidad.Tipos.Select(t => new { codigo = t.Key, descripcion = t.Value }));
    }

    /// <summary>
    /// Generar y descargar PDF de la guía de remisión
    /// </summary>
    [HttpGet("{id}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> DescargarPdf(int id)
    {
        try
        {
            var guia = await _guiaService.ObtenerGuiaRemisionAsync(id);
            
            if (guia == null)
                return NotFound(new { mensaje = "Guía de remisión no encontrada" });

            // Generar el PDF
            var document = new GuiaRemisionDocument(guia);
            var pdf = document.GeneratePdf();

            var nombreArchivo = $"GRE_{guia.Serie}-{guia.Numero:D8}.pdf";
            
            return File(pdf, "application/pdf", nombreArchivo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = $"Error al generar PDF: {ex.Message}" });
        }
    }
}

/// <summary>
/// Request para anular una GRE en SUNAT
/// </summary>
public class AnularGuiaRequest
{
    public string MotivoBaja { get; set; } = "";
}


