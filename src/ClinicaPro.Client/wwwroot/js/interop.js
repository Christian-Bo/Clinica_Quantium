// Interop mínimo para persistir la sesión (JWT) del lado del navegador.
// No contiene lógica de negocio: solo lee/escribe/borra una clave.
//
// Dos almacenes según lo que pida el usuario en el login:
//   localStorage   -> "mantener sesión iniciada": sobrevive a cerrar el navegador.
//   sessionStorage -> sesión de una sola pestaña: se borra al cerrarla.
// Todo va envuelto en try/catch porque en modo incógnito o con las cookies
// bloqueadas, acceder al almacenamiento lanza excepción.
window.clinicaProStorage = {
    get: function (key) {
        try {
            return window.sessionStorage.getItem(key) || window.localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    },
    set: function (key, value, persistente) {
        // Sin el tercer argumento se asume persistente, para no cambiar
        // el comportamiento de las pantallas que ya existían.
        var recordar = persistente !== false;
        try {
            if (recordar) {
                window.localStorage.setItem(key, value);
                window.sessionStorage.removeItem(key);
            } else {
                window.sessionStorage.setItem(key, value);
                window.localStorage.removeItem(key);
            }
        } catch (e) {
            // Sin almacenamiento la sesión solo dura lo que dure la página.
        }
    },
    remove: function (key) {
        try {
            window.localStorage.removeItem(key);
            window.sessionStorage.removeItem(key);
        } catch (e) {
        }
    }
};


// Imprime un solo elemento de la página (el código QR de la cita) sin
// arrastrar la barra lateral ni el resto del portal.
window.clinicaProImpresion = {
    imprimirElemento: function (id) {
        var origen = document.getElementById(id);
        if (!origen) {
            return;
        }

        var ventana = window.open('', '_blank', 'width=420,height=640');
        if (!ventana) {
            // Si el navegador bloqueó la ventana emergente, se imprime la página.
            window.print();
            return;
        }

        ventana.document.write(
            '<!DOCTYPE html><html lang="es"><head><meta charset="utf-8">' +
            '<title>Codigo de cita</title><style>' +
            'body{font-family:system-ui,-apple-system,sans-serif;margin:0;padding:28px;' +
            'display:flex;justify-content:center;color:#1f2937}' +
            '*{box-sizing:border-box}svg{display:block;margin:0 auto}' +
            '</style></head><body>' + origen.innerHTML + '</body></html>');
        ventana.document.close();
        ventana.focus();

        // Se le da un instante al navegador para pintar antes de imprimir.
        ventana.setTimeout(function () {
            ventana.print();
            ventana.close();
        }, 250);
    }
};
