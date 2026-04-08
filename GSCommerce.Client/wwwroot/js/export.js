// wwwroot/js/export.js
(function () {
    function toNum(v) {
        // Limpia strings "S/ 1.234,56" -> num. Si no es finito, devuelve 0.
        if (typeof v === 'string') v = v.replace(/[^\d.-]/g, '');
        const n = Number(v);
        return Number.isFinite(n) ? n : 0;
    }

    window.exportTop20Excel = (rows, desde, hasta, almacenLabel, lineasLabel, familiasLabel, title, fileLabel) => {
        if (!Array.isArray(rows) || rows.length === 0) return;

        // Normaliza claves por si vienen en camelCase desde Blazor
        const norm = rows.map(r => ({
            nro: r.nro ?? r.Nro ?? 0,
            codigo: r.codigo ?? r.Codigo ?? '',
            descripcion: r.descripcion ?? r.Descripcion ?? '',
            cantidad: toNum(r.cantidad ?? r.Cantidad),
            importe: toNum(r.importe ?? r.Importe)
        }));

        const headerTitle = typeof title === 'string' && title.trim().length > 0
            ? title.trim()
            : 'Top 20 Artículos';
        const fileName = typeof fileLabel === 'string' && fileLabel.trim().length > 0
            ? fileLabel.trim()
            : 'Top20_Articulos';

        const header = [
            [headerTitle],
            [`Rango: ${desde} a ${hasta}`],
            [`Ámbito: ${almacenLabel}`]
        ];

        if (typeof lineasLabel === 'string' && lineasLabel.trim().length > 0) {
            header.push([`Líneas: ${lineasLabel}`]);
        }

        if (typeof familiasLabel === 'string' && familiasLabel.trim().length > 0) {
            header.push([`Familias: ${familiasLabel}`]);
        }

        header.push([]);

        const cols = ["#", "Código", "Descripción", "Cantidad Vendida", "Total Importe (S/)"];
        const data = norm.map(r => [r.nro, r.codigo, r.descripcion, r.cantidad, r.importe]);

        const wb = XLSX.utils.book_new();
        const ws = XLSX.utils.aoa_to_sheet([...header, cols, ...data]);

        // Asegura tipo numérico y formato en Cantidad (col 4) e Importe (col 5)
        const range = XLSX.utils.decode_range(ws["!ref"]);
        const firstDataRow = header.length + 1; // cabecera dinámica + fila de columnas
        for (let R = firstDataRow; R <= range.e.r; R++) {
            const cCant = XLSX.utils.encode_cell({ r: R, c: 3 });
            const cImp = XLSX.utils.encode_cell({ r: R, c: 4 });
            if (ws[cCant]) ws[cCant].t = 'n';
            if (ws[cImp]) { ws[cImp].t = 'n'; ws[cImp].z = '#,##0.00'; }
        }

        ws["!cols"] = [
            { wch: 5 }, { wch: 14 }, { wch: 48 }, { wch: 18 }, { wch: 20 }
        ];

        XLSX.utils.book_append_sheet(wb, ws, "Top20");
        XLSX.writeFile(wb, `${fileName}_${desde}_a_${hasta}.xlsx`);
    };

    window.exportVentasArticuloExcel = (reportes, filename) => {
        if (!Array.isArray(reportes) || reportes.length === 0) return;
        const wb = XLSX.utils.book_new();
        reportes.forEach(r => {
            const ws = XLSX.utils.aoa_to_sheet([r.headers, ...r.rows]);
            XLSX.utils.book_append_sheet(wb, ws, r.sheetName?.substring(0, 31) || "Hoja");
        });
        XLSX.writeFile(wb, filename + ".xlsx");
    };

    /**
     * Exporta reporte talla / color / combinación por marcas (una hoja con secciones).
     * payload: { desde, hasta, marcas, almacen, incluirCombinacion, tallas[], colores[], combinaciones[] }
     * Cada fila de métrica: { nombre, cantidadVendida|CantidadVendida, totalIngresos|TotalIngresos, porcentaje|Porcentaje }
     */
    window.exportSeparacionMarcaTallaColorExcel = (payload) => {
        if (!payload || typeof payload !== 'object') return;

        const desde = String(payload.desde ?? payload.Desde ?? '');
        const hasta = String(payload.hasta ?? payload.Hasta ?? '');
        const marcas = String(payload.marcas ?? payload.Marcas ?? '');
        const almacen = String(payload.almacen ?? payload.Almacen ?? 'General (todos)');
        const incluirCombinacion = !!(payload.incluirCombinacion ?? payload.IncluirCombinacion);

        const mapMetricas = (arr) => (Array.isArray(arr) ? arr : []).map(r => ({
            nombre: r.nombre ?? r.Nombre ?? '',
            cantidad: toNum(r.cantidadVendida ?? r.CantidadVendida ?? r.cantidad ?? r.Cantidad),
            importe: toNum(r.totalIngresos ?? r.TotalIngresos ?? r.importe ?? r.Importe),
            porcentaje: toNum(r.porcentaje ?? r.Porcentaje)
        }));

        const tallas = mapMetricas(payload.tallas ?? payload.Tallas);
        const colores = mapMetricas(payload.colores ?? payload.Colores);
        const combos = mapMetricas(payload.combinaciones ?? payload.Combinaciones);

        if (tallas.length === 0 && colores.length === 0 && (!incluirCombinacion || combos.length === 0)) return;

        const header = [
            ['Talla y color por marca'],
            [`Rango: ${desde} a ${hasta}`],
            [`Marcas: ${marcas}`],
            [`Ámbito: ${almacen}`],
            [`Combinación talla+color: ${incluirCombinacion ? 'Sí' : 'No'}`],
            []
        ];

        const cols = ['Nombre', 'Cantidad vendida', 'Total importe (S/)', '% sobre sección'];

        const aoa = [...header];
        const appendSection = (title, rows) => {
            aoa.push([title]);
            aoa.push(cols);
            rows.forEach(x => aoa.push([x.nombre, x.cantidad, x.importe, x.porcentaje]));
            aoa.push([]);
        };

        appendSection('TALLA', tallas);
        appendSection('COLOR', colores);
        if (incluirCombinacion) {
            appendSection('COMBINACIÓN TALLA — COLOR', combos);
        }

        const wb = XLSX.utils.book_new();
        const ws = XLSX.utils.aoa_to_sheet(aoa);
        ws['!cols'] = [{ wch: 36 }, { wch: 16 }, { wch: 18 }, { wch: 14 }];
        XLSX.utils.book_append_sheet(wb, ws, 'Reporte');
        XLSX.writeFile(wb, `SeparacionMarcaTallaColor_${desde}_a_${hasta}.xlsx`);
    };
})();