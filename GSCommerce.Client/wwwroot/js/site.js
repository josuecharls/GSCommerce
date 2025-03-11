function resizeImage(file, width, height, dotNetObject) {
    try {
        if (!(file instanceof Blob)) {
            console.error("El archivo seleccionado no es un Blob válido.");
            return;
        }

        const reader = new FileReader();
        reader.onload = function (event) {
            const img = new Image();
            img.onload = function () {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                canvas.width = width;
                canvas.height = height;
                ctx.drawImage(img, 0, 0, width, height);

                // Intentar convertir la imagen a Base64
                try {
                    const dataUrl = canvas.toDataURL('image/jpeg', 0.8); // Comprime al 70%
                    dotNetObject.invokeMethodAsync('SetResizedImage', dataUrl);
                } catch (error) {
                    console.error("Error al convertir la imagen a Base64: ", error);
                }
            };
            img.src = event.target.result;
        };

        reader.onerror = function (error) {
            console.error("Error al leer el archivo: ", error);
        };

        reader.readAsDataURL(file);

    } catch (error) {
        console.error("Error en resizeImage:", error);
    }
}