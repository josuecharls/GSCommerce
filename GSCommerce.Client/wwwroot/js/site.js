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

window.renderVentasDiaChart = (canvasId, items, highlightId) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;

    const labels = items.map(i => i.nombreAlmacen);
    const data = items.map(i => i.total);
    const bg = items.map(i => i.idAlmacen === highlightId ? 'rgba(255,99,132,0.5)' : 'rgba(54,162,235,0.5)');
    const border = items.map(i => i.idAlmacen === highlightId ? 'rgba(255,99,132,1)' : 'rgba(54,162,235,1)');

    if (window._ventasDiaChart) {
        window._ventasDiaChart.data.labels = labels;
        window._ventasDiaChart.data.datasets[0].data = data;
        window._ventasDiaChart.data.datasets[0].backgroundColor = bg;
        window._ventasDiaChart.data.datasets[0].borderColor = border;
        window._ventasDiaChart.update();
        return;
    }

    window._ventasDiaChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Ventas (S/)',
                data: data,
                backgroundColor: bg,
                borderColor: border,
                borderWidth: 1
            }]
        },
        options: {
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
}

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