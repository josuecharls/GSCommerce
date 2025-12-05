using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Models.SUNAT.DTOs;
using GSCommerceAPI.Services.SUNAT;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/guia-remision-electronica")]
    [Authorize(Roles = "ADMINISTRADOR,CAJERO,ALMACEN")]
    public class GuiaRemisionElectronicaController : ControllerBase
    {
        private readonly IGuiaRemisionElectronicaService _guiaService;
        private readonly SyscharlesContext _context;
        private readonly IWebHostEnvironment _env;

        public GuiaRemisionElectronicaController(
            IGuiaRemisionElectronicaService guiaService,
            SyscharlesContext context,
            IWebHostEnvironment env)
        {
            _guiaService = guiaService;
            _context = context;
            _env = env;
        }

        [HttpPost("generar")]
        public async Task<IActionResult> GenerarXmlGuia([FromBody] GuiaRemisionDTO guia)
        {
            try
            {
                // Validar serie
                if (string.IsNullOrWhiteSpace(guia.Serie) || !guia.Serie.StartsWith("T", StringComparison.OrdinalIgnoreCase) || guia.Serie.Length != 4)
                {
                    return BadRequest(new { mensaje = "La serie debe comenzar con 'T' y tener 4 caracteres" });
                }

                string nombreArchivo = $"{guia.RucRemitente}-09-{guia.Serie}-{guia.Numero:D8}.xml";
                string rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

                var resultado = await _guiaService.GenerarYFirmarGuiaAsync(guia, rutaXml);

                if (!resultado.exito)
                    return BadRequest(new { mensaje = resultado.mensaje });

                return Ok(new
                {
                    mensaje = resultado.mensaje,
                    hash = resultado.hash,
                    ruta = nombreArchivo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        [HttpPost("enviar")]
        public async Task<IActionResult> EnviarGuia([FromBody] GuiaRemisionDTO guia)
        {
            try
            {
                var resultado = await _guiaService.EnviarGuiaCompletaAsync(guia);

                if (resultado.exito)
                    return Ok(new { mensaje = resultado.mensaje });

                return BadRequest(new { mensaje = resultado.mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al enviar guía: {ex.Message}" });
            }
        }

        [HttpPost("consultar-ticket/{idGuiaRemision}")]
        public async Task<IActionResult> ConsultarTicket(int idGuiaRemision, [FromBody] ConsultarTicketRequest request)
        {
            try
            {
                var guia = await _context.GuiaRemisionCabeceras
                    .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

                if (guia == null)
                    return NotFound(new { mensaje = "Guía no encontrada" });

                var almacen = await _context.Almacens
                    .FirstOrDefaultAsync(a => a.IdAlmacen == guia.IdAlmacen);

                if (almacen == null)
                    return BadRequest(new { mensaje = "Almacén no encontrado" });

                var (usuarioSOL, claveSOL) = await ObtenerCredencialesSunatAsync(almacen.Ruc ?? "");

                if (string.IsNullOrWhiteSpace(usuarioSOL))
                    return BadRequest(new { mensaje = "Credenciales SUNAT no configuradas" });

                string nombreArchivo = $"{almacen.Ruc}-09-{guia.Serie}-{guia.Numero:D8}.zip";
                string rutaArchivo = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

                var resultado = await _guiaService.ValidarTicketGuiaAsync(request.Ticket, rutaArchivo, usuarioSOL, claveSOL);

                // Leer CDR y actualizar estado
                var (exito, codigo, mensaje) = await _guiaService.LeerCdrGuiaAsync(rutaArchivo);
                if (exito || !string.IsNullOrWhiteSpace(codigo))
                {
                    await _guiaService.ActualizarEstadoSunatGuiaAsync(idGuiaRemision, codigo, mensaje, exito);
                }

                return Ok(new { mensaje = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al consultar ticket: {ex.Message}" });
            }
        }

        [HttpGet("{idGuiaRemision}")]
        public async Task<IActionResult> ObtenerGuia(int idGuiaRemision)
        {
            try
            {
                var guia = await _context.GuiaRemisionCabeceras
                    .Include(g => g.GuiaRemisionDetalles)
                    .Include(g => g.GuiaRemisionSunat)
                    .Include(g => g.IdMovimientoNavigation)
                    .ThenInclude(m => m.IdAlmacenNavigation)
                    .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

                if (guia == null)
                    return NotFound(new { mensaje = "Guía no encontrada" });

                var almacen = guia.IdMovimientoNavigation?.IdAlmacenNavigation;
                if (almacen == null)
                    return BadRequest(new { mensaje = "Almacén no encontrado" });

                var dto = MapearAGuiaRemisionDTO(guia, almacen);

                // Agregar datos de SUNAT si existen
                if (guia.GuiaRemisionSunat != null)
                {
                    dto.EnviadoSunat = guia.GuiaRemisionSunat.EnviadoSunat ?? false;
                    dto.TicketSunat = guia.GuiaRemisionSunat.TicketSunat;
                    dto.FechaEnvio = guia.GuiaRemisionSunat.FechaEnvio;
                    dto.FechaRespuestaSunat = guia.GuiaRemisionSunat.FechaRespuestaSunat;
                    dto.RespuestaSunat = guia.GuiaRemisionSunat.RespuestaSunat;
                    dto.EstadoSunat = guia.GuiaRemisionSunat.EstadoSunat;
                    dto.CodigoRespuestaSunat = guia.GuiaRemisionSunat.CodigoRespuestaSunat;
                    dto.MensajeRespuestaSunat = guia.GuiaRemisionSunat.MensajeRespuestaSunat;
                    dto.Hash = guia.GuiaRemisionSunat.Hash;
                }

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener guía: {ex.Message}" });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListarGuias(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? idAlmacen = null)
        {
            try
            {
                var query = _context.GuiaRemisionCabeceras
                    .Include(g => g.GuiaRemisionSunat)
                    .Include(g => g.IdMovimientoNavigation)
                    .ThenInclude(m => m.IdAlmacenNavigation)
                    .AsQueryable();

                if (idAlmacen.HasValue && idAlmacen.Value > 0)
                {
                    query = query.Where(g => g.IdAlmacen == idAlmacen.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(g => 
                        g.Serie.Contains(search) || 
                        g.RazonSocialNombre.Contains(search) ||
                        g.Rucdnice.Contains(search));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                var guias = await query
                    .OrderByDescending(g => g.IdGuiaRemision)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var dtos = guias.Select(g =>
                {
                    var almacen = g.IdMovimientoNavigation?.IdAlmacenNavigation;
                    return new
                    {
                        g.IdGuiaRemision,
                        g.IdMovimiento,
                        g.Serie,
                        g.Numero,
                        g.Fecha,
                        g.RazonSocialNombre,
                        g.Rucdnice,
                        g.Estado,
                        Almacen = almacen?.Nombre,
                        EstadoSunat = g.GuiaRemisionSunat != null ? g.GuiaRemisionSunat.EstadoSunat : null,
                        EnviadoSunat = g.GuiaRemisionSunat != null ? (g.GuiaRemisionSunat.EnviadoSunat ?? false) : false,
                        FechaEnvio = g.GuiaRemisionSunat != null ? g.GuiaRemisionSunat.FechaEnvio : null
                    };
                }).ToList();

                return Ok(new
                {
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Data = dtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al listar guías: {ex.Message}" });
            }
        }

        [HttpPost("crear-desde-movimiento/{idMovimiento}")]
        public async Task<IActionResult> CrearGuiaDesdeMovimiento(int idMovimiento, [FromBody] GuiaRemisionDTO guiaDTO)
        {
            try
            {
                var movimiento = await _context.MovimientosCabeceras
                    .Include(m => m.MovimientosDetalles)
                    .Include(m => m.IdAlmacenNavigation)
                    .FirstOrDefaultAsync(m => m.IdMovimiento == idMovimiento);

                if (movimiento == null)
                    return NotFound(new { mensaje = "Movimiento no encontrado" });

                // Obtener o asignar serie y número
                if (string.IsNullOrWhiteSpace(guiaDTO.Serie))
                {
                    guiaDTO.Serie = await ObtenerSerieGuiaAsync(movimiento.IdAlmacen);
                }

                if (guiaDTO.Numero == 0)
                {
                    guiaDTO.Numero = await ObtenerSiguienteNumeroAsync(guiaDTO.Serie, movimiento.IdAlmacen);
                }

                // Mapear datos del movimiento a la guía
                var almacen = movimiento.IdAlmacenNavigation;
                if (almacen == null)
                    return BadRequest(new { mensaje = "Almacén no encontrado" });

                // Completar datos del remitente desde almacén
                guiaDTO.RucRemitente = almacen.Ruc ?? "";
                guiaDTO.RazonSocialRemitente = almacen.RazonSocial ?? "";
                guiaDTO.DireccionRemitente = almacen.Direccion ?? "";
                guiaDTO.UbigeoRemitente = almacen.Ubigeo ?? "";

                // Parsear DPD
                var dpdParts = (almacen.Dpd ?? "").Split(new[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);
                guiaDTO.DistritoRemitente = dpdParts.Length > 0 ? dpdParts[0].Trim() : "";
                guiaDTO.ProvinciaRemitente = dpdParts.Length > 1 ? dpdParts[1].Trim() : "";
                guiaDTO.DepartamentoRemitente = dpdParts.Length > 2 ? dpdParts[2].Trim() : "";

                // Completar detalles desde movimiento
                if (guiaDTO.Detalles == null || !guiaDTO.Detalles.Any())
                {
                    guiaDTO.Detalles = movimiento.MovimientosDetalles.Select((d, index) => new GuiaRemisionDetalleDTO
                    {
                        Item = index + 1,
                        CodigoItem = d.IdArticulo,
                        DescripcionItem = d.DescripcionArticulo,
                        Cantidad = d.Cantidad,
                        UnidadMedida = "NIU"
                    }).ToList();
                }

                guiaDTO.IdMovimiento = idMovimiento;
                guiaDTO.FechaEmision = movimiento.Fecha.HasValue ? movimiento.Fecha.Value.ToDateTime(TimeOnly.MinValue) : DateTime.Now;
                guiaDTO.HoraEmision = guiaDTO.FechaEmision.TimeOfDay;

                // Crear registro en BD
                var guiaRemision = new GuiaRemisionCabecera
                {
                    IdMovimiento = idMovimiento,
                    Serie = guiaDTO.Serie,
                    Numero = guiaDTO.Numero,
                    Fecha = guiaDTO.FechaEmision,
                    DireccionOrigen = guiaDTO.DireccionOrigen,
                    Dpdorigen = guiaDTO.UbigeoOrigen,
                    TipoDestinatario = guiaDTO.TipoDocumentoDestinatario,
                    RazonSocialNombre = guiaDTO.RazonSocialDestinatario,
                    Rucdnice = guiaDTO.NumeroDocumentoDestinatario,
                    DireccionDestino = guiaDTO.DireccionDestino,
                    Dpddestino = guiaDTO.UbigeoDestino,
                    Observaciones = guiaDTO.Observaciones,
                    IdUsuario = movimiento.IdUsuario,
                    Estado = "E",
                    IdAlmacen = movimiento.IdAlmacen
                };

                _context.GuiaRemisionCabeceras.Add(guiaRemision);
                await _context.SaveChangesAsync();

                // Agregar detalles
                foreach (var detalle in guiaDTO.Detalles)
                {
                    _context.GuiaRemisionDetalles.Add(new GuiaRemisionDetalle
                    {
                        IdGuiaRemision = guiaRemision.IdGuiaRemision,
                        Item = detalle.Item,
                        IdArticulo = detalle.CodigoItem,
                        DescripcionArticulo = detalle.DescripcionItem,
                        Cantidad = (int)detalle.Cantidad,
                        UnidadMedida = detalle.UnidadMedida
                    });
                }

                await _context.SaveChangesAsync();

                guiaDTO.IdGuiaRemision = guiaRemision.IdGuiaRemision;

                return Ok(new { mensaje = "Guía creada correctamente", guia = guiaDTO });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al crear guía: {ex.Message}" });
            }
        }

        private GuiaRemisionDTO MapearAGuiaRemisionDTO(GuiaRemisionCabecera guia, Almacen almacen)
        {
            var dpdParts = (almacen.Dpd ?? "").Split(new[] { '-', ',' }, StringSplitOptions.RemoveEmptyEntries);

            return new GuiaRemisionDTO
            {
                IdGuiaRemision = guia.IdGuiaRemision,
                IdMovimiento = guia.IdMovimiento,
                Serie = guia.Serie ?? "",
                Numero = guia.Numero ?? 0,
                FechaEmision = guia.Fecha ?? DateTime.Now,
                HoraEmision = guia.Fecha.HasValue ? guia.Fecha.Value.TimeOfDay : TimeSpan.Zero,
                RucRemitente = almacen.Ruc ?? "",
                RazonSocialRemitente = almacen.RazonSocial ?? "",
                DireccionRemitente = almacen.Direccion ?? "",
                UbigeoRemitente = almacen.Ubigeo ?? "",
                DistritoRemitente = dpdParts.Length > 0 ? dpdParts[0].Trim() : "",
                ProvinciaRemitente = dpdParts.Length > 1 ? dpdParts[1].Trim() : "",
                DepartamentoRemitente = dpdParts.Length > 2 ? dpdParts[2].Trim() : "",
                TipoDocumentoDestinatario = guia.TipoDestinatario ?? "",
                NumeroDocumentoDestinatario = guia.Rucdnice ?? "",
                RazonSocialDestinatario = guia.RazonSocialNombre ?? "",
                DireccionDestinatario = guia.DireccionDestino ?? "",
                UbigeoDestinatario = guia.Dpddestino ?? "",
                DireccionOrigen = guia.DireccionOrigen ?? "",
                UbigeoOrigen = guia.Dpdorigen ?? "",
                DireccionDestino = guia.DireccionDestino ?? "",
                UbigeoDestino = guia.Dpddestino ?? "",
                Observaciones = guia.Observaciones ?? "",
                Detalles = guia.GuiaRemisionDetalles.Select(d => new GuiaRemisionDetalleDTO
                {
                    Item = d.Item,
                    CodigoItem = d.IdArticulo,
                    DescripcionItem = d.DescripcionArticulo,
                    Cantidad = d.Cantidad,
                    UnidadMedida = d.UnidadMedida
                }).ToList()
            };
        }

        private async Task<(string usuarioSOL, string claveSOL)> ObtenerCredencialesSunatAsync(string rucEmisor)
        {
            var credenciales = await _context.Almacens
                .Where(a => a.Ruc == rucEmisor)
                .Select(a => new { a.UsuarioSol, a.ClaveSol })
                .FirstOrDefaultAsync();

            if (credenciales == null)
            {
                return (string.Empty, string.Empty);
            }

            var usuarioSOL = $"{rucEmisor}{credenciales.UsuarioSol}";
            return (usuarioSOL, credenciales.ClaveSol);
        }

        private async Task<string> ObtenerSerieGuiaAsync(int idAlmacen)
        {
            // Buscar serie de guías para el almacén
            // Por defecto, usar "T001" si no existe configuración
            var serie = await _context.SerieCorrelativos
                .Where(s => s.IdAlmacen == idAlmacen && s.Serie.StartsWith("T"))
                .Select(s => s.Serie)
                .FirstOrDefaultAsync();

            return serie ?? "T001";
        }

        private async Task<int> ObtenerSiguienteNumeroAsync(string serie, int idAlmacen)
        {
            // Buscar último número usado para esta serie
            var ultimoNumero = await _context.GuiaRemisionCabeceras
                .Where(g => g.Serie == serie && g.IdAlmacen == idAlmacen)
                .OrderByDescending(g => g.Numero)
                .Select(g => (int?)g.Numero)
                .FirstOrDefaultAsync();

            return (ultimoNumero ?? 0) + 1;
        }
    }

    public class ConsultarTicketRequest
    {
        public string Ticket { get; set; } = string.Empty;
    }
}
