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
