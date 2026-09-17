// Interop deliberadamente pequeño: almacenamiento de sesión, impresión segura,
// foco accesible, visibilidad de página y temporizador de inactividad.
// No contiene reglas clínicas ni acceso a datos de negocio.

window.clinicaProStorage = {
    get: function (key) {
        try {
            return window.sessionStorage.getItem(key) || window.localStorage.getItem(key);
        } catch (_) {
            return null;
        }
    },
    isPersistent: function (key) {
        try {
            return window.localStorage.getItem(key) !== null;
        } catch (_) {
            return false;
        }
    },
    set: function (key, value, persistente) {
        var recordar = persistente === true;
        try {
            if (recordar) {
                window.localStorage.setItem(key, value);
                window.sessionStorage.removeItem(key);
            } else {
                window.sessionStorage.setItem(key, value);
                window.localStorage.removeItem(key);
            }
        } catch (_) {
            // Si el navegador bloquea almacenamiento, el servicio .NET mantiene
            // la sesión solo en memoria hasta que la página se recargue.
        }
    },
    remove: function (key) {
        try {
            window.localStorage.removeItem(key);
            window.sessionStorage.removeItem(key);
        } catch (_) {
        }
    }
};

window.clinicaProImpresion = {
    imprimirElemento: function (id) {
        var target = document.getElementById(id);
        if (!target) return;

        function cleanup() {
            document.body.classList.remove('cp-print-mode');
            target.classList.remove('cp-print-target');
        }

        document.body.classList.add('cp-print-mode');
        target.classList.add('cp-print-target');
        window.addEventListener('afterprint', cleanup, { once: true });

        window.setTimeout(function () {
            window.print();
            window.setTimeout(cleanup, 1000);
        }, 50);
    },

    // Genera un informe independiente a partir de datos estructurados.
    // No captura la pantalla ni copia el DOM de la aplicación.
    generarReporteCitas: function (data) {
        if (!data) return;

        function esc(value) {
            return String(value == null ? '' : value)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
        }

        function numero(value) {
            var parsed = Number(value);
            return Number.isFinite(parsed) ? parsed : 0;
        }

        var resumen = data.resumen || {};
        var distribucion = Array.isArray(data.distribucion) ? data.distribucion : [];
        var citas = Array.isArray(data.citas) ? data.citas : [];
        var clinicName = esc(data.clinicName || 'Clínica Quantium');

        var distribucionRows = distribucion.length
            ? distribucion.map(function (item) {
                return '<tr>' +
                    '<td>' + esc(item.estado) + '</td>' +
                    '<td class="num">' + numero(item.cantidad) + '</td>' +
                    '<td class="num">' + numero(item.porcentaje).toLocaleString('es-GT', { maximumFractionDigits: 1 }) + '%</td>' +
                    '</tr>';
            }).join('')
            : '<tr><td colspan="3" class="empty">Sin registros para los filtros aplicados.</td></tr>';

        var detalleRows = citas.length
            ? citas.map(function (cita, index) {
                return '<tr>' +
                    '<td class="num">' + (index + 1) + '</td>' +
                    '<td><strong>' + esc(cita.fecha) + '</strong><br><span class="muted">' + esc(cita.horaInicio) + ' – ' + esc(cita.horaFin) + '</span></td>' +
                    '<td>' + esc(cita.paciente) + '</td>' +
                    '<td>' + esc(cita.medico) + '</td>' +
                    '<td class="motivo">' + esc(cita.motivo) + '</td>' +
                    '<td>' + esc(cita.estado) + '</td>' +
                    '<td class="num">' + numero(cita.reprogramaciones) + '</td>' +
                    '</tr>';
            }).join('')
            : '<tr><td colspan="7" class="empty">No hay citas que coincidan con los filtros aplicados.</td></tr>';

        var html = '<!doctype html><html lang="es"><head><meta charset="utf-8">' +
            '<title>' + clinicName + ' - Informe de citas</title>' +
            '<style>' +
            '@page{size:A4 landscape;margin:12mm 10mm 14mm}' +
            '*{box-sizing:border-box}body{font-family:Arial,Helvetica,sans-serif;color:#172033;margin:0;font-size:10px;line-height:1.35}' +
            '.header{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #238b70;padding-bottom:8px;margin-bottom:10px}' +
            '.brand{font-size:13px;font-weight:700;color:#238b70;letter-spacing:.04em;text-transform:uppercase}.title{font-size:22px;margin:2px 0 0}.meta{text-align:right;color:#657086;font-size:9px}' +
            '.filters{display:grid;grid-template-columns:1.2fr 1fr .8fr;gap:8px;margin:10px 0}.filter{border:1px solid #d9dee7;border-radius:6px;padding:7px 9px}.label{display:block;color:#657086;font-size:8px;text-transform:uppercase;letter-spacing:.06em;margin-bottom:2px}' +
            '.kpis{display:grid;grid-template-columns:repeat(6,1fr);gap:7px;margin:10px 0 12px}.kpi{border:1px solid #d9dee7;border-radius:6px;padding:8px;background:#f8fafb}.kpi .v{font-size:18px;font-weight:700;color:#172033}.kpi .l{font-size:8px;color:#657086;text-transform:uppercase}' +
            'h2{font-size:13px;margin:14px 0 6px;color:#172033}.section{break-inside:avoid;margin-bottom:12px}' +
            'table{width:100%;border-collapse:collapse;table-layout:auto}th{background:#eef5f2;color:#354357;text-align:left;text-transform:uppercase;font-size:8px;letter-spacing:.04em;padding:6px;border:1px solid #d9dee7}td{padding:6px;border:1px solid #d9dee7;vertical-align:top}tbody tr:nth-child(even){background:#fbfcfd}.num{text-align:right;white-space:nowrap}.muted{color:#657086}.motivo{max-width:270px}.empty{text-align:center;color:#657086;padding:14px}' +
            '.distribution{width:52%;min-width:360px}.footer{margin-top:10px;padding-top:6px;border-top:1px solid #d9dee7;color:#657086;font-size:8px;display:flex;justify-content:space-between}' +
            '@media print{body{-webkit-print-color-adjust:exact;print-color-adjust:exact}thead{display:table-header-group}tr{break-inside:avoid}}' +
            '</style></head><body>' +
            '<div class="header"><div><div class="brand">' + clinicName + '</div><div class="title">' + esc(data.titulo || 'Informe de citas') + '</div></div><div class="meta"><strong>Fecha de generación</strong><br>' + esc(data.generado) + '<br><br>Documento generado por el sistema</div></div>' +
            '<div class="filters"><div class="filter"><span class="label">Período</span><strong>' + esc(data.periodo) + '</strong></div><div class="filter"><span class="label">Médico</span><strong>' + esc(data.medico) + '</strong></div><div class="filter"><span class="label">Estado</span><strong>' + esc(data.estado) + '</strong></div></div>' +
            '<div class="kpis">' +
                '<div class="kpi"><div class="v">' + numero(resumen.total) + '</div><div class="l">Total</div></div>' +
                '<div class="kpi"><div class="v">' + numero(resumen.pendientes) + '</div><div class="l">Pendientes</div></div>' +
                '<div class="kpi"><div class="v">' + numero(resumen.atendidas) + '</div><div class="l">Atendidas</div></div>' +
                '<div class="kpi"><div class="v">' + numero(resumen.canceladas) + '</div><div class="l">Canceladas</div></div>' +
                '<div class="kpi"><div class="v">' + numero(resumen.noPresentadas) + '</div><div class="l">No presentadas</div></div>' +
                '<div class="kpi"><div class="v">' + numero(resumen.reprogramadas) + '</div><div class="l">Reprogramadas</div></div>' +
            '</div>' +
            '<div class="section distribution"><h2>Distribución por estados</h2><table><thead><tr><th>Estado</th><th class="num">Cantidad</th><th class="num">Porcentaje</th></tr></thead><tbody>' + distribucionRows + '</tbody></table></div>' +
            '<div class="section"><h2>Detalle de citas (' + citas.length + ')</h2><table><thead><tr><th>#</th><th>Fecha y hora</th><th>Paciente</th><th>Médico</th><th>Motivo de consulta</th><th>Estado</th><th class="num">Reprog.</th></tr></thead><tbody>' + detalleRows + '</tbody></table></div>' +
            '<div class="footer"><span>' + clinicName + ' · Informe de citas</span><span>Filtros aplicados al momento de exportar</span></div>' +
            '</body></html>';

        var blob = new Blob([html], { type: 'text/html;charset=utf-8' });
        var url = URL.createObjectURL(blob);
        var frame = document.createElement('iframe');
        frame.setAttribute('aria-hidden', 'true');
        frame.style.position = 'fixed';
        frame.style.right = '0';
        frame.style.bottom = '0';
        frame.style.width = '1px';
        frame.style.height = '1px';
        frame.style.border = '0';
        frame.style.opacity = '0';
        frame.src = url;

        function cleanup() {
            window.setTimeout(function () {
                try { frame.remove(); } catch (_) {}
                URL.revokeObjectURL(url);
            }, 1000);
        }

        frame.onload = function () {
            try {
                var printable = frame.contentWindow;
                printable.focus();
                printable.addEventListener('afterprint', cleanup, { once: true });
                printable.print();
                window.setTimeout(cleanup, 30000);
            } catch (_) {
                cleanup();
            }
        };

        document.body.appendChild(frame);
    }
};

