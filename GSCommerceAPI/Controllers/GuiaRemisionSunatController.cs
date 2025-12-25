using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GSCommerceAPI.Services.SUNAT;
using GSCommerceAPI.Models.SUNAT.DTOs;

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
    /// Anular guía de remisión
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
}


