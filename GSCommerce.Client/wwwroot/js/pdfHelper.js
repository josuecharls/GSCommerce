window.generarPDFDesdeHTML = async function (htmlString, nombreArchivo) {
    const { jsPDF } = window.jspdf;

    const pdf = new jsPDF({
        orientation: "portrait",
        unit: "mm",
        format: "a4"
    });

    // Creamos un contenedor temporal oculto
    const tempDiv = document.createElement("div");
    tempDiv.innerHTML = htmlString;
    tempDiv.style.position = "absolute";
    tempDiv.style.left = "-9999px";
    document.body.appendChild(tempDiv);

    const canvas = await html2canvas(tempDiv, {
        scale: 2,
        useCORS: true
    });

    const imgData = canvas.toDataURL("image/png");
    const imgProps = pdf.getImageProperties(imgData);
    const pageWidth = pdf.internal.pageSize.getWidth();
    const imgWidth = pageWidth - 20;
    const imgHeight = (imgProps.height * imgWidth) / imgProps.width;

    pdf.addImage(imgData, "PNG", 10, 10, imgWidth, imgHeight);
    pdf.save(nombreArchivo + ".pdf");

    document.body.removeChild(tempDiv);
};
