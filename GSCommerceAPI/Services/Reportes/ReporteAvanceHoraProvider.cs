using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GSCommerceAPI.Services.Reportes;

public class ReporteAvanceHoraProvider
{
    private readonly SyscharlesContext _context;

    public ReporteAvanceHoraProvider(SyscharlesContext context)
    {
        _context = context;
    }

    public async Task<ReporteAvanceHoraDTO> ObtenerReporteAsync(
        DateTime? desde,
        DateTime? hasta,
        int? idAlmacen,
        int? idAlmacenForzado = null,
        CancellationToken cancellationToken = default)
    {
        var inicio = desde ?? DateTime.Today.Date;
        inicio = new DateTime(inicio.Year, inicio.Month, inicio.Day, inicio.Hour, 0, 0);

        var fin = hasta ?? inicio.AddHours(1);
        fin = new DateTime(fin.Year, fin.Month, fin.Day, fin.Hour, 0, 0);

        if (fin <= inicio)
        {
            fin = inicio.AddHours(1);
        }

        var diaInicio = inicio.Date;
        var diaFin = diaInicio.AddDays(1);

        if (fin > diaFin)
        {
            fin = diaFin;
        }

        var queryBase = _context.ComprobanteDeVentaCabeceras
            .AsNoTracking()
            .Where(c => c.Fecha >= diaInicio && c.Fecha < diaFin)
            .Where(c => c.Estado != "A" || c.GeneroNc != null);

        var almacenFiltro = idAlmacenForzado.HasValue
            ? idAlmacenForzado
            : (idAlmacen.HasValue && idAlmacen.Value > 0 ? idAlmacen : null);

        if (almacenFiltro.HasValue)
        {
            queryBase = queryBase.Where(c => c.IdAlmacen == almacenFiltro.Value);
        }

        var ventasDia = await queryBase
            .Select(c => new
            {
                c.Fecha,
                Monto = c.Apagar ?? c.Total,
                c.IdComprobante
            })
            .ToListAsync(cancellationToken);

        var ventasPorHora = ventasDia
            .GroupBy(v => v.Fecha.Hour)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Total = g.Sum(x => x.Monto),
                    Tickets = g.Count()
                });

        var detalles = new List<ReporteAvanceHoraDetalleDTO>();
        decimal acumuladoMontos = 0m;
        int acumuladoTickets = 0;

        for (var hora = 0; hora < 24; hora++)
        {
            var horaInicioDetalle = diaInicio.AddHours(hora);
            var horaFinDetalle = horaInicioDetalle.AddHours(1);

            decimal totalHoraDetalle = 0m;
            int ticketsHoraDetalle = 0;

            if (ventasPorHora.TryGetValue(hora, out var resumenHora))
            {
                totalHoraDetalle = resumenHora.Total;
                ticketsHoraDetalle = resumenHora.Tickets;
            }

            acumuladoMontos += totalHoraDetalle;
            acumuladoTickets += ticketsHoraDetalle;

            detalles.Add(new ReporteAvanceHoraDetalleDTO
            {
                HoraInicio = horaInicioDetalle,
                HoraFin = horaFinDetalle,
                TotalHora = totalHoraDetalle,
                TotalAcumulado = acumuladoMontos,
                TicketsHora = ticketsHoraDetalle,
                TicketsAcumulados = acumuladoTickets
            });
        }

        var detalleSeleccionado = detalles
            .FirstOrDefault(d => inicio >= d.HoraInicio && inicio < d.HoraFin);

        var detalleAcumulado = detalles
            .LastOrDefault(d => d.HoraInicio < fin);

        return new ReporteAvanceHoraDTO
        {
            HoraInicio = inicio,
            HoraFin = fin,
            TotalVentas = detalleSeleccionado?.TotalHora ?? 0m,
            TotalVentasDia = detalleAcumulado?.TotalAcumulado ?? 0m,
            Tickets = detalleSeleccionado?.TicketsHora ?? 0,
            TicketsDia = detalleAcumulado?.TicketsAcumulados ?? 0,
            DetalleHoras = detalles
        };
    }
}
