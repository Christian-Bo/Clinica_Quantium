// Interop mínimo para persistir la sesión (JWT) del lado del navegador.
// No contiene lógica de negocio: solo lee/escribe/borra una clave de localStorage.
window.clinicaProStorage = {
    get: function (key) {
        return window.localStorage.getItem(key);
    },
    set: function (key, value) {
        window.localStorage.setItem(key, value);
    },
    remove: function (key) {
        window.localStorage.removeItem(key);
    }
};
