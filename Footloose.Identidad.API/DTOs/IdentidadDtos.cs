namespace Footloose.Identidad.API.DTOs;

// Servicio 12: Iniciar sesión
public class IniciarSesionRequest
{
    public string MetodoAcceso { get; set; } = "footloose"; // "footloose" o "google"
    public string Email { get; set; } = string.Empty;
    public string? Contrasena { get; set; }
    public string? IdentificadorExterno { get; set; }
    public string? Dispositivo { get; set; }
    public string? Navegador { get; set; }
    public string? DireccionIp { get; set; }
}

// Servicio 14: Actualizar perfil
public class ActualizarPerfilRequest
{
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = "DNI";
    public string NumeroDocumento { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    public bool? AceptaMarketing { get; set; }

    // Datos opcionales de persona jurídica
    public bool EsPersonaJuridica { get; set; } = false;
    public string? NombreCorporativo { get; set; }
    public string? Ruc { get; set; }
    public string? TelefonoCorporativo { get; set; }
}

// Servicio 16: Registrar dirección
public class RegistrarDireccionRequest
{
    public Guid ClienteId { get; set; }
    public Guid UbicacionId { get; set; } // ID del distrito en ubicaciones_geograficas
    public string Alias { get; set; } = string.Empty; // Ej. Casa, Trabajo
    public string DireccionLinea1 { get; set; } = string.Empty; // Calle y número
    public string? Referencia { get; set; } // Información adicional y destinatario
    public string? CodigoPostal { get; set; }
    public bool EsPrincipal { get; set; } = false;
}

// Servicio 18: Registrar tarjeta de crédito
public class RegistrarTarjetaRequest
{
    public Guid ClienteId { get; set; }
    public Guid? MedioPagoId { get; set; }
    public string NumeroTarjeta { get; set; } = string.Empty;
    public string NombreImpreso { get; set; } = string.Empty;
    public int MesExpiracion { get; set; }
    public int AnioExpiracion { get; set; }
    public string CodigoSeguridad { get; set; } = string.Empty; // Solo se valida; nunca se guarda en BD
    public bool EsPredeterminado { get; set; } = false;

    // Dirección de facturación solicitada por el formulario
    public string PaisFacturacion { get; set; } = "Perú";
    public string DepartamentoFacturacion { get; set; } = string.Empty;
    public string ProvinciaFacturacion { get; set; } = string.Empty;
    public string DistritoFacturacion { get; set; } = string.Empty;
    public string? CodigoPostalFacturacion { get; set; }
    public string CalleFacturacion { get; set; } = string.Empty;
    public string NumeroFacturacion { get; set; } = string.Empty;
    public string? InformacionAdicional { get; set; } // Opcional según regla de negocio
}

// Servicio 20: Definir contraseña
public class DefinirContrasenaRequest
{
    public Guid ClienteId { get; set; }
    public string CodigoVerificacion { get; set; } = string.Empty;
    public string NuevaContrasena { get; set; } = string.Empty;
}