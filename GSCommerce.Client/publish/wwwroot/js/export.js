// wwwroot/js/export.js
(function () {
    function toNum(v) {
        // Limpia strings "S/ 1.234,56" -> num. Si no es finito, devuelve 0.
        if (typeof v === 'string') v = v.replace(/[^\d.-]/g, '');
        const n = Number(v);
        return Number.isFinite(n) ? n : 0;
    }

    window.exportTop10Excel = (rows, desde, hasta, almacenLabel, lineasLabel, familiasLabel, title, fileLabel) => {
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
            : 'Top 10 Artículos';
        const fileName = typeof fileLabel === 'string' && fileLabel.trim().length > 0
            ? fileLabel.trim()
            : 'Top10_Articulos';

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

        XLSX.utils.book_append_sheet(wb, ws, "Top10");
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
})();