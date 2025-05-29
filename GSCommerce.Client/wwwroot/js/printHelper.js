window.imprimirTicket = (html) => {
    const ventana = window.open("", "_blank", "width=400,height=600");
    ventana.document.write(`
        <html>
        <head>
            <title>Impresión de Ticket</title>
            <style>
                body { font-family: monospace; font-size: 12px; padding: 10px; }
                .center { text-align: center; }
                table { width: 100%; border-collapse: collapse; margin-top: 10px; }
                td { padding: 2px 0; }
                .total { font-weight: bold; }
                hr { border: none; border-top: 1px dashed black; }
            </style>
        </head>
        <body onload="window.print(); window.close();">
            ${html}
        </body>
        </html>
    `);
    ventana.document.close();
};