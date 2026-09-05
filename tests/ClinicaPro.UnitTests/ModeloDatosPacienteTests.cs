using ClinicaPro.Client.Features.Paciente.Components;
using ClinicaPro.Contracts.Auth;
using ClinicaPro.Contracts.Pacientes;

namespace ClinicaPro.UnitTests;

/// <summary>
/// Validación del formulario del paciente, que respalda tanto el registro
/// público como la edición del propio perfil. Es lógica pura: no toca red
/// ni base de datos, así que se puede probar sin levantar nada.
/// </summary>
public sealed class ModeloDatosPacienteTests
{
    private static ModeloDatosPaciente ModeloValido() => new()
    {
        Email = "ana.lopez@correo.com",
        Password = "Clinica2026!",
        PasswordConfirmacion = "Clinica2026!",
        Nombres = "Ana Lucía",
        Apellidos = "López Ramírez",
        Documento = "2547889120101",
        FechaNacimiento = new DateTime(1994, 3, 18),
        Sexo = "F",
        Telefono = "5512 4478"
    };

    // ── Campos obligatorios ────────────────────────────────────────────────

    [Fact]
    public void Validar_ConModeloCompleto_NoDevuelveErrores()
    {
        var errores = ModeloValido().Validar(incluyeCredenciales: true);

        Assert.Empty(errores);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_SinNombres_SenalaElCampo(string nombres)
    {
        var modelo = ModeloValido();
        modelo.Nombres = nombres;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Nombres)));
    }

    [Fact]
    public void Validar_SinApellidos_SenalaElCampo()
    {
        var modelo = ModeloValido();
        modelo.Apellidos = "";

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Apellidos)));
    }

    [Fact]
    public void Validar_NombresMasLargosQueElLimite_SenalaElCampo()
    {
        var modelo = ModeloValido();
        modelo.Nombres = new string('a', ModeloDatosPaciente.NombresMaxLength + 1);

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Nombres)));
    }

    // ── Credenciales ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("dos@@arrobas.com")]
    [InlineData("sin punto@dominio")]
    public void Validar_ConCorreoMalFormado_SenalaElCampo(string email)
    {
        var modelo = ModeloValido();
        modelo.Email = email;

        var errores = modelo.Validar(incluyeCredenciales: true);

        Assert.True(errores.ContainsKey(nameof(modelo.Email)));
    }

    [Fact]
    public void Validar_ConContrasenasQueNoCoinciden_SenalaLaConfirmacion()
    {
        var modelo = ModeloValido();
        modelo.PasswordConfirmacion = "OtraCosa2026!";

        var errores = modelo.Validar(incluyeCredenciales: true);

        Assert.True(errores.ContainsKey(nameof(modelo.PasswordConfirmacion)));
    }

    /// <summary>
    /// Al editar el perfil no se piden credenciales, así que un modelo sin
    /// correo ni contraseña debe pasar. Si esto falla, el paciente no puede
    /// guardar sus datos.
    /// </summary>
    [Fact]
    public void Validar_SinCredenciales_IgnoraCorreoYContrasena()
    {
        var modelo = ModeloValido();
        modelo.Email = "";
        modelo.Password = "";
        modelo.PasswordConfirmacion = "";

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.Empty(errores);
    }

    // ── Fecha de nacimiento ────────────────────────────────────────────────

    [Fact]
    public void Validar_ConFechaDeNacimientoFutura_SenalaElCampo()
    {
        var modelo = ModeloValido();
        modelo.FechaNacimiento = DateTime.Today.AddDays(1);

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.FechaNacimiento)));
    }

    [Fact]
    public void Validar_ConAnioAnteriorA1900_SenalaElCampo()
    {
        var modelo = ModeloValido();
        modelo.FechaNacimiento = new DateTime(1899, 12, 31);

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.FechaNacimiento)));
    }

    [Fact]
    public void Validar_SinFechaDeNacimiento_NoEsError()
    {
        var modelo = ModeloValido();
        modelo.FechaNacimiento = null;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.Empty(errores);
    }

    // ── Sexo: debe coincidir con Domain.Entities.Paciente ──────────────────

    [Theory]
    [InlineData("M")]
    [InlineData("F")]
    [InlineData("X")]
    [InlineData("")]
    public void Validar_ConSexoQueAceptaElDominio_NoEsError(string sexo)
    {
        var modelo = ModeloValido();
        modelo.Sexo = sexo;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.False(errores.ContainsKey(nameof(modelo.Sexo)));
    }

    /// <summary>
    /// El dominio solo acepta M, F o X. Si el formulario mandara la palabra
    /// completa, el guardado fallaría con DomainException.
    /// </summary>
    [Theory]
    [InlineData("Femenino")]
    [InlineData("Masculino")]
    [InlineData("Otro")]
    public void Validar_ConSexoEnPalabraCompleta_SenalaElCampo(string sexo)
    {
        var modelo = ModeloValido();
        modelo.Sexo = sexo;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Sexo)));
    }

    // ── Teléfono ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("5512 4478")]
    [InlineData("+502 5512-4478")]
    [InlineData("(502) 55124478")]
    public void Validar_ConTelefonoUsual_NoEsError(string telefono)
    {
        var modelo = ModeloValido();
        modelo.Telefono = telefono;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.False(errores.ContainsKey(nameof(modelo.Telefono)));
    }

    [Fact]
    public void Validar_ConTelefonoDeMenosDeOchoDigitos_SenalaElCampo()
    {
        var modelo = ModeloValido();
        modelo.Telefono = "5512447";

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Telefono)));
    }

    [Fact]
    public void Telefono_ConLetras_DescartaLoQueNoSeaDigito()
    {
        var modelo = ModeloValido();
        modelo.Telefono = "5712dasd";

        // El campo no guarda letras: se filtran al escribir, así el paciente
        // ve enseguida qué acepta el sistema en vez de enterarse al enviar.
        Assert.Equal("5712", modelo.Telefono);
    }

    [Fact]
    public void Validar_ConTelefonoIncompleto_SenalaElCampo()
    {
        var modelo = ModeloValido();
        modelo.Telefono = "5712";

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Telefono)));
    }

    [Fact]
    public void Telefono_ConGuionesYEspacios_LosDescarta()
    {
        var modelo = ModeloValido();
        modelo.Telefono = "5512-4478";

        Assert.Equal("55124478", modelo.Telefono);
    }

    // ── DPI ────────────────────────────────────────────────────────────────

    [Fact]
    public void Documento_ConLetras_DescartaLoQueNoSeaDigito()
    {
        var modelo = ModeloValido();
        modelo.Documento = "sdfsdfasasdasdas";

        Assert.Equal(string.Empty, modelo.Documento);
    }

    [Fact]
    public void Validar_SinDocumento_NoEsError()
    {
        // El DPI es opcional: un extranjero se registra sin él.
        var modelo = ModeloValido();
        modelo.Documento = "";

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.Empty(errores);
    }

    [Theory]
    [InlineData("1111111111111")]
    [InlineData("5456465489894")]
    [InlineData("3045291800101")]
    public void Validar_ConDpiDeDigitoDeControlIncorrecto_SenalaElCampo(string dpi)
    {
        var modelo = ModeloValido();
        modelo.Documento = dpi;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.True(errores.ContainsKey(nameof(modelo.Documento)));
    }

    [Theory]
    [InlineData("2547889120101")]
    [InlineData("3045291840101")]
    [InlineData("1234567890101")]
    public void Validar_ConDpiReal_NoSenalaElCampo(string dpi)
    {
        var modelo = ModeloValido();
        modelo.Documento = dpi;

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.False(errores.ContainsKey(nameof(modelo.Documento)));
    }

    [Fact]
    public void EsCuiValido_ConCorrelativoEnCeros_EsFalso()
    {
        // 0 % 11 da 0, así que la fórmula sola aceptaría este número aunque no
        // corresponda a ninguna persona.
        Assert.False(ModeloDatosPaciente.EsCuiValido("0000000000101"));
    }

    [Fact]
    public void Validar_SinTelefono_NoEsError()
    {
        var modelo = ModeloValido();
        modelo.Telefono = "";

        var errores = modelo.Validar(incluyeCredenciales: false);

        Assert.Empty(errores);
    }

    // ── Reglas de contraseña ───────────────────────────────────────────────

    [Fact]
    public void ReglasPasswordFaltantes_ConContrasenaCompleta_NoDevuelveNada()
    {
        Assert.Empty(ModeloDatosPaciente.ReglasPasswordFaltantes("Clinica2026!"));
    }

    [Theory]
    [InlineData("Corta1!")]              // menos de 8
    [InlineData("clinica2026!")]         // sin mayúscula
    [InlineData("CLINICA2026!")]         // sin minúscula
    [InlineData("ClinicaPro!")]          // sin número
    [InlineData("Clinica2026")]          // sin símbolo
    public void ReglasPasswordFaltantes_ConContrasenaIncompleta_DevuelveLoQueFalta(string password)
    {
        Assert.NotEmpty(ModeloDatosPaciente.ReglasPasswordFaltantes(password));
    }

    // ── Construcción de las peticiones a la API ────────────────────────────

    [Fact]
    public void ConstruirRegistro_RecortaEspaciosYConvierteVaciosEnNulos()
    {
        var modelo = ModeloValido();
        modelo.Nombres = "  Ana Lucía  ";
        modelo.Apellidos = "  López  ";
        modelo.Direccion = "   ";
        modelo.Alergias = "";

        var peticion = modelo.ConstruirRegistro();

        Assert.Equal("Ana Lucía", peticion.Nombres);
        Assert.Equal("López", peticion.Apellidos);
        Assert.Null(peticion.Direccion);
        Assert.Null(peticion.Alergias);
    }

    [Fact]
    public void ConstruirActualizacion_ConservaLosDatosClinicos()
    {
        var modelo = ModeloValido();
        modelo.Alergias = "Penicilina";
        modelo.ContactoEmergenciaNombre = "Marta Ramírez";

        var peticion = modelo.ConstruirActualizacion();

        Assert.Equal("Penicilina", peticion.Alergias);
        Assert.Equal("Marta Ramírez", peticion.ContactoEmergenciaNombre);
        Assert.Equal("F", peticion.Sexo);
    }
}
