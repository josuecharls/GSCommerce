using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Models.SUNAT.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace GSCommerceAPI.Services.SUNAT;

public class GuiaRemisionElectronicaService : IGuiaRemisionElectronicaService
{
    private readonly IWebHostEnvironment _env;
    private readonly SyscharlesContext _context;

    // ============================================
    // CONFIGURACIÓN DE AMBIENTE SUNAT - NUEVA API REST GRE
    // ============================================
    // true = Ambiente BETA (pruebas con Nubefact)
    // false = Ambiente PRODUCCIÓN (SUNAT real)
    private const bool ES_AMBIENTE_BETA = true;
    
    // ===== NUEVA API REST SUNAT GRE =====
    // Endpoints BETA (Nubefact - servicio de pruebas gratuito)
    // Token: https://gre-test.nubefact.com/v1/clientessol/{client_id}/oauth2/token
    private const string TOKEN_ENDPOINT_BETA_BASE = "https://gre-test.nubefact.com/v1/clientessol";
    private const string API_GRE_ENDPOINT_BETA = "https://gre-test.nubefact.com/v1/contribuyente/gem/comprobantes";
    private const string API_GRE_CONSULTA_BETA = "https://gre-test.nubefact.com/v1/contribuyente/gem/comprobantes/envios";
    
    // Endpoints PRODUCCIÓN (SUNAT real)
    private const string TOKEN_ENDPOINT_PROD_BASE = "https://api-cpe.sunat.gob.pe/v1/clientessol";
    private const string API_GRE_ENDPOINT_PRODUCCION = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes";
    private const string API_GRE_CONSULTA_PRODUCCION = "https://api-cpe.sunat.gob.pe/v1/contribuyente/gem/comprobantes/envios";
    
    // Client ID y Secret para la API de pruebas (Nubefact)
    private const string CLIENT_ID_BETA = "test-85e5b0ae-255c-4891-a595-0b98c65c9854";
    private const string CLIENT_SECRET_BETA = "test-Hty/M6QshYvPgItX2P0+Kw==";
    
    // Credenciales de prueba SUNAT BETA
    // username = {numRucEmisor}MODDATOS, password = MODDATOS
    private const string USUARIO_BETA_SUFIJO = "MODDATOS";
    private const string CLAVE_BETA = "MODDATOS";
    
    // HttpClient para llamadas REST
    private static readonly HttpClient _httpClient = new HttpClient();
    // ============================================

    public GuiaRemisionElectronicaService(IWebHostEnvironment env, SyscharlesContext context)
    {
        _env = env;
        _context = context;
    }

    /// <summary>
    /// Obtiene las credenciales SOL según el ambiente configurado
    /// </summary>
    private (string usuario, string clave) ObtenerCredencialesSunat(string ruc, string? usuarioSolReal, string? claveSolReal)
    {
        if (ES_AMBIENTE_BETA)
        {
            // Credenciales de prueba: {RUC}MODDATOS / MODDATOS
            return ($"{ruc}{USUARIO_BETA_SUFIJO}", CLAVE_BETA);
        }
        else
        {
            // Credenciales reales del almacén
            //return ($"{ruc}{usuarioSolReal}", claveSolReal ?? "");
        }
    }

    /// <summary>
    /// Obtiene el endpoint de token OAuth según el ambiente configurado
    /// El formato es: {base_url}/{client_id}/oauth2/token
    /// </summary>
    private string ObtenerTokenEndpoint(string clientId)
    {
        string baseUrl = ES_AMBIENTE_BETA ? TOKEN_ENDPOINT_BETA_BASE : TOKEN_ENDPOINT_PROD_BASE;
        return $"{baseUrl}/{clientId}/oauth2/token";
    }

    /// <summary>
    /// Obtiene el endpoint de API GRE según el ambiente configurado
    /// </summary>
    private string ObtenerApiGreEndpoint()
    {
        return ES_AMBIENTE_BETA ? API_GRE_ENDPOINT_BETA : API_GRE_ENDPOINT_PRODUCCION;
    }

    /// <summary>
    /// Obtiene el endpoint de consulta de ticket GRE según el ambiente configurado
    /// </summary>
    private string ObtenerApiGreConsultaEndpoint()
    {
        return ES_AMBIENTE_BETA ? API_GRE_CONSULTA_BETA : API_GRE_CONSULTA_PRODUCCION;
    }

    /// <summary>
    /// Obtiene el Client ID y Secret según el ambiente configurado
    /// </summary>
    private (string clientId, string clientSecret) ObtenerClientCredentials()
    {
        if (ES_AMBIENTE_BETA)
        {
            return (CLIENT_ID_BETA, CLIENT_SECRET_BETA);
        }
        else
        {
            // En producción, obtener de configuración o base de datos
            // TODO: Configurar credenciales de producción
            return ("", "");
        }
    }

