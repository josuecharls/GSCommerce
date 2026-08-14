function showConsoleWarning() {
    console.log(
        "%c⚠️ ADVERTENCIA DE SEGURIDAD\n" +
        "Esta función del navegador está pensada para desarrolladores.\n" +
        "No ingreses código aquí a menos que sepas exactamente lo que estás haciendo.",
        "color: orange; font-size: 16px; font-weight: bold; line-height: 1.5;"
    );
}

if (window.location.hostname !== "localhost") {
    let warningShown = false;

    const showWarningOnce = () => {
        if (!warningShown) {
            showConsoleWarning();
            warningShown = true;

            // Opcional: Mostrar solo una vez cada 30 segundos si siguen abriendo consola
            setTimeout(() => { warningShown = false; }, 30000);
        }
    };

    // Detección mejorada sin spam
    const detector = () => {
        const element = new Image();
        let devtoolsOpen = false;

        Object.defineProperty(element, "id", {
            get() {
                if (!devtoolsOpen) {
                    devtoolsOpen = true;
                    showWarningOnce();
                }
                return "";
            }
        });

        // Verificar cada 2 segundos (menos intrusivo)
        setInterval(() => {
            devtoolsOpen = false;
            console.log(element);
        }, 2000);
    };

    // Iniciar detección después de un breve delay
    setTimeout(detector, 1000);

    // Event listeners para métodos comunes de abrir consola
    const shortcuts = ['F12', 'Ctrl+Shift+I', 'Ctrl+Shift+J', 'Ctrl+Shift+C'];

    window.addEventListener("keydown", event => {
        const keyCombo = [
            event.ctrlKey ? 'Ctrl+' : '',
            event.shiftKey ? 'Shift+' : '',
            event.key
        ].join('');

        if (shortcuts.includes(keyCombo) || shortcuts.includes(event.key)) {
            showWarningOnce();
        }
    });
}

function resizeImage(file, width, height, dotNetObject) {
    try {
        if (!(file instanceof Blob)) {
            console.error("El archivo seleccionado no es un Blob válido.");
            return;
        }

        const reader = new FileReader();
        reader.onload = function (event) {
            try {
                const img = new Image();
                img.onload = function () {
                    const canvas = document.createElement('canvas');
                    const ctx = canvas.getContext('2d');
                    canvas.width = width;
                    canvas.height = height;
                    ctx.drawImage(img, 0, 0, width, height);

                    try {
                        const dataUrl = canvas.toDataURL('image/jpeg', 0.7);
                        console.log("Imagen redimensionada correctamente.");
                        dotNetObject.invokeMethodAsync('SetResizedImage', dataUrl);
                    } catch (error) {
                        console.error("Error al convertir la imagen a Base64: ", error);
                    }
                };

                img.onerror = function () {
                    console.error("Error al cargar la imagen.");
                };

                img.src = event.target.result;
            } catch (error) {
                console.error("Error dentro de FileReader.onload: ", error);
            }
        };

        reader.onerror = function (error) {
            console.error("Error al leer el archivo: ", error);
        };

        reader.readAsDataURL(file);
    } catch (error) {
        console.error("Error en resizeImage:", error);
    }
}

window.downloadFileFromBytes = (fileName, contentType, base64Data) => {
    try {
        const link = document.createElement('a');
        link.href = `data:${contentType};base64,${base64Data}`;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    } catch (error) {
        console.error('No se pudo descargar el archivo', error);
    }
};

// Versión de este site.js. Debe coincidir con el ?v= de index.html y con
// EsperarSiteJs.VersionEsperada en Home.razor. Si el navegador sirve un
// site.js viejo (index.html cacheado), C# lo detecta y recarga con cache-bust.
window.__DASH_VER = 3;
window.getDashVersion = () => (window.__DASH_VER || 0);

// Espera a que Chart.js esté disponible (se carga por CDN de forma asíncrona).
function _esperarChartJs(intentos = 40) {
    return new Promise((resolve) => {
        const check = () => {
            if (typeof Chart !== 'undefined') { resolve(true); return; }
            if (intentos-- <= 0) { resolve(false); return; }
            setTimeout(check, 50);
        };
        check();
    });
}

