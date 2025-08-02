// Utilidades para impresión de tickets desde el cliente
// Intenta imprimir el ticket de forma directa usando la librería
// JSPrintManager. Si no está disponible o se produce un error,
// se recurre a la impresión clásica del navegador.
window.imprimirTicket = async (html) => {
    if (window.JSPM) {
        try {
            if (JSPM.JSPrintManager.websocket_status !== JSPM.WSStatus.Open) {
                await JSPM.JSPrintManager.autoConnect();
            }

            const cpj = new JSPM.ClientPrintJob();
            cpj.printer = new JSPM.DefaultPrinter();
            const file = new JSPM.PrintFileHTML(html, JSPM.FileSourceType.String, "ticket.html", 1);
            cpj.files.push(file);
            cpj.sendToClient();
            return;
        } catch (error) {
            console.error("Impresión directa falló, usando método de ventana", error);
        }
    }

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

// Imprime texto plano como ticket (tipo POS o térmica)
window.imprimirTicketTexto = (texto) => {
    const win = window.open("", "_blank", "width=400,height=600");
    win.document.write(`<pre style="font-family: monospace; font-size:12px;">${texto}</pre>`);
    win.document.close();
    win.focus();
    win.print();
    win.close();
};