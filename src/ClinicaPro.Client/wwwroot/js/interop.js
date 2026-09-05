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
            // Algunos navegadores no disparan afterprint de forma consistente.
            window.setTimeout(cleanup, 1000);
        }, 50);
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
