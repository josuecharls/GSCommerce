using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Models.SUNAT.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace GSCommerceAPI.Services.SUNAT;

public class GuiaRemisionElectronicaService : IGuiaRemisionElectronicaService
{
    private readonly IWebHostEnvironment _env;
    private readonly SyscharlesContext _context;

    // ============================================
    // CONFIGURACIÓN DE AMBIENTE SUNAT
    // ============================================
    // true = Ambiente BETA (pruebas)
    // false = Ambiente PRODUCCIÓN
    private const bool ES_AMBIENTE_BETA = true;
    
    // Endpoints SUNAT
    private const string ENDPOINT_BETA = "https://e-beta.sunat.gob.pe/ol-ti-itemision-guia-gem-beta/billService";
    private const string ENDPOINT_PRODUCCION = "https://e-guiaremision.sunat.gob.pe/ol-ti-itemision-guia-gem/billService";
    
    // Credenciales de prueba SUNAT BETA
    private const string USUARIO_BETA_SUFIJO = "MODDATOS";
    private const string CLAVE_BETA = "MODDATOS";
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
    /// Obtiene el endpoint SUNAT según el ambiente configurado
    /// </summary>
    private string ObtenerEndpointSunat()
    {
        return ES_AMBIENTE_BETA ? ENDPOINT_BETA : ENDPOINT_PRODUCCION;
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
                IdAlmacen = dto.IdAlmacen
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

            // Obtener certificado
            var rutaCertificado = Path.Combine(_env.ContentRootPath, "Certificados", almacen.Certificado ?? $"{almacen.Ruc}.p12");
            var passwordCertificado = almacen.PasswordCertificado ?? "";

            if (!File.Exists(rutaCertificado))
                return (false, $"Certificado no encontrado: {rutaCertificado}", "");

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

            // Actualizar estado - sendBill devuelve respuesta directa (no ticket)
            var sunatInfo = await _context.GuiaRemisionSunats
                .FirstOrDefaultAsync(s => s.IdGuiaRemision == idGuiaRemision);

            if (sunatInfo != null)
            {
                sunatInfo.EnviadoSunat = true;
                sunatInfo.FechaEnvio = DateTime.Now;
                sunatInfo.RespuestaSunat = resultado.mensaje;
                sunatInfo.FechaRespuestaSunat = DateTime.Now;
                sunatInfo.FechaActualizacion = DateTime.Now;

                // Determinar estado basado en la respuesta
                if (resultado.exito)
                {
                    sunatInfo.EstadoSunat = "ACEPTADO";
                    sunatInfo.CodigoRespuestaSunat = "0";
                    sunatInfo.MensajeRespuestaSunat = resultado.mensaje;
                }
                else
                {
                    // Extraer código de error del mensaje si es posible
                    sunatInfo.EstadoSunat = "RECHAZADO";
                    sunatInfo.MensajeRespuestaSunat = resultado.mensaje;
                }

                await _context.SaveChangesAsync();
            }

            return resultado;
        }
        catch (Exception ex)
        {
            return (false, "", $"Error al enviar a SUNAT: {ex.Message}");
        }
    }

    private async Task<(bool exito, string ticket, string mensaje)> EnviarGuiaSunatWsAsync(string rutaZip, string usuarioSOL, string claveSOL, string ruc)
    {
        try
        {
            byte[] archivoZip = await File.ReadAllBytesAsync(rutaZip);
            string nombreZip = Path.GetFileName(rutaZip);

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
            {
                MaxReceivedMessageSize = int.MaxValue
            };
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

            // Endpoint según ambiente configurado (BETA o PRODUCCIÓN)
            var endpointUrl = ObtenerEndpointSunat();
            var direccion = new EndpointAddress(endpointUrl);
            var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

            ws.ClientCredentials.UserName.UserName = usuarioSOL;
            ws.ClientCredentials.UserName.Password = claveSOL;

            var elementos = ws.Endpoint.Binding.CreateBindingElements();
            var securityElement = elementos.Find<SecurityBindingElement>();
            if (securityElement != null)
            {
                securityElement.EnableUnsecuredResponse = true;
            }
            ws.Endpoint.Binding = new CustomBinding(elementos);

            Console.WriteLine($"[SUNAT-GRE] ========================================");
            Console.WriteLine($"[SUNAT-GRE] Ambiente: {(ES_AMBIENTE_BETA ? "BETA (Pruebas)" : "PRODUCCIÓN")}");
            Console.WriteLine($"[SUNAT-GRE] Enviando guía: {nombreZip}");
            Console.WriteLine($"[SUNAT-GRE] RUC Emisor: {ruc}");
            Console.WriteLine($"[SUNAT-GRE] Usuario SOL: {usuarioSOL}");
            Console.WriteLine($"[SUNAT-GRE] Endpoint: {endpointUrl}");
            Console.WriteLine($"[SUNAT-GRE] ========================================");

            // Enviar usando sendBill para guías de remisión
            var response = await ws.sendBillAsync(nombreZip, archivoZip, "");
            byte[] contenidoRespuesta = response.applicationResponse;

            // Guardar respuesta ZIP
            string carpeta = Path.GetDirectoryName(rutaZip)!;
            string nombreZipRespuesta = "R-" + nombreZip;
            string rutaZipRespuesta = Path.Combine(carpeta, nombreZipRespuesta);
            await File.WriteAllBytesAsync(rutaZipRespuesta, contenidoRespuesta);

            // Extraer y leer CDR
            if (File.Exists(rutaZipRespuesta))
            {
                ZipFile.ExtractToDirectory(rutaZipRespuesta, carpeta, true);
                var (exitoCdr, codigoCdr, descripcionCdr) = LeerRespuestaCdr(carpeta, nombreZip);
                
                Console.WriteLine($"[SUNAT-GRE] Respuesta: [{codigoCdr}] {descripcionCdr}");
                
                if (exitoCdr)
                    return (true, "", $"SUNAT respondió: [{codigoCdr}] {descripcionCdr}");
                else
                    return (false, "", $"SUNAT rechazó: [{codigoCdr}] {descripcionCdr}");
            }

            return (true, "", "Guía enviada. Respuesta recibida: " + contenidoRespuesta.Length + " bytes");
        }
        catch (FaultException ex)
        {
            var detalle = ObtenerDetalleFaultException(ex);
            return (false, "", $"SUNAT rechazó el envío: {ex.Message} - {detalle}");
        }
        catch (Exception ex)
        {
            return (false, "", $"Error al enviar a SUNAT: {ex.Message}");
        }
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
            var almacen = await _context.Almacens.FirstOrDefaultAsync(a => a.IdAlmacen == dto.IdAlmacen);

            if (almacen == null)
                return (false, "", "Almacén no encontrado");

            // Obtener credenciales SOL (usa credenciales de prueba si ES_AMBIENTE_BETA = true)
            var (usuarioSOL, claveSOL) = ObtenerCredencialesSunat(almacen.Ruc!, almacen.UsuarioSol, almacen.ClaveSol);

            var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
            {
                MaxReceivedMessageSize = int.MaxValue
            };
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

            // Endpoint según ambiente configurado (BETA o PRODUCCIÓN)
            var direccion = new EndpointAddress(ObtenerEndpointSunat());
            var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

            ws.ClientCredentials.UserName.UserName = usuarioSOL;
            ws.ClientCredentials.UserName.Password = claveSOL;

            var elementos = ws.Endpoint.Binding.CreateBindingElements();
            var securityElement = elementos.Find<SecurityBindingElement>();
            if (securityElement != null)
            {
                securityElement.EnableUnsecuredResponse = true;
            }
            ws.Endpoint.Binding = new CustomBinding(elementos);

            var respuesta = await ws.getStatusAsync(sunatInfo.TicketSunat);

            if (respuesta?.status?.content != null && respuesta.status.content.Length > 0)
            {
                var nombreArchivo = $"{almacen.Ruc}-09-{dto?.Serie}-{dto?.Numero:D8}";
                var rutaZipRespuesta = Path.Combine(_env.ContentRootPath, "Facturacion", $"R-{nombreArchivo}.zip");

                await File.WriteAllBytesAsync(rutaZipRespuesta, respuesta.status.content);

                var (exitoCdr, codigoCdr, descripcionCdr) = await LeerCdrAsync(rutaZipRespuesta);

                sunatInfo.CodigoRespuestaSunat = codigoCdr;
                sunatInfo.MensajeRespuestaSunat = descripcionCdr;
                sunatInfo.FechaRespuestaSunat = DateTime.Now;
                sunatInfo.EstadoSunat = exitoCdr ? "ACEPTADO" : "RECHAZADO";
                sunatInfo.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return (exitoCdr, codigoCdr, descripcionCdr);
            }

            return (false, respuesta?.status?.statusCode ?? "", "Sin respuesta de SUNAT");
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

        // Versiones
        sb.AppendLine("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
        sb.AppendLine("<cbc:CustomizationID>2.0</cbc:CustomizationID>");

        // ID del documento
        sb.AppendLine($"<cbc:ID>{dto.Serie}-{dto.Numero:D8}</cbc:ID>");

        // Fecha y hora de emisión
        sb.AppendLine($"<cbc:IssueDate>{dto.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
        sb.AppendLine($"<cbc:IssueTime>{dto.FechaEmision:HH:mm:ss}</cbc:IssueTime>");

        // Tipo de documento (09 = Guía de Remisión Remitente)
        sb.AppendLine("<cbc:DespatchAdviceTypeCode listAgencyName=\"PE:SUNAT\" listName=\"SUNAT:Identificador de Tipo de Documento\" listURI=\"urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo01\">09</cbc:DespatchAdviceTypeCode>");

        // Observaciones
        if (!string.IsNullOrEmpty(dto.Observaciones))
        {
            sb.AppendLine($"<cbc:Note>{EscaparTextoXml(dto.Observaciones)}</cbc:Note>");
        }

        // Firma digital (referencia)
        sb.AppendLine("<cac:Signature>");
        sb.AppendLine($"<cbc:ID>{almacen.Ruc}</cbc:ID>");
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
        sb.AppendLine($"<cbc:URI>#{almacen.Ruc}</cbc:URI>");
        sb.AppendLine("</cac:ExternalReference>");
        sb.AppendLine("</cac:DigitalSignatureAttachment>");
        sb.AppendLine("</cac:Signature>");

        // Datos del remitente (DespatchSupplierParty)
        sb.AppendLine("<cac:DespatchSupplierParty>");
        sb.AppendLine($"<cbc:CustomerAssignedAccountID schemeID={q}6{q}>{almacen.Ruc}</cbc:CustomerAssignedAccountID>");
        sb.AppendLine("<cac:Party>");
        sb.AppendLine("<cac:PartyIdentification>");
        sb.AppendLine($"<cbc:ID schemeID={q}6{q} schemeName={q}Documento de Identidad{q} schemeAgencyName={q}PE:SUNAT{q} schemeURI={q}urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06{q}>{almacen.Ruc}</cbc:ID>");
        sb.AppendLine("</cac:PartyIdentification>");
        sb.AppendLine("<cac:PartyLegalEntity>");
        sb.AppendLine($"<cbc:RegistrationName>{EscaparTextoXml(almacen.RazonSocial ?? "")}</cbc:RegistrationName>");
        sb.AppendLine("</cac:PartyLegalEntity>");
        sb.AppendLine("</cac:Party>");
        sb.AppendLine("</cac:DespatchSupplierParty>");

        // Datos del destinatario (DeliveryCustomerParty)
        sb.AppendLine("<cac:DeliveryCustomerParty>");
        sb.AppendLine($"<cbc:CustomerAssignedAccountID schemeID={q}{dto.TipoDocumentoDestinatario}{q}>{dto.NumeroDocumentoDestinatario}</cbc:CustomerAssignedAccountID>");
        sb.AppendLine("<cac:Party>");
        sb.AppendLine("<cac:PartyIdentification>");
        sb.AppendLine($"<cbc:ID schemeID={q}{dto.TipoDocumentoDestinatario}{q} schemeName={q}Documento de Identidad{q} schemeAgencyName={q}PE:SUNAT{q} schemeURI={q}urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo06{q}>{dto.NumeroDocumentoDestinatario}</cbc:ID>");
        sb.AppendLine("</cac:PartyIdentification>");
        sb.AppendLine("<cac:PartyLegalEntity>");
        sb.AppendLine($"<cbc:RegistrationName>{EscaparTextoXml(dto.RazonSocialDestinatario)}</cbc:RegistrationName>");
        sb.AppendLine("</cac:PartyLegalEntity>");
        sb.AppendLine("</cac:Party>");
        sb.AppendLine("</cac:DeliveryCustomerParty>");

        // Datos del envío (Shipment)
        sb.AppendLine("<cac:Shipment>");
        sb.AppendLine("<cbc:ID>SUNAT_Envio</cbc:ID>");

        // Motivo de traslado
        sb.AppendLine($"<cbc:HandlingCode listAgencyName={q}PE:SUNAT{q} listName={q}SUNAT:Identificador de motivo de traslado{q} listURI={q}urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo20{q}>{dto.MotivoTraslado}</cbc:HandlingCode>");
        sb.AppendLine($"<cbc:Information>{EscaparTextoXml(dto.DescripcionMotivoTraslado)}</cbc:Information>");

        // Peso bruto
        sb.AppendLine($"<cbc:GrossWeightMeasure unitCode={q}{dto.UnidadMedidaPeso}{q}>{pesoTotal:F2}</cbc:GrossWeightMeasure>");

        // Cantidad de bultos
        if (dto.NumeroBultos.HasValue && dto.NumeroBultos > 0)
        {
            sb.AppendLine($"<cbc:TotalTransportHandlingUnitQuantity>{dto.NumeroBultos}</cbc:TotalTransportHandlingUnitQuantity>");
        }

        // Etapa del envío
        sb.AppendLine("<cac:ShipmentStage>");

        // Modalidad de transporte
        sb.AppendLine($"<cbc:TransportModeCode listName={q}SUNAT:Indicador de Modalidad de traslado{q} listAgencyName={q}PE:SUNAT{q} listURI={q}urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo18{q}>{dto.ModalidadTraslado}</cbc:TransportModeCode>");

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

        // Datos del vehículo (si es transporte privado)
        if (dto.ModalidadTraslado == "02" && !string.IsNullOrEmpty(dto.PlacaVehiculo))
        {
            sb.AppendLine("<cac:TransportMeans>");
            sb.AppendLine("<cac:RoadTransport>");
            sb.AppendLine($"<cbc:LicensePlateID>{dto.PlacaVehiculo}</cbc:LicensePlateID>");
            sb.AppendLine("</cac:RoadTransport>");
            sb.AppendLine("</cac:TransportMeans>");
        }

        // Datos del conductor (si es transporte privado)
        if (dto.ModalidadTraslado == "02" && !string.IsNullOrEmpty(dto.NumeroDocumentoConductor))
        {
            sb.AppendLine("<cac:DriverPerson>");
            sb.AppendLine($"<cbc:ID schemeID={q}{dto.TipoDocumentoConductor ?? "1"}{q}>{dto.NumeroDocumentoConductor}</cbc:ID>");
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

        sb.AppendLine("</cac:ShipmentStage>");

        // Punto de llegada
        sb.AppendLine("<cac:Delivery>");
        sb.AppendLine("<cac:DeliveryAddress>");
        sb.AppendLine($"<cbc:ID schemeAgencyName={q}PE:INEI{q} schemeName={q}Ubigeos{q}>{dto.UbigeoLlegada}</cbc:ID>");
        sb.AppendLine($"<cbc:StreetName>{EscaparTextoXml(dto.DireccionLlegada)}</cbc:StreetName>");
        sb.AppendLine("</cac:DeliveryAddress>");
        sb.AppendLine("</cac:Delivery>");

        // Punto de partida
        sb.AppendLine("<cac:OriginAddress>");
        sb.AppendLine($"<cbc:ID schemeAgencyName={q}PE:INEI{q} schemeName={q}Ubigeos{q}>{dto.UbigeoPartida}</cbc:ID>");
        sb.AppendLine($"<cbc:StreetName>{EscaparTextoXml(dto.DireccionPartida)}</cbc:StreetName>");
        sb.AppendLine("</cac:OriginAddress>");

        sb.AppendLine("</cac:Shipment>");

        // Detalles (DespatchLine)
        foreach (var det in dto.Detalles)
        {
            sb.AppendLine("<cac:DespatchLine>");
            sb.AppendLine($"<cbc:ID>{det.Item}</cbc:ID>");
            sb.AppendLine($"<cbc:DeliveredQuantity unitCode={q}{det.UnidadMedida}{q}>{det.Cantidad:F2}</cbc:DeliveredQuantity>");
            sb.AppendLine("<cac:OrderLineReference>");
            sb.AppendLine($"<cbc:LineID>{det.Item}</cbc:LineID>");
            sb.AppendLine("</cac:OrderLineReference>");
            sb.AppendLine("<cac:Item>");
            sb.AppendLine($"<cbc:Name>{EscaparTextoXml(det.DescripcionArticulo)}</cbc:Name>");
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
}


