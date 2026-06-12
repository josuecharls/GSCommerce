export function renderUtilidadTiendasChart(canvasId, items) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    // Altura similar al reporte
    canvas.style.height = "360px";

    const labels = items.map(i => i.tienda);
    const ventas = items.map(i => i.venta);
    const utilidades = items.map(i => i.utilidad);

    if (window._utilidadTiendasChart) {
        window._utilidadTiendasChart.data.labels = labels;
        window._utilidadTiendasChart.data.datasets[0].data = ventas;
        window._utilidadTiendasChart.data.datasets[1].data = utilidades;
        window._utilidadTiendasChart.update();
        return;
    }

    window._utilidadTiendasChart = new Chart(canvas, {
        type: 'bar',
        data: {
            labels,
            datasets: [
                { label: '#Total', data: ventas, borderWidth: 1 },
                { label: '#UtilidadTotal', data: utilidades, borderWidth: 1 }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'right' } },
            scales: {
                x: { ticks: { maxRotation: 0, autoSkip: false } },
                y: { beginAtZero: true, grid: { color: 'rgba(0,0,0,0.2)' } }
            }
        }
    });
}

// Coloca la imagen del canvas dentro de #chart-holder para que salga en el PDF
export function injectChartImageForPdf(canvasId) {
    const c = document.getElementById(canvasId);
    const holder = document.getElementById('chart-holder');
    if (!c || !holder) return;
    const img = new Image();
    img.src = c.toDataURL('image/png');
    img.style.width = '100%';
    img.style.height = 'auto';
    holder.innerHTML = '';
    holder.appendChild(img);
}