    public async Task<(bool exito, string mensaje, int idGuiaRemision)> CrearGuiaRemisionAsync(GuiaRemisionCabeceraDTO dto)
    {
        try
        {
            // Obtener correlativo
            var ultimoNumero = await _context.GuiaRemisionCabeceras
                .Where(g => g.Serie == dto.Serie)
                .MaxAsync(g => (int?)g.Numero) ?? 0;

            var guia = new GuiaRemisionCabecera
            {
                Serie = dto.Serie,
                Numero = ultimoNumero + 1,
                Fecha = dto.FechaEmision,
                DireccionOrigen = dto.DireccionPartida,
                Dpdorigen = dto.UbigeoPartida,
                TipoDestinatario = dto.TipoDocumentoDestinatario,
                RazonSocialNombre = dto.RazonSocialDestinatario,
                Rucdnice = dto.NumeroDocumentoDestinatario,
                DireccionDestino = dto.DireccionLlegada,
                Dpddestino = dto.UbigeoLlegada,
                Observaciones = dto.Observaciones,
                IdUsuario = dto.IdUsuario,
                Estado = "P", // Pendiente
                IdAlmacen = dto.IdAlmacen,
                
                // Campos de transporte
                MotivoTraslado = dto.MotivoTraslado,
                DescripcionMotivoTraslado = dto.DescripcionMotivoTraslado,
                ModalidadTraslado = dto.ModalidadTraslado,
                FechaInicioTraslado = dto.FechaInicioTraslado,
                PesoBrutoTotal = dto.PesoBrutoTotal,
                UnidadMedidaPeso = dto.UnidadMedidaPeso,
                NumeroBultos = dto.NumeroBultos,
                
                // Transportista
                RucTransportista = dto.RucTransportista,
                RazonSocialTransportista = dto.RazonSocialTransportista,
                
                // Vehículo y conductor
                PlacaVehiculo = dto.PlacaVehiculo,
                TipoDocumentoConductor = dto.TipoDocumentoConductor,
                NumeroDocumentoConductor = dto.NumeroDocumentoConductor,
                NombreConductor = dto.NombreConductor,
                ApellidoConductor = dto.ApellidoConductor,
                LicenciaConductor = dto.LicenciaConductor
            };

            _context.GuiaRemisionCabeceras.Add(guia);
            await _context.SaveChangesAsync();

            // Agregar detalles
            int item = 1;
            foreach (var det in dto.Detalles)
            {
                _context.GuiaRemisionDetalles.Add(new GuiaRemisionDetalle
                {
                    IdGuiaRemision = guia.IdGuiaRemision,
                    Item = item++,
                    IdArticulo = det.IdArticulo,
                    DescripcionArticulo = det.DescripcionArticulo,
                    Cantidad = (int)det.Cantidad,
                    UnidadMedida = det.UnidadMedida
                });
            }

            // Crear registro en FE.GuiaRemisionSunat
            _context.GuiaRemisionSunats.Add(new GuiaRemisionSunat
            {
                IdGuiaRemision = guia.IdGuiaRemision,
                EnviadoSunat = false,
                EstadoSunat = "PENDIENTE",
                FechaRegistro = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return (true, "Guía de remisión creada correctamente", guia.IdGuiaRemision);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear guía de remisión: {ex.Message}", 0);
        }
    }

    public async Task<GuiaRemisionCabeceraDTO?> ObtenerGuiaRemisionAsync(int idGuiaRemision)
    {
        var guia = await _context.GuiaRemisionCabeceras
            .Include(g => g.GuiaRemisionDetalles)
            .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

        if (guia == null) return null;

        var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == guia.IdAlmacen);
        var estadoSunat = await _context.GuiaRemisionSunats
            .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

        return new GuiaRemisionCabeceraDTO
        {
            IdGuiaRemision = guia.IdGuiaRemision,
            Serie = guia.Serie ?? "",
            Numero = guia.Numero ?? 0,
            FechaEmision = guia.Fecha ?? DateTime.Now,
            Observaciones = guia.Observaciones,
            RucRemitente = almacen?.Ruc ?? "",
            RazonSocialRemitente = almacen?.RazonSocial ?? "",
            DireccionRemitente = almacen?.Direccion ?? "",
            UbigeoRemitente = almacen?.Ubigeo ?? "",
            TipoDocumentoDestinatario = guia.TipoDestinatario ?? "6",
            NumeroDocumentoDestinatario = guia.Rucdnice ?? "",
            RazonSocialDestinatario = guia.RazonSocialNombre ?? "",
            DireccionDestinatario = guia.DireccionDestino ?? "",
            UbigeoDestinatario = guia.Dpddestino ?? "",
            UbigeoPartida = guia.Dpdorigen ?? "",
            DireccionPartida = guia.DireccionOrigen ?? "",
            UbigeoLlegada = guia.Dpddestino ?? "",
            DireccionLlegada = guia.DireccionDestino ?? "",
            IdAlmacen = guia.IdAlmacen,
            IdUsuario = guia.IdUsuario,
            Estado = guia.Estado ?? "P",
            EstadoSunat = estadoSunat?.EstadoSunat,
            HashSunat = estadoSunat?.Hash,
            EnviadoSunat = estadoSunat?.EnviadoSunat ?? false,
            TicketSunat = estadoSunat?.TicketSunat,
            RespuestaSunat = estadoSunat?.RespuestaSunat,
            
            // Campos de transporte
            MotivoTraslado = guia.MotivoTraslado ?? "01",
            DescripcionMotivoTraslado = guia.DescripcionMotivoTraslado ?? "",
            ModalidadTraslado = guia.ModalidadTraslado ?? "02",
            FechaInicioTraslado = guia.FechaInicioTraslado ?? DateTime.Now,
            PesoBrutoTotal = guia.PesoBrutoTotal ?? 0,
            UnidadMedidaPeso = guia.UnidadMedidaPeso ?? "KGM",
            NumeroBultos = guia.NumeroBultos,
            
            // Transportista
            RucTransportista = guia.RucTransportista,
            RazonSocialTransportista = guia.RazonSocialTransportista,
            
            // Vehículo y conductor
            PlacaVehiculo = guia.PlacaVehiculo,
            TipoDocumentoConductor = guia.TipoDocumentoConductor ?? "1",
            NumeroDocumentoConductor = guia.NumeroDocumentoConductor,
            NombreConductor = guia.NombreConductor,
            ApellidoConductor = guia.ApellidoConductor,
            LicenciaConductor = guia.LicenciaConductor,
            
            Detalles = guia.GuiaRemisionDetalles.Select(d => new GuiaRemisionDetalleDTO
            {
                Item = d.Item,
                IdArticulo = d.IdArticulo,
                DescripcionArticulo = d.DescripcionArticulo,
                Cantidad = d.Cantidad,
                UnidadMedida = d.UnidadMedida
            }).ToList()
        };
    }

    public async Task<List<GuiaRemisionCabeceraDTO>> ListarGuiasRemisionAsync(
        int? idAlmacen, DateTime? fechaDesde, DateTime? fechaHasta, 
        string? estadoSunat, int page, int pageSize)
    {
        var query = _context.GuiaRemisionCabeceras
            .Include(g => g.GuiaRemisionDetalles)
            .AsQueryable();

        if (idAlmacen.HasValue)
            query = query.Where(g => g.IdAlmacen == idAlmacen.Value);

        if (fechaDesde.HasValue)
            query = query.Where(g => g.Fecha >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(g => g.Fecha <= fechaHasta.Value.AddDays(1));

        var guias = await query
            .OrderByDescending(g => g.IdGuiaRemision)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<GuiaRemisionCabeceraDTO>();
        foreach (var guia in guias)
        {
            var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == guia.IdAlmacen);
            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == guia.IdGuiaRemision);

            if (!string.IsNullOrEmpty(estadoSunat) && sunatInfo?.EstadoSunat != estadoSunat)
                continue;

            result.Add(new GuiaRemisionCabeceraDTO
            {
                IdGuiaRemision = guia.IdGuiaRemision,
                Serie = guia.Serie ?? "",
                Numero = guia.Numero ?? 0,
                FechaEmision = guia.Fecha ?? DateTime.Now,
                RazonSocialDestinatario = guia.RazonSocialNombre ?? "",
                NumeroDocumentoDestinatario = guia.Rucdnice ?? "",
                Estado = guia.Estado ?? "P",
                EstadoSunat = sunatInfo?.EstadoSunat,
                EnviadoSunat = sunatInfo?.EnviadoSunat ?? false,
                RucRemitente = almacen?.Ruc ?? "",
                RazonSocialRemitente = almacen?.RazonSocial ?? ""
            });
        }

        return result;
    }

    public async Task<int> ContarGuiasRemisionAsync(int? idAlmacen, DateTime? fechaDesde, DateTime? fechaHasta, string? estadoSunat)
    {
        var query = _context.GuiaRemisionCabeceras.AsQueryable();

        if (idAlmacen.HasValue)
            query = query.Where(g => g.IdAlmacen == idAlmacen.Value);

        if (fechaDesde.HasValue)
            query = query.Where(g => g.Fecha >= fechaDesde.Value);

        if (fechaHasta.HasValue)
            query = query.Where(g => g.Fecha <= fechaHasta.Value.AddDays(1));

        return await query.CountAsync();
    }

