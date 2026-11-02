function _readPayload(payload) {
    if (!payload) return { labels: [], ventas: [], utilidades: [] };

    // Preferir payload ya reducido desde Blazor; tolerar DTOs con camel/PascalCase.
    if (Array.isArray(payload.labels)) {
        return {
            labels: payload.labels,
            ventas: payload.ventas ?? [],
            utilidades: payload.utilidades ?? []
        };
    }

    const items = Array.isArray(payload) ? payload : [];
    return {
        labels: items.map(i => i.tienda ?? i.Tienda ?? ''),
        ventas: items.map(i => Number(i.venta ?? i.Venta ?? 0)),
        utilidades: items.map(i => Number(i.utilidad ?? i.Utilidad ?? 0))
    };
}

function _destroyChart(canvasId) {
    if (typeof Chart === 'undefined') return;

    const byId = Chart.getChart(canvasId);
    if (byId) byId.destroy();

    const canvas = document.getElementById(canvasId);
    if (canvas) {
        const byCanvas = Chart.getChart(canvas);
        if (byCanvas) byCanvas.destroy();
    }

    if (window._utilidadTiendasChart) {
        try { window._utilidadTiendasChart.destroy(); } catch { /* ignore */ }
        window._utilidadTiendasChart = null;
    }
}

export function renderUtilidadTiendasChart(canvasId, payload) {
    if (typeof Chart === 'undefined') return;

    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    _destroyChart(canvasId);

    const { labels, ventas, utilidades } = _readPayload(payload);
    if (!labels.length) return;

    const wrapper = canvas.parentElement;
    if (wrapper) {
        wrapper.style.height = '360px';
        wrapper.style.position = 'relative';
    }
    canvas.style.width = '100%';
    canvas.style.height = '100%';

    window._utilidadTiendasChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels,
            datasets: [
                {
                    label: 'Venta',
                    data: ventas,
                    backgroundColor: 'rgba(37, 99, 235, 0.75)',
                    borderWidth: 0,
                    maxBarThickness: 48
                },
                {
                    label: 'Utilidad',
                    data: utilidades,
                    backgroundColor: 'rgba(16, 185, 129, 0.75)',
                    borderWidth: 0,
                    maxBarThickness: 48
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: false,
            plugins: {
                legend: { position: 'top' },
                tooltip: {
                    callbacks: {
                        label(ctx) {
                            const v = ctx.parsed?.y ?? 0;
                            return `${ctx.dataset.label}: S/ ${Number(v).toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
                        }
                    }
                }
            },
            scales: {
                x: {
                    ticks: {
                        autoSkip: true,
                        maxRotation: 45,
                        minRotation: 0
                    },
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    grid: { color: 'rgba(0,0,0,0.08)' }
                }
            }
        }
    });
}

export function destroyUtilidadTiendasChart(canvasId) {
    _destroyChart(canvasId);
}

/** Devuelve data URL PNG del canvas (para embeber en PDF). */
export function getChartImageDataUrl(canvasId) {
    const c = document.getElementById(canvasId);
    if (!c) return null;
    try {
        return c.toDataURL('image/png');
    } catch {
        return null;
    }
}

/** @deprecated Preferir getChartImageDataUrl + img en el HTML del PDF. */
export function injectChartImageForPdf(canvasId) {
    const holder = document.getElementById('chart-holder');
    const dataUrl = getChartImageDataUrl(canvasId);
    if (!holder || !dataUrl) return;
    holder.innerHTML = '';
    const img = new Image();
    img.src = dataUrl;
    img.style.width = '100%';
    img.style.height = 'auto';
    holder.appendChild(img);
}
