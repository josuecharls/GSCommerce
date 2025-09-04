if (window.location.hostname !== "localhost") {
    console.log("%c¡Detente!", "color:red; font-size:40px; font-weight:bold;");
    console.log(
        "Esta función del navegador está pensada para desarrolladores. Si alguien te pidió copiar y pegar algo aquí para habilitar funciones o \"hackear\" una cuenta, es un engaño."
    );
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