    public async Task<(bool exito, string mensaje)> ActualizarGuiaRemisionAsync(GuiaRemisionCabeceraDTO dto)
    {
        try
        {
            var guia = await _context.GuiaRemisionCabeceras
                .Include(g => g.GuiaRemisionDetalles)
                .FirstOrDefaultAsync(g => g.IdGuiaRemision == dto.IdGuiaRemision);

            if (guia == null)
                return (false, "Guía de remisión no encontrada");

            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == dto.IdGuiaRemision);

            if (sunatInfo?.EnviadoSunat == true)
                return (false, "No se puede modificar una guía ya enviada a SUNAT");

            guia.Fecha = dto.FechaEmision;
            guia.DireccionOrigen = dto.DireccionPartida;
            guia.Dpdorigen = dto.UbigeoPartida;
            guia.TipoDestinatario = dto.TipoDocumentoDestinatario;
            guia.RazonSocialNombre = dto.RazonSocialDestinatario;
            guia.Rucdnice = dto.NumeroDocumentoDestinatario;
            guia.DireccionDestino = dto.DireccionLlegada;
            guia.Dpddestino = dto.UbigeoLlegada;
            guia.Observaciones = dto.Observaciones;
            
            // Campos de transporte
            guia.MotivoTraslado = dto.MotivoTraslado;
            guia.DescripcionMotivoTraslado = dto.DescripcionMotivoTraslado;
            guia.ModalidadTraslado = dto.ModalidadTraslado;
            guia.FechaInicioTraslado = dto.FechaInicioTraslado;
            guia.PesoBrutoTotal = dto.PesoBrutoTotal;
            guia.UnidadMedidaPeso = dto.UnidadMedidaPeso;
            guia.NumeroBultos = dto.NumeroBultos;
            
            // Transportista
            guia.RucTransportista = dto.RucTransportista;
            guia.RazonSocialTransportista = dto.RazonSocialTransportista;
            
            // Vehículo y conductor
            guia.PlacaVehiculo = dto.PlacaVehiculo;
            guia.TipoDocumentoConductor = dto.TipoDocumentoConductor;
            guia.NumeroDocumentoConductor = dto.NumeroDocumentoConductor;
            guia.NombreConductor = dto.NombreConductor;
            guia.ApellidoConductor = dto.ApellidoConductor;
            guia.LicenciaConductor = dto.LicenciaConductor;

            // Eliminar detalles anteriores y agregar nuevos
            _context.GuiaRemisionDetalles.RemoveRange(guia.GuiaRemisionDetalles);

            int item = 1;
            foreach (var det in dto.Detalles)
            {
                _context.GuiaRemisionDetalles.Add(new GuiaRemisionDetalle
                {
                    IdGuiaRemision = guia.IdGuiaRemision,
                    Item = item++,
                    IdArticulo = det.IdArticulo,
                    DescripcionArticulo = det.DescripcionArticulo,
                    Cantidad = (int)det.Cantidad,
                    UnidadMedida = det.UnidadMedida
                });
            }

            await _context.SaveChangesAsync();

            return (true, "Guía de remisión actualizada correctamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al actualizar guía de remisión: {ex.Message}");
        }
    }

    public async Task<(bool exito, string mensaje)> AnularGuiaRemisionAsync(int idGuiaRemision)
    {
        try
        {
            var guia = await _context.GuiaRemisionCabeceras
                .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

            if (guia == null)
                return (false, "Guía de remisión no encontrada");

            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

            if (sunatInfo?.EstadoSunat == "ACEPTADO")
                return (false, "No se puede anular una guía aceptada por SUNAT. Debe comunicar baja.");

            guia.Estado = "A";

            if (sunatInfo != null)
            {
                sunatInfo.EstadoSunat = "ANULADO";
                sunatInfo.FechaActualizacion = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return (true, "Guía de remisión anulada correctamente");
        }
        catch (Exception ex)
        {
            return (false, $"Error al anular guía de remisión: {ex.Message}");
        }
    }

    /// <summary>
    /// Anula una GRE ya aceptada en SUNAT mediante la API REST de baja
    /// </summary>
    public async Task<(bool exito, string ticket, string mensaje)> AnularGuiaRemisionSunatAsync(int idGuiaRemision, string motivoBaja)
    {
        try
        {
            var dto = await ObtenerGuiaRemisionAsync(idGuiaRemision);
            if (dto == null)
                return (false, "", "Guía de remisión no encontrada");

            var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == dto.IdAlmacen);
            if (almacen == null)
                return (false, "", "Almacén no encontrado");

            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

            if (sunatInfo == null)
                return (false, "", "Registro SUNAT no encontrado");

            if (sunatInfo.EstadoSunat != "ACEPTADO" && sunatInfo.EstadoSunat != "EMITIDO")
                return (false, "", "Solo se pueden anular guías aceptadas por SUNAT");

            // Obtener credenciales
            var (usuarioSOL, claveSOL) = ObtenerCredencialesSunat(almacen.Ruc!, almacen.UsuarioSol, almacen.ClaveSol);

            // Obtener token OAuth
            var (exitoToken, token, mensajeToken) = await ObtenerTokenSunatAsync(almacen.Ruc!, usuarioSOL, claveSOL);
            if (!exitoToken)
                return (false, "", $"Error de autenticación: {mensajeToken}");

            // Preparar request de baja
            var apiEndpoint = ObtenerApiGreEndpoint();
            // URL: {endpoint}/{RUC}-09-{serie}-{numero}/baja
            var requestUrl = $"{apiEndpoint}/{almacen.Ruc}-09-{dto.Serie}-{dto.Numero:D8}/baja";

            Console.WriteLine($"[SUNAT-GRE] ========================================");
            Console.WriteLine($"[SUNAT-GRE] Enviando baja GRE a SUNAT");
            Console.WriteLine($"[SUNAT-GRE] URL: {requestUrl}");
            Console.WriteLine($"[SUNAT-GRE] Motivo: {motivoBaja}");
            Console.WriteLine($"[SUNAT-GRE] ========================================");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Body con el motivo de baja
            var bodyContent = new
            {
                codMotivoBaja = "01", // 01 = Error en el comprobante
                desMotivoBaja = motivoBaja
            };

            var jsonBody = JsonSerializer.Serialize(bodyContent);
            Console.WriteLine($"[SUNAT-GRE] Body: {jsonBody}");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[SUNAT-GRE] Status Baja: {response.StatusCode}");
            Console.WriteLine($"[SUNAT-GRE] Respuesta Baja: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                // Parsear respuesta solo si hay contenido
                GreResponse? greResponse = null;
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        greResponse = JsonSerializer.Deserialize<GreResponse>(responseContent);
                    }
                    catch { }
                }

                // Actualizar estados
                var guia = await _context.GuiaRemisionCabeceras
                    .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

                if (guia != null)
                    guia.Estado = "A"; // Anulado

                sunatInfo.EstadoSunat = "ANULADO";
                sunatInfo.MensajeRespuestaSunat = $"Baja comunicada: {motivoBaja}";
                sunatInfo.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                string ticket = greResponse?.NumTicket ?? "";
                return (true, ticket, $"Baja comunicada exitosamente. {(string.IsNullOrEmpty(ticket) ? "" : $"Ticket: {ticket}")}");
            }
            else
            {
                string errorMsg = responseContent;
                string errorExc = "";
                
                // Intentar obtener detalles del error solo si hay contenido JSON
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("msg", out var msgElement))
                        {
                            errorMsg = msgElement.GetString() ?? responseContent;
                        }
                        if (jsonDoc.RootElement.TryGetProperty("exc", out var excElement))
                        {
                            errorExc = excElement.GetString() ?? "";
                        }
                    }
                    catch { }
                }

                var mensajeCompleto = string.IsNullOrEmpty(errorExc) 
                    ? $"SUNAT rechazó la baja: {errorMsg}" 
                    : $"SUNAT rechazó la baja: {errorMsg} - {errorExc}";
                    
                return (false, "", mensajeCompleto);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SUNAT-GRE] Error en baja: {ex.Message}");
            return (false, "", $"Error al comunicar baja: {ex.Message}");
        }
    }

    public async Task<(bool exito, string mensaje, string hash)> GenerarYFirmarGuiaAsync(int idGuiaRemision)
    {
        try
        {
            var dto = await ObtenerGuiaRemisionAsync(idGuiaRemision);
            if (dto == null)
                return (false, "Guía de remisión no encontrada", "");

            var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == dto.IdAlmacen);
            if (almacen == null)
                return (false, "Almacén no encontrado", "");

            // Generar XML
            var xml = GenerarXmlGuiaRemision(dto, almacen);

            // Ruta del archivo
            var nombreArchivo = $"{almacen.Ruc}-09-{dto.Serie}-{dto.Numero:D8}.xml";
            var rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

            // Validar RUC del almacén
            if (string.IsNullOrWhiteSpace(almacen.Ruc))
                return (false, $"El almacén '{almacen.Nombre}' no tiene RUC configurado. Configure el RUC para poder emitir guías electrónicas.", "");

            // Obtener certificado (usando el patrón de FacturacionElectronicaService)
            string nombreCertificado = GetCertificadoArchivo(almacen.Ruc!);
            var rutaCertificado = Path.Combine(_env.ContentRootPath, "Certificados", nombreCertificado);
            var passwordCertificado = GetCertificadoPassword(almacen.Ruc!);

            if (!File.Exists(rutaCertificado))
                return (false, $"Certificado digital no encontrado para el almacén '{almacen.Nombre}' (RUC: {almacen.Ruc}). Debe subir el archivo '{nombreCertificado}' a la carpeta Certificados.", "");

            // Firmar XML
            var hash = FirmarXml(xml, rutaCertificado, passwordCertificado, rutaXml);

            // Actualizar registro en FE.GuiaRemisionSunat
            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

            if (sunatInfo != null)
            {
                sunatInfo.Hash = hash;
                sunatInfo.Xml = await File.ReadAllTextAsync(rutaXml);
                sunatInfo.FechaActualizacion = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return (true, "XML generado y firmado correctamente", hash);
        }
        catch (Exception ex)
        {
            return (false, $"Error al generar/firmar XML: {ex.Message}", "");
        }
    }

    public async Task<(bool exito, string ticket, string mensaje)> EnviarGuiaRemisionSunatAsync(int idGuiaRemision)
    {
        try
        {
            // Primero generar y firmar
            var (exitoFirma, mensajeFirma, hash) = await GenerarYFirmarGuiaAsync(idGuiaRemision);
            if (!exitoFirma)
                return (false, "", mensajeFirma);

            var dto = await ObtenerGuiaRemisionAsync(idGuiaRemision);
            if (dto == null)
                return (false, "", "Guía no encontrada");

            var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == dto.IdAlmacen);
            if (almacen == null)
                return (false, "", "Almacén no encontrado");

            // Comprimir archivo
            var nombreArchivo = $"{almacen.Ruc}-09-{dto.Serie}-{dto.Numero:D8}";
            var rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", $"{nombreArchivo}.xml");
            var rutaZip = Path.Combine(_env.ContentRootPath, "Facturacion", $"{nombreArchivo}.zip");

            if (File.Exists(rutaZip))
                File.Delete(rutaZip);

            using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(rutaXml, Path.GetFileName(rutaXml));
            }

            // Obtener credenciales SOL (usa credenciales de prueba si ES_AMBIENTE_BETA = true)
            var (usuarioSOL, claveSOL) = ObtenerCredencialesSunat(almacen.Ruc!, almacen.UsuarioSol, almacen.ClaveSol);

            // Enviar a SUNAT
            var resultado = await EnviarGuiaSunatWsAsync(rutaZip, usuarioSOL, claveSOL, almacen.Ruc!);

            // Actualizar estado en GuiaRemisionSunat
            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

            // Si no existe el registro, crearlo
            if (sunatInfo == null)
            {
                sunatInfo = new GuiaRemisionSunat
                {
                    IdGuiaRemision = idGuiaRemision,
                    FechaRegistro = DateTime.Now
                };
                _context.GuiaRemisionSunats.Add(sunatInfo);
                Console.WriteLine($"[SUNAT-GRE] Creando nuevo registro GuiaRemisionSunat para ID {idGuiaRemision}");
            }

            // Obtener también la cabecera para actualizar el Estado
            var guiaCabecera = await _context.GuiaRemisionCabeceras
                .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

            sunatInfo.EnviadoSunat = true;
            sunatInfo.FechaEnvio = DateTime.Now;
            sunatInfo.RespuestaSunat = resultado.mensaje;
            sunatInfo.FechaRespuestaSunat = DateTime.Now;
            sunatInfo.FechaActualizacion = DateTime.Now;

            // Si recibimos un ticket, consultar automáticamente el estado
            if (!string.IsNullOrEmpty(resultado.ticket))
            {
                sunatInfo.TicketSunat = resultado.ticket;
                sunatInfo.EstadoSunat = "ENVIADO";
                await _context.SaveChangesAsync();
                Console.WriteLine($"[SUNAT-GRE] Ticket recibido: {resultado.ticket}, consultando estado...");

                // Consultar automáticamente el estado del ticket (esperar un poco y consultar)
                await Task.Delay(2000); // Esperar 2 segundos antes de consultar

                var consultaResultado = await ConsultarEstadoSunatAsync(idGuiaRemision);
                Console.WriteLine($"[SUNAT-GRE] Resultado consulta: {consultaResultado.mensaje}");

                // Recargar la entidad para obtener los cambios de la consulta
                await _context.Entry(sunatInfo).ReloadAsync();
                if (guiaCabecera != null)
                    await _context.Entry(guiaCabecera).ReloadAsync();

                // Retornar el resultado de la consulta
                if (consultaResultado.exito)
                {
                    return (true, resultado.ticket, $"GRE aceptada por SUNAT. {consultaResultado.mensaje}");
                }
                else if (sunatInfo.EstadoSunat == "PROCESANDO")
                {
                    return (true, resultado.ticket, $"Ticket SUNAT: {resultado.ticket}. {consultaResultado.mensaje}");
                }
                else
                {
                    return (false, resultado.ticket, consultaResultado.mensaje);
                }
            }
            else if (resultado.exito)
            {
                // Respuesta directa exitosa - ACEPTADO
                sunatInfo.EstadoSunat = "ACEPTADO";
                sunatInfo.CodigoRespuestaSunat = "0";
                sunatInfo.MensajeRespuestaSunat = resultado.mensaje;
                Console.WriteLine($"[SUNAT-GRE] Respuesta directa exitosa, Estado: ACEPTADO");

                // Actualizar Estado de la cabecera a "E" (Emitido)
                if (guiaCabecera != null)
                {
                    guiaCabecera.Estado = "E"; // Emitido
                    Console.WriteLine($"[SUNAT-GRE] Cabecera actualizada a Estado: E (Emitido)");
                }
            }
            else
            {
                // Rechazado por SUNAT
                sunatInfo.EstadoSunat = "RECHAZADO";
                sunatInfo.MensajeRespuestaSunat = resultado.mensaje;
                Console.WriteLine($"[SUNAT-GRE] Rechazado, Estado: RECHAZADO");
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"[SUNAT-GRE] Cambios guardados en base de datos");

            return resultado;
        }
        catch (Exception ex)
        {
            return (false, "", $"Error al enviar a SUNAT: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene token OAuth 2.0 de SUNAT para la nueva API GRE
    /// </summary>
    private async Task<(bool exito, string token, string mensaje)> ObtenerTokenSunatAsync(string ruc, string usuarioSOL, string claveSOL)
    {
        try
        {
            var (clientId, clientSecret) = ObtenerClientCredentials();
            var tokenEndpoint = ObtenerTokenEndpoint(clientId);

            Console.WriteLine($"[SUNAT-GRE] ========================================");
            Console.WriteLine($"[SUNAT-GRE] Obteniendo token OAuth...");
            Console.WriteLine($"[SUNAT-GRE] Endpoint Token: {tokenEndpoint}");
            Console.WriteLine($"[SUNAT-GRE] Client ID: {clientId}");
            Console.WriteLine($"[SUNAT-GRE] Username: {usuarioSOL}");
            Console.WriteLine($"[SUNAT-GRE] ========================================");

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);
            
            // Body con credenciales (formato x-www-form-urlencoded)
            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "scope", "https://api-cpe.sunat.gob.pe" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "username", usuarioSOL },
                { "password", claveSOL }
            };

            request.Content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[SUNAT-GRE] Respuesta Token Status: {response.StatusCode}");
            Console.WriteLine($"[SUNAT-GRE] Respuesta Token: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    Console.WriteLine($"[SUNAT-GRE] Token obtenido correctamente");
                    return (true, tokenResponse.AccessToken, "Token obtenido");
                }
            }

            return (false, "", $"Error al obtener token: {responseContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SUNAT-GRE] Error obteniendo token: {ex.Message}");
            return (false, "", $"Error al obtener token: {ex.Message}");
        }
    }

    /// <summary>
    /// Envía la Guía de Remisión a SUNAT usando la nueva API REST
    /// </summary>
    private async Task<(bool exito, string ticket, string mensaje)> EnviarGuiaSunatWsAsync(string rutaZip, string usuarioSOL, string claveSOL, string ruc)
    {
        try
        {
            Console.WriteLine($"[SUNAT-GRE] ========================================");
            Console.WriteLine($"[SUNAT-GRE] Ambiente: {(ES_AMBIENTE_BETA ? "BETA (Pruebas)" : "PRODUCCIÓN")}");
            Console.WriteLine($"[SUNAT-GRE] RUC Emisor: {ruc}");
            Console.WriteLine($"[SUNAT-GRE] Usuario SOL: {usuarioSOL}");
            Console.WriteLine($"[SUNAT-GRE] ========================================");

            // 1. Obtener token OAuth
            var (exitoToken, token, mensajeToken) = await ObtenerTokenSunatAsync(ruc, usuarioSOL, claveSOL);
            if (!exitoToken)
            {
                return (false, "", $"Error de autenticación: {mensajeToken}");
            }

            // 2. Leer archivo ZIP
            byte[] archivoZip = await File.ReadAllBytesAsync(rutaZip);
            string nombreZip = Path.GetFileName(rutaZip);
            string archivoBase64 = Convert.ToBase64String(archivoZip);

            // Extraer datos del nombre del archivo: {RUC}-09-{Serie}-{Numero}.zip
            var partes = Path.GetFileNameWithoutExtension(nombreZip).Split('-');
            if (partes.Length < 4)
            {
                return (false, "", "Nombre de archivo inválido");
            }

            string serie = partes[2];
            string numero = partes[3];

            // 3. Preparar request para la API GRE
            var apiEndpoint = ObtenerApiGreEndpoint();
            var requestUrl = $"{apiEndpoint}/{ruc}-09-{serie}-{numero}";

            Console.WriteLine($"[SUNAT-GRE] Enviando a: {requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Body con el archivo en base64
            var bodyContent = new
            {
                archivo = new
                {
                    nomArchivo = nombreZip,
                    arcGreZip = archivoBase64,
                    hashZip = CalcularHashSHA256(archivoZip)
                }
            };

            var jsonBody = JsonSerializer.Serialize(bodyContent);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // 4. Enviar request
            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[SUNAT-GRE] Status: {response.StatusCode}");
            Console.WriteLine($"[SUNAT-GRE] Respuesta: {responseContent}");

            // 5. Procesar respuesta
            if (response.IsSuccessStatusCode)
            {
                var greResponse = JsonSerializer.Deserialize<GreResponse>(responseContent);
                
                if (greResponse != null)
                {
                    // Si tiene ticket, es procesamiento asíncrono
                    if (!string.IsNullOrEmpty(greResponse.NumTicket))
                    {
                        Console.WriteLine($"[SUNAT-GRE] Ticket recibido: {greResponse.NumTicket}");
                        return (true, greResponse.NumTicket, $"Ticket SUNAT: {greResponse.NumTicket}");
                    }
                    
                    // Si tiene CDR, es respuesta directa
                    if (!string.IsNullOrEmpty(greResponse.CodRespuesta))
                    {
                        bool aceptado = greResponse.CodRespuesta == "0";
                        string mensaje = $"SUNAT respondió: [{greResponse.CodRespuesta}] {greResponse.MsgRespuesta}";
                        
                        // Guardar CDR si existe
                        if (!string.IsNullOrEmpty(greResponse.ArcCdr))
                        {
                            string carpeta = Path.GetDirectoryName(rutaZip)!;
                            string rutaCdr = Path.Combine(carpeta, $"R-{nombreZip}");
                            byte[] cdrBytes = Convert.FromBase64String(greResponse.ArcCdr);
                            await File.WriteAllBytesAsync(rutaCdr, cdrBytes);
                        }
                        
                        return (aceptado, "", mensaje);
                    }
                }
                
                return (true, "", "Guía enviada correctamente");
            }
            else
            {
                // Procesar error
                var errorResponse = JsonSerializer.Deserialize<GreErrorResponse>(responseContent);
                string errorMsg = errorResponse?.Errors?.FirstOrDefault()?.Msg ?? errorResponse?.Msg ?? responseContent;
                string errorCod = errorResponse?.Errors?.FirstOrDefault()?.GetCodAsString() ?? errorResponse?.GetCodAsString() ?? response.StatusCode.ToString();
                
                return (false, "", $"SUNAT rechazó: [{errorCod}] {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SUNAT-GRE] Error: {ex.Message}");
            return (false, "", $"Error al enviar a SUNAT: {ex.Message}");
        }
    }

    /// <summary>
    /// Calcula el hash SHA256 de un archivo en formato hexadecimal
    /// </summary>
    private string CalcularHashSHA256(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hash = sha256.ComputeHash(data);
        // SUNAT espera el hash en formato hexadecimal (lowercase)
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    public async Task<(bool exito, string codigo, string mensaje)> ConsultarEstadoSunatAsync(int idGuiaRemision)
    {
        try
        {
            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

            if (sunatInfo == null)
                return (false, "", "Registro SUNAT no encontrado");

            if (string.IsNullOrEmpty(sunatInfo.TicketSunat))
                return (false, "", "No hay ticket para consultar");

            var dto = await ObtenerGuiaRemisionAsync(idGuiaRemision);
            if (dto == null)
                return (false, "", "Guía no encontrada");

            var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == dto.IdAlmacen);

            if (almacen == null)
                return (false, "", "Almacén no encontrado");

            // Obtener credenciales SOL (usa credenciales de prueba si ES_AMBIENTE_BETA = true)
            var (usuarioSOL, claveSOL) = ObtenerCredencialesSunat(almacen.Ruc!, almacen.UsuarioSol, almacen.ClaveSol);

            // 1. Obtener token OAuth
            var (exitoToken, token, mensajeToken) = await ObtenerTokenSunatAsync(almacen.Ruc!, usuarioSOL, claveSOL);
            if (!exitoToken)
            {
                return (false, "", $"Error de autenticación: {mensajeToken}");
            }

            // 2. Consultar estado del ticket
            // Endpoint: https://gre-test.nubefact.com/v1/contribuyente/gem/comprobantes/envios/{numTicket}
            var apiEndpoint = ObtenerApiGreConsultaEndpoint();
            var requestUrl = $"{apiEndpoint}/{sunatInfo.TicketSunat}";

            Console.WriteLine($"[SUNAT-GRE] Consultando ticket: {requestUrl}");

            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[SUNAT-GRE] Respuesta consulta: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var greResponse = JsonSerializer.Deserialize<GreResponse>(responseContent);
                Console.WriteLine($"[SUNAT-GRE] CodRespuesta: {greResponse?.CodRespuesta}, IndCdrGenerado: {greResponse?.IndCdrGenerado}");

                // Verificar si SUNAT aún está procesando
                // indCdrGenerado = "0" significa que aún no ha procesado, "1" significa que ya procesó
                if (greResponse != null && greResponse.IndCdrGenerado == "0" && string.IsNullOrEmpty(greResponse.CodRespuesta))
                {
                    sunatInfo.EstadoSunat = "PROCESANDO";
                    sunatInfo.MensajeRespuestaSunat = "SUNAT aún está procesando el documento";
                    sunatInfo.FechaActualizacion = DateTime.Now;
                    await _context.SaveChangesAsync();

                    Console.WriteLine($"[SUNAT-GRE] Estado: PROCESANDO (aún no hay CDR)");
                    return (false, "PROCESANDO", "SUNAT aún está procesando el documento. Intente consultar más tarde.");
                }

                if (greResponse != null && !string.IsNullOrEmpty(greResponse.CodRespuesta))
                {
                    bool aceptado = greResponse.CodRespuesta == "0";
                    string mensaje = greResponse.GetMensaje();

                    sunatInfo.CodigoRespuestaSunat = greResponse.CodRespuesta;
                    sunatInfo.MensajeRespuestaSunat = mensaje;
                    sunatInfo.FechaRespuestaSunat = DateTime.Now;
                    sunatInfo.EstadoSunat = aceptado ? "ACEPTADO" : "RECHAZADO";
                    sunatInfo.FechaActualizacion = DateTime.Now;

                    Console.WriteLine($"[SUNAT-GRE] CodRespuesta={greResponse.CodRespuesta}, Mensaje={mensaje}");

                    // Si fue aceptado, actualizar Estado de la cabecera a "E" (Emitido)
                    if (aceptado)
                    {
                        var guiaCabecera = await _context.GuiaRemisionCabeceras
                            .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);
                        if (guiaCabecera != null)
                        {
                            guiaCabecera.Estado = "E"; // Emitido
                            Console.WriteLine($"[SUNAT-GRE] Cabecera actualizada a Estado: E (Emitido)");
                        }
                    }

                    // Guardar CDR si existe
                    if (!string.IsNullOrEmpty(greResponse.ArcCdr))
                    {
                        var nombreArchivo = $"{almacen.Ruc}-09-{dto.Serie}-{dto.Numero:D8}";
                        var rutaCdr = Path.Combine(_env.ContentRootPath, "Facturacion", $"R-{nombreArchivo}.zip");
                        byte[] cdrBytes = Convert.FromBase64String(greResponse.ArcCdr);
                        await File.WriteAllBytesAsync(rutaCdr, cdrBytes);
                    }

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[SUNAT-GRE] Estado final: {sunatInfo.EstadoSunat}");

                    return (aceptado, greResponse.CodRespuesta, mensaje);
                }

                return (false, "", "Respuesta sin código de estado");
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<GreErrorResponse>(responseContent);
                string errorMsg = errorResponse?.Msg ?? responseContent;
                return (false, errorResponse?.GetCodAsString() ?? "", errorMsg);
            }
        }
        catch (Exception ex)
        {
            return (false, "", $"Error al consultar estado: {ex.Message}");
        }
    }

    private async Task<(bool exito, string codigo, string descripcion)> LeerCdrAsync(string rutaZipRespuesta)
    {
        try
        {
            string carpeta = Path.GetDirectoryName(rutaZipRespuesta)!;

            if (File.Exists(rutaZipRespuesta))
            {
                ZipFile.ExtractToDirectory(rutaZipRespuesta, carpeta, true);
            }

            string nombreXmlRespuesta = Path.GetFileNameWithoutExtension(rutaZipRespuesta) + ".xml";
            string rutaXmlRespuesta = Path.Combine(carpeta, nombreXmlRespuesta);

            if (!File.Exists(rutaXmlRespuesta))
                return (false, "", "No se encontró el XML del CDR");

            XmlDocument xml = new XmlDocument();
            xml.Load(rutaXmlRespuesta);

            XmlNode? responseNode = xml.SelectSingleNode("//*[local-name()='DocumentResponse']/*[local-name()='Response']");

            if (responseNode != null)
            {
                string codigo = responseNode.SelectSingleNode("./*[local-name()='ResponseCode']")?.InnerText ?? "";
                string descripcion = responseNode.SelectSingleNode("./*[local-name()='Description']")?.InnerText ?? "";

                bool aceptado = codigo == "0";
                return (aceptado, codigo, descripcion);
            }

            return (false, "", "No se encontró el nodo de respuesta en el XML");
        }
        catch (Exception ex)
        {
            return (false, "", $"Error al leer CDR: {ex.Message}");
        }
    }

    public Task<byte[]?> GenerarPdfGuiaAsync(int idGuiaRemision)
    {
        // TODO: Implementar generación de PDF
        throw new NotImplementedException("Generación de PDF pendiente de implementar");
    }

    private string GenerarXmlGuiaRemision(GuiaRemisionCabeceraDTO dto, Almacen almacen)
    {
        string q = "\"";
        var sb = new StringBuilder();

        // Calcular peso total de los detalles
        decimal pesoTotal = dto.PesoBrutoTotal > 0 ? dto.PesoBrutoTotal : dto.Detalles.Sum(d => (d.PesoUnitario ?? 0.1m) * d.Cantidad);
        if (pesoTotal <= 0) pesoTotal = 1; // Mínimo 1 KG

        sb.AppendLine($"<?xml version={q}1.0{q} encoding={q}ISO-8859-1{q} standalone={q}no{q}?>");
        sb.AppendLine($"<DespatchAdvice xmlns={q}urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2{q}" +
                      $" xmlns:cac={q}urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2{q}" +
                      $" xmlns:cbc={q}urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2{q}" +
                      $" xmlns:ds={q}http://www.w3.org/2000/09/xmldsig#{q}" +
                      $" xmlns:ext={q}urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2{q}>");

        // UBL Extensions para la firma
        sb.AppendLine("<ext:UBLExtensions>");
        sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension>");
        sb.AppendLine("</ext:UBLExtensions>");

        // Versiones - Para GRE el CustomizationID debe ser 2.0
        sb.AppendLine("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
        sb.AppendLine("<cbc:CustomizationID>2.0</cbc:CustomizationID>");

        // ID del documento
        sb.AppendLine($"<cbc:ID>{dto.Serie}-{dto.Numero:D8}</cbc:ID>");

        // Fecha y hora de emisión
        sb.AppendLine($"<cbc:IssueDate>{dto.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
        sb.AppendLine($"<cbc:IssueTime>{dto.FechaEmision:HH:mm:ss}</cbc:IssueTime>");

        // Tipo de documento (09 = Guía de Remisión Remitente) - Sin atributos según ejemplo real
        sb.AppendLine("<cbc:DespatchAdviceTypeCode>09</cbc:DespatchAdviceTypeCode>");

        // Observaciones
        if (!string.IsNullOrEmpty(dto.Observaciones))
        {
            sb.AppendLine($"<cbc:Note>{EscaparTextoXml(dto.Observaciones)}</cbc:Note>");
        }

        // Firma digital (referencia) - Según ejemplo real
        sb.AppendLine("<cac:Signature>");
        sb.AppendLine("<cbc:ID>SignSUNAT</cbc:ID>");
        sb.AppendLine("<cac:SignatoryParty>");
        sb.AppendLine("<cac:PartyIdentification>");
        sb.AppendLine($"<cbc:ID>{almacen.Ruc}</cbc:ID>");
        sb.AppendLine("</cac:PartyIdentification>");
        sb.AppendLine("<cac:PartyName>");
        sb.AppendLine($"<cbc:Name>{EscaparTextoXml(almacen.RazonSocial ?? "")}</cbc:Name>");
        sb.AppendLine("</cac:PartyName>");
        sb.AppendLine("</cac:SignatoryParty>");
        sb.AppendLine("<cac:DigitalSignatureAttachment>");
        sb.AppendLine("<cac:ExternalReference>");
        sb.AppendLine("<cbc:URI>SignSUNAT</cbc:URI>");
        sb.AppendLine("</cac:ExternalReference>");
        sb.AppendLine("</cac:DigitalSignatureAttachment>");
        sb.AppendLine("</cac:Signature>");

        // Datos del remitente (DespatchSupplierParty) - Sin CustomerAssignedAccountID según ejemplo
        sb.AppendLine("<cac:DespatchSupplierParty>");
        sb.AppendLine("<cac:Party>");
        sb.AppendLine("<cac:PartyIdentification>");
        sb.AppendLine($"<cbc:ID schemeID={q}6{q}>{almacen.Ruc}</cbc:ID>");
        sb.AppendLine("</cac:PartyIdentification>");
        sb.AppendLine("<cac:PartyLegalEntity>");
        sb.AppendLine($"<cbc:RegistrationName>{EscaparTextoXml(almacen.RazonSocial ?? "")}</cbc:RegistrationName>");
        sb.AppendLine("</cac:PartyLegalEntity>");
        sb.AppendLine("</cac:Party>");
        sb.AppendLine("</cac:DespatchSupplierParty>");

        // Datos del destinatario (DeliveryCustomerParty) - Sin CustomerAssignedAccountID según ejemplo
        sb.AppendLine("<cac:DeliveryCustomerParty>");
        sb.AppendLine("<cac:Party>");
        sb.AppendLine("<cac:PartyIdentification>");
        sb.AppendLine($"<cbc:ID schemeID={q}{dto.TipoDocumentoDestinatario}{q}>{dto.NumeroDocumentoDestinatario}</cbc:ID>");
        sb.AppendLine("</cac:PartyIdentification>");
        sb.AppendLine("<cac:PartyLegalEntity>");
        sb.AppendLine($"<cbc:RegistrationName>{EscaparTextoXml(dto.RazonSocialDestinatario)}</cbc:RegistrationName>");
        sb.AppendLine("</cac:PartyLegalEntity>");
        sb.AppendLine("</cac:Party>");
        sb.AppendLine("</cac:DeliveryCustomerParty>");

        // Datos del envío (Shipment)
        sb.AppendLine("<cac:Shipment>");
        sb.AppendLine("<cbc:ID>SUNAT_Envio</cbc:ID>");

        // Motivo de traslado - Sin atributos según ejemplo real
        sb.AppendLine($"<cbc:HandlingCode>{dto.MotivoTraslado}</cbc:HandlingCode>");
        
        // Descripción del motivo - solo para motivo "13" (Otros) que lo requiere
        // Para motivos 01, 02, 04, 08, 09, 18, 19 NO debe incluirse
        var motivosConDescripcion = new[] { "13" };
        if (motivosConDescripcion.Contains(dto.MotivoTraslado) && !string.IsNullOrWhiteSpace(dto.DescripcionMotivoTraslado))
        {
            sb.AppendLine($"<cbc:Information>{EscaparTextoXml(dto.DescripcionMotivoTraslado)}</cbc:Information>");
        }

        // Peso bruto - DEBE IR ANTES de SpecialInstructions
        sb.AppendLine($"<cbc:GrossWeightMeasure unitCode={q}{dto.UnidadMedidaPeso}{q}>{pesoTotal:F3}</cbc:GrossWeightMeasure>");

        // Cantidad de bultos - DEBE IR ANTES de SpecialInstructions
        if (dto.NumeroBultos.HasValue && dto.NumeroBultos > 0)
        {
            sb.AppendLine($"<cbc:TotalTransportHandlingUnitQuantity>{dto.NumeroBultos}</cbc:TotalTransportHandlingUnitQuantity>");
        }

        // SpecialInstructions DESPUÉS de TotalTransportHandlingUnitQuantity
        var indicadorVehiculo = "M1L"; // Siempre M1L según ejemplo real
        sb.AppendLine($"<cbc:SpecialInstructions>SUNAT_Envio_IndicadorTrasladoVehiculo{indicadorVehiculo}</cbc:SpecialInstructions>");

        // Etapa del envío - Con ID según ejemplo real
        sb.AppendLine("<cac:ShipmentStage>");
        sb.AppendLine("<cbc:ID>1</cbc:ID>");

        // Modalidad de transporte - Con atributos según ejemplo real
        sb.AppendLine($"<cbc:TransportModeCode listAgencyName={q}PE:SUNAT{q} listName={q}Modalidad de traslado{q} listURI={q}urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo18{q}>{dto.ModalidadTraslado}</cbc:TransportModeCode>");

        // Fecha de inicio del traslado
        sb.AppendLine("<cac:TransitPeriod>");
        sb.AppendLine($"<cbc:StartDate>{dto.FechaInicioTraslado:yyyy-MM-dd}</cbc:StartDate>");
        sb.AppendLine("</cac:TransitPeriod>");

        // Datos del transportista (si es transporte público)
        if (dto.ModalidadTraslado == "01" && !string.IsNullOrEmpty(dto.RucTransportista))
        {
            sb.AppendLine("<cac:CarrierParty>");
            sb.AppendLine("<cac:PartyIdentification>");
            sb.AppendLine($"<cbc:ID schemeID={q}6{q}>{dto.RucTransportista}</cbc:ID>");
            sb.AppendLine("</cac:PartyIdentification>");
            sb.AppendLine("<cac:PartyName>");
            sb.AppendLine($"<cbc:Name>{EscaparTextoXml(dto.RazonSocialTransportista ?? "")}</cbc:Name>");
            sb.AppendLine("</cac:PartyName>");
            sb.AppendLine("</cac:CarrierParty>");
        }

        // Datos del conductor - Según ejemplo real
        if (dto.ModalidadTraslado == "02" && !string.IsNullOrEmpty(dto.NumeroDocumentoConductor))
        {
            // Transporte privado: llenar datos del conductor
            // El schemeID del conductor debe tener los atributos del catálogo 06
            // IMPORTANTE: SUNAT GRE 2.0 requiere código de 2 dígitos (Ej: "01" para DNI, no "1")
            var tipoDocConductorRaw = dto.TipoDocumentoConductor ?? "1";
            // Normalizar a 2 dígitos si es un número de 1 dígito
            var tipoDocConductor = tipoDocConductorRaw.Length == 1 && char.IsDigit(tipoDocConductorRaw[0]) 
                ? tipoDocConductorRaw.PadLeft(2, '0') 
                : tipoDocConductorRaw;
            sb.AppendLine("<cac:DriverPerson>");
            sb.AppendLine($"<cbc:ID schemeID={q}{tipoDocConductor}{q} schemeName={q}Documento de Identidad{q} schemeAgencyName={q}PE:SUNAT{q} schemeURI={q}urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06{q}>{dto.NumeroDocumentoConductor}</cbc:ID>");
            sb.AppendLine($"<cbc:FirstName>{EscaparTextoXml(dto.NombreConductor ?? "")}</cbc:FirstName>");
            sb.AppendLine($"<cbc:FamilyName>{EscaparTextoXml(dto.ApellidoConductor ?? "")}</cbc:FamilyName>");
            sb.AppendLine("<cbc:JobTitle>Principal</cbc:JobTitle>");
            if (!string.IsNullOrEmpty(dto.LicenciaConductor))
            {
                sb.AppendLine("<cac:IdentityDocumentReference>");
                sb.AppendLine($"<cbc:ID>{dto.LicenciaConductor}</cbc:ID>");
                sb.AppendLine("</cac:IdentityDocumentReference>");
            }
            sb.AppendLine("</cac:DriverPerson>");
        }
        else
        {
            // Transporte público: DriverPerson vacío según ejemplo
            sb.AppendLine("<cac:DriverPerson/>");
        }

        sb.AppendLine("</cac:ShipmentStage>");

        // Delivery - Según ejemplo real: contiene DeliveryAddress (llegada) y Despatch/DespatchAddress (partida)
        sb.AppendLine("<cac:Delivery>");
        // Dirección de llegada
        sb.AppendLine("<cac:DeliveryAddress>");
        sb.AppendLine($"<cbc:ID>{dto.UbigeoLlegada}</cbc:ID>");
        sb.AppendLine("<cac:AddressLine>");
        sb.AppendLine($"<cbc:Line>{EscaparTextoXml(dto.DireccionLlegada)}</cbc:Line>");
        sb.AppendLine("</cac:AddressLine>");
        sb.AppendLine("</cac:DeliveryAddress>");
        // Dirección de partida (dentro de Despatch)
        sb.AppendLine("<cac:Despatch>");
        sb.AppendLine("<cac:DespatchAddress>");
        sb.AppendLine($"<cbc:ID>{dto.UbigeoPartida}</cbc:ID>");
        sb.AppendLine("<cac:AddressLine>");
        sb.AppendLine($"<cbc:Line>{EscaparTextoXml(dto.DireccionPartida)}</cbc:Line>");
        sb.AppendLine("</cac:AddressLine>");
        sb.AppendLine("</cac:DespatchAddress>");
        sb.AppendLine("</cac:Despatch>");
        sb.AppendLine("</cac:Delivery>");

        // TransportHandlingUnit - Según ejemplo real
        if (dto.ModalidadTraslado == "02" && !string.IsNullOrEmpty(dto.PlacaVehiculo))
        {
            // Transporte privado: llenar datos del vehículo
            var placaLimpia = dto.PlacaVehiculo.Replace("-", "").Replace(" ", "").ToUpperInvariant();
            sb.AppendLine("<cac:TransportHandlingUnit>");
            sb.AppendLine("<cac:TransportEquipment>");
            sb.AppendLine($"<cbc:ID>{placaLimpia}</cbc:ID>");
            sb.AppendLine("</cac:TransportEquipment>");
            sb.AppendLine("</cac:TransportHandlingUnit>");
        }
        else
        {
            // Transporte público: TransportHandlingUnit vacío según ejemplo
            sb.AppendLine("<cac:TransportHandlingUnit/>");
        }

        sb.AppendLine("</cac:Shipment>");

        // Detalles (DespatchLine) - Según ejemplo real: usar Description en lugar de Name
        foreach (var det in dto.Detalles)
        {
            sb.AppendLine("<cac:DespatchLine>");
            sb.AppendLine($"<cbc:ID>{det.Item}</cbc:ID>");
            sb.AppendLine($"<cbc:DeliveredQuantity unitCode={q}{det.UnidadMedida}{q}>{det.Cantidad:F3}</cbc:DeliveredQuantity>");
            sb.AppendLine("<cac:OrderLineReference>");
            sb.AppendLine($"<cbc:LineID>{det.Item}</cbc:LineID>");
            sb.AppendLine("</cac:OrderLineReference>");
            sb.AppendLine("<cac:Item>");
            sb.AppendLine($"<cbc:Description>{EscaparTextoXml(det.DescripcionArticulo)}</cbc:Description>");
            sb.AppendLine("<cac:SellersItemIdentification>");
            sb.AppendLine($"<cbc:ID>{det.IdArticulo}</cbc:ID>");
            sb.AppendLine("</cac:SellersItemIdentification>");
            sb.AppendLine("</cac:Item>");
            sb.AppendLine("</cac:DespatchLine>");
        }

        sb.AppendLine("</DespatchAdvice>");

        return sb.ToString();
    }

    private string FirmarXml(string xmlContenido, string rutaCertificado, string password, string rutaSalida)
    {
        var xmlDoc = new XmlDocument { PreserveWhitespace = true };
        xmlDoc.LoadXml(xmlContenido);

        var certBytes = File.ReadAllBytes(rutaCertificado);
        using var cert = new X509Certificate2(certBytes, password,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

        var signedXml = new SignedXml(xmlDoc)
        {
            SigningKey = cert.GetRSAPrivateKey()
        };

        var reference = new Reference("")
        {
            DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1"
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigExcC14NTransform());
        signedXml.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        signedXml.ComputeSignature();

        var signature = signedXml.GetXml();
        var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        nsmgr.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
        var nodoExt = xmlDoc.SelectSingleNode("//ext:ExtensionContent", nsmgr);

        if (nodoExt != null)
        {
            nodoExt.RemoveAll();
            nodoExt.AppendChild(xmlDoc.ImportNode(signature, true));
        }

        xmlDoc.Save(rutaSalida);

        return signedXml.GetXml().GetElementsByTagName("DigestValue")[0].InnerText;
    }

    private string EscaparTextoXml(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        return texto.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("'", "&apos;");
    }

    private (bool exito, string codigo, string descripcion) LeerRespuestaCdr(string carpeta, string nombreArchivo)
    {
        try
        {
            // Buscar el XML de respuesta (R-{nombreArchivo}.xml)
            string rutaXmlRespuesta = Path.Combine(carpeta, $"R-{nombreArchivo}.xml");
            
            if (!File.Exists(rutaXmlRespuesta))
            {
                // Intentar buscar sin el prefijo R-
                var archivosXml = Directory.GetFiles(carpeta, "R-*.xml");
                if (archivosXml.Length > 0)
                {
                    rutaXmlRespuesta = archivosXml.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                }
            }

            if (!File.Exists(rutaXmlRespuesta))
                return (false, "", "No se encontró el XML de respuesta SUNAT");

            XmlDocument xml = new XmlDocument();
            xml.Load(rutaXmlRespuesta);

            // Buscar nodo de respuesta
            XmlNode? responseNode = xml.SelectSingleNode("//*[local-name()='DocumentResponse']/*[local-name()='Response']");

            if (responseNode != null)
            {
                string codigo = responseNode.SelectSingleNode("./*[local-name()='ResponseCode']")?.InnerText ?? "";
                string descripcion = responseNode.SelectSingleNode("./*[local-name()='Description']")?.InnerText ?? "";

                bool aceptado = codigo == "0";
                return (aceptado, codigo, descripcion);
            }

            // Buscar código de respuesta alternativo
            string responseCode = xml.SelectSingleNode("//*[local-name()='ResponseCode']")?.InnerText ?? "";
            string description = xml.SelectSingleNode("//*[local-name()='Description']")?.InnerText ?? "Sin descripción";

            if (!string.IsNullOrWhiteSpace(responseCode))
            {
                return (responseCode == "0", responseCode, description);
            }

            return (false, "", "No se encontró el nodo de respuesta en el XML");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SUNAT-GRE] Error al leer CDR: {ex.Message}");
            return (false, "", $"Error al leer respuesta: {ex.Message}");
        }
    }

    private string ObtenerDetalleFaultException(FaultException ex)
    {
        try
        {
            var fault = ex.CreateMessageFault();
            if (fault.HasDetail)
            {
                using var reader = fault.GetReaderAtDetailContents();
                var doc = new XmlDocument();
                doc.Load(reader);
                return doc.InnerText.Trim();
            }
        }
        catch { }
        return string.Empty;
    }

    /// <summary>
    /// Obtiene el nombre del archivo de certificado según el RUC del emisor
    /// </summary>
    private string GetCertificadoArchivo(string rucEmisor)
    {
        return rucEmisor switch
        {
            "20611555203" => "certificadoCEMAVI.p12",
            "20613191624" => "certificadoFATISHOP.p12",
            "20611746751" => "certificadoLYMARIN.p12",
            "20606834048" => "certificadoROMAGE.p12",
            _ => throw new Exception("RUC no registrado para emisión electrónica")
        };
    }

    /// <summary>
    /// Obtiene la contraseña del certificado según el RUC del emisor
    /// </summary>
    private string GetCertificadoPassword(string rucEmisor)
    {
        return rucEmisor switch
        {
            "20611555203" => "agglythes123",
            "20613191624" => "Fe101010",
            "20611746751" => "Marin123",
            "20606834048" => "Grupog2021",
            _ => throw new Exception("RUC no registrado para contraseña de certificado")
        };
    }
}

// ============================================
// DTOs para la API REST de SUNAT GRE
// ============================================

/// <summary>
/// Respuesta del token OAuth de SUNAT
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Respuesta exitosa de la API GRE
/// </summary>
public class GreResponse
{
    [JsonPropertyName("numTicket")]
    public string? NumTicket { get; set; }

    [JsonPropertyName("fecRecepcion")]
    public string? FecRecepcion { get; set; }

    [JsonPropertyName("codRespuesta")]
    public string? CodRespuesta { get; set; }

    [JsonPropertyName("arcCdr")]
    public string? ArcCdr { get; set; }

    [JsonPropertyName("indCdrGenerado")]
    public string? IndCdrGenerado { get; set; }

    [JsonPropertyName("msgRespuesta")]
    public string? MsgRespuesta { get; set; }

    [JsonPropertyName("error")]
    public GreResponseError? Error { get; set; }

    /// <summary>
    /// Obtiene el mensaje de respuesta o el mensaje de error
    /// </summary>
    public string GetMensaje()
    {
        if (!string.IsNullOrEmpty(MsgRespuesta))
            return MsgRespuesta;
        if (Error != null)
            return $"{Error.NumError} - {Error.DesError}";
        return "";
    }
}

public class GreResponseError
{
    [JsonPropertyName("numError")]
    public string? NumError { get; set; }

    [JsonPropertyName("desError")]
    public string? DesError { get; set; }
}

/// <summary>
/// Respuesta de error de la API GRE
/// </summary>
public class GreErrorResponse
{
    [JsonPropertyName("cod")]
    public JsonElement? Cod { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("exc")]
    public string? Exc { get; set; }

    [JsonPropertyName("errors")]
    public List<GreError>? Errors { get; set; }

    /// <summary>
    /// Obtiene el código como string (puede venir como número o string)
    /// </summary>
    public string? GetCodAsString() => Cod?.ToString();
}

/// <summary>
/// Detalle de error GRE
/// </summary>
public class GreError
{
    [JsonPropertyName("cod")]
    public JsonElement? Cod { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    /// <summary>
    /// Obtiene el código como string (puede venir como número o string)
    /// </summary>
    public string? GetCodAsString() => Cod?.ToString();
}