window.clinicaProUi = {
    focusFirst: function (container) {
        if (!container) return;

        var preferred = container.querySelector('[autofocus]') || container.querySelector([
            'input:not([disabled]):not([type="hidden"])',
            'select:not([disabled])',
            'textarea:not([disabled])'
        ].join(','));

        var fallback = container.querySelector([
            'button:not([disabled])',
            'a[href]',
            '[tabindex]:not([tabindex="-1"])'
        ].join(','));

        (preferred || fallback || container).focus({ preventScroll: true });
    }
};

window.clinicaProPage = {
    isVisible: function () {
        return document.visibilityState === 'visible';
    }
};

window.clinicaProIdle = (function () {
    var dotnet = null;
    var warningTimer = null;
    var expireTimer = null;
    var warningMs = 25 * 60 * 1000;
    var graceMs = 5 * 60 * 1000;
    var lastReset = 0;
    var events = ['pointerdown', 'keydown', 'touchstart', 'scroll', 'mousemove'];

    function clearTimers() {
        if (warningTimer) window.clearTimeout(warningTimer);
        if (expireTimer) window.clearTimeout(expireTimer);
        warningTimer = null;
        expireTimer = null;
    }

    function schedule() {
        clearTimers();
        warningTimer = window.setTimeout(function () {
            if (dotnet) dotnet.invokeMethodAsync('AvisarInactividad');
            expireTimer = window.setTimeout(function () {
                if (dotnet) dotnet.invokeMethodAsync('ExpirarPorInactividad');
            }, graceMs);
        }, warningMs);
    }

    function activity() {
        var now = Date.now();
        if (now - lastReset < 5000) return;
        lastReset = now;
        schedule();
    }

    return {
        register: function (dotnetRef, warnAfterMs, graceAfterMs) {
            this.unregister();
            dotnet = dotnetRef;
            warningMs = warnAfterMs || warningMs;
            graceMs = graceAfterMs || graceMs;
            lastReset = Date.now();
            events.forEach(function (eventName) {
                window.addEventListener(eventName, activity, { passive: true });
            });
            schedule();
        },
        reset: function () {
            lastReset = Date.now();
            schedule();
        },
        unregister: function () {
            clearTimers();
            events.forEach(function (eventName) {
                window.removeEventListener(eventName, activity);
            });
            dotnet = null;
        }
    };
})();