const _fmtSoles = (v) => 'S/ ' + Number(v).toLocaleString('es-PE', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
const _fmtSoles2 = (v) => 'S/ ' + Number(v).toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const _paletaDashboard = [
    'rgba(59,130,246,0.85)',  // azul
    'rgba(16,185,129,0.85)',  // verde
    'rgba(99,102,241,0.85)',  // indigo
    'rgba(245,158,11,0.85)',  // ámbar
    'rgba(236,72,153,0.85)',  // rosa
    'rgba(14,165,233,0.85)',  // celeste
    'rgba(139,92,246,0.85)',  // violeta
    'rgba(20,184,166,0.85)'   // teal
];

// Plugin liviano para mostrar el valor encima de cada barra (sin librería extra).
const _etiquetasBarras = {
    id: 'etiquetasBarras',
    afterDatasetsDraw(chart) {
        const { ctx } = chart;
        const ds = chart.data.datasets[0];
        if (!ds) return;
        const meta = chart.getDatasetMeta(0);
        ctx.save();
        ctx.font = '600 11px system-ui, sans-serif';
        ctx.fillStyle = '#334155';
        ctx.textAlign = 'center';
        meta.data.forEach((bar, i) => {
            const val = ds.data[i];
            if (val == null) return;
            ctx.fillText(_fmtSoles(val), bar.x, bar.y - 6);
        });
        ctx.restore();
    }
};

function _destruir(canvasId) {
    const c = document.getElementById(canvasId);
    if (!c) return null;
    const previo = Chart.getChart(canvasId);
    if (previo) previo.destroy();
    return c;
}

// Renderiza el dashboard de inicio: gráfico de barras + dona de participación.
window.renderVentasDashboard = async (barId, donutId, items, highlightId) => {
    const ok = await _esperarChartJs();
    if (!ok) return;

    const labels = items.map(i => i.nombreAlmacen);
    const data = items.map(i => i.total);

    // --- Barras: ventas por almacén ---
    const barCtx = _destruir(barId);
    if (barCtx) {
        const bg = items.map((i, idx) => i.idAlmacen === highlightId ? 'rgba(37,99,235,0.95)' : _paletaDashboard[idx % _paletaDashboard.length]);
        new Chart(barCtx, {
            type: 'bar',
            data: { labels, datasets: [{ label: 'Ventas (S/)', data, backgroundColor: bg, borderWidth: 0, borderRadius: 8, maxBarThickness: 64 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                layout: { padding: { top: 24 } },
                animation: { duration: 500 },
                plugins: {
                    legend: { display: false },
                    tooltip: { callbacks: { label: (c) => _fmtSoles2(c.parsed.y) } }
                },
                scales: {
                    y: { beginAtZero: true, grid: { color: 'rgba(15,23,42,0.06)' }, border: { display: false }, ticks: { color: '#94a3b8', callback: (v) => _fmtSoles(v) } },
                    x: { grid: { display: false }, border: { display: false }, ticks: { color: '#64748b', font: { weight: '600' } } }
                }
            },
            plugins: [_etiquetasBarras]
        });
    }

    // --- Dona: participación por almacén ---
    const donutCtx = _destruir(donutId);
    if (donutCtx) {
        const colores = items.map((i, idx) => _paletaDashboard[idx % _paletaDashboard.length]);
        new Chart(donutCtx, {
            type: 'doughnut',
            data: { labels, datasets: [{ data, backgroundColor: colores, borderColor: '#fff', borderWidth: 2, hoverOffset: 6 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '62%',
                animation: { duration: 500 },
                plugins: {
                    legend: { position: 'right', labels: { boxWidth: 12, boxHeight: 12, usePointStyle: true, pointStyle: 'circle', color: '#475569', font: { size: 12 } } },
                    tooltip: {
                        callbacks: {
                            label: (c) => {
                                const total = c.dataset.data.reduce((a, b) => a + b, 0) || 1;
                                const pct = (c.parsed / total * 100).toFixed(1);
                                return `${c.label}: ${_fmtSoles2(c.parsed)} (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
    }
};

// Compatibilidad: mantiene la función anterior por si se usa en otro lugar.
window.renderVentasDiaChart = (canvasId, items, highlightId) =>
    window.renderVentasDashboard(canvasId, null, items, highlightId);

// Función segura para enfocar inputs por data-item (reemplaza eval)
window.focusInputByItem = (item) => {
    setTimeout(function () {
        var input = document.querySelector(`input[data-item="${item}"]`);
        if (input) {
            input.focus();
            input.select();
        }
    }, 50);
};

// Enfocar por id como respaldo seguro
window.focusElementById = (id) => {
    setTimeout(() => document.getElementById(id)?.focus(), 30);
};

// Determinar si el foco actual está en un input/campo interactivo
window.isActiveElementInput = () => {
    const el = document.activeElement;
    if (!el) return false;
    if (el.isContentEditable) return true;
    const tag = el.tagName;
    return tag === 'INPUT' || tag === 'SELECT' || tag === 'TEXTAREA' || tag === 'BUTTON';
};

// Quitar el foco actual (deja el foco "en el aire")
window.blurActiveElement = () => {
    try {
        if (document.activeElement && document.activeElement.blur) {
            document.activeElement.blur();
        }
    } catch (e) { }
};

// Seleccionar todo el texto de un input (recibe ElementReference)
window.selectInputText = (element) => {
    setTimeout(() => {
        if (element && element.select) {
            element.select();
        }
    }, 100);
};

// Enfocar y seleccionar texto por id
window.focusAndSelectElementById = (id) => {
    setTimeout(() => {
        const element = document.getElementById(id);
        if (element) {
            element.focus();
            element.select();
        }
    }, 100);
};