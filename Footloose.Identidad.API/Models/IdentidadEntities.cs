using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Footloose.Identidad.API.Models;

[Table("clientes")]
public class Cliente
{
    [Key]
    [Column("id_cliente")]
    public Guid IdCliente { get; set; }

    [Column("nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Column("apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Column("tipo_documento")]
    public string TipoDocumento { get; set; } = "DNI";

    [Column("numero_documento")]
    public string? NumeroDocumento { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("fecha_nacimiento")]
    public DateOnly? FechaNacimiento { get; set; }

    [Column("genero")]
    public string? Genero { get; set; }

    [Column("acepta_marketing")]
    public bool AceptaMarketing { get; set; }

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; }

    [Column("estado")]
    public string Estado { get; set; } = "activo";

    [Column("responsable_modulo")]
    public string ResponsableModulo { get; set; } = "G2 Identidad";
}

[Table("cuentas_acceso")]
public class CuentaAcceso
{
    [Key]
    [Column("id_cuenta")]
    public Guid IdCuenta { get; set; }

    [Column("cliente_id")]
    public Guid ClienteId { get; set; }

    [Column("proveedor_auth")]
    public string ProveedorAuth { get; set; } = "footloose";

    [Column("identificador_externo")]
    public string? IdentificadorExterno { get; set; }

    [Column("email_verificado")]
    public bool EmailVerificado { get; set; }

    [Column("ultimo_acceso")]
    public DateTime? UltimoAcceso { get; set; }

    [Column("intentos_fallidos")]
    public int IntentosFallidos { get; set; }

    [Column("bloqueada_hasta")]
    public DateTime? BloqueadaHasta { get; set; }

    [Column("responsable_modulo")]
    public string ResponsableModulo { get; set; } = "G2 Identidad";
}

[Table("sesiones")]
public class Sesion
{
    [Key]
    [Column("id_sesion")]
    public Guid IdSesion { get; set; }

    [Column("cuenta_id")]
    public Guid CuentaId { get; set; }

    [Column("dispositivo")]
    public string? Dispositivo { get; set; }

    [Column("navegador")]
    public string? Navegador { get; set; }

    [Column("direccion_ip")]
    public string? DireccionIp { get; set; }

    [Column("fecha_inicio")]
    public DateTime FechaInicio { get; set; }

    [Column("fecha_expiracion")]
    public DateTime FechaExpiracion { get; set; }

    [Column("activa")]
    public bool Activa { get; set; }

    [Column("responsable_modulo")]
    public string ResponsableModulo { get; set; } = "G2 Identidad";
}

[Table("ubicaciones_geograficas")]
public class UbicacionGeografica
{
    [Key]
    [Column("id_ubicacion")]
    public Guid IdUbicacion { get; set; }

    [Column("padre_id")]
    public Guid? PadreId { get; set; }

    [Column("tipo_nivel")]
    public string TipoNivel { get; set; } = string.Empty;

    [Column("codigo_ubigeo")]
    public string? CodigoUbigeo { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Column("activo")]
    public bool Activo { get; set; }

    [Column("responsable_modulo")]
    public string ResponsableModulo { get; set; } = "G2 Identidad";
}

[Table("direcciones")]
public class Direccion
{
    [Key]
    [Column("id_direccion")]
    public Guid IdDireccion { get; set; }

    [Column("cliente_id")]
    public Guid ClienteId { get; set; }

    [Column("ubicacion_id")]
    public Guid UbicacionId { get; set; }

    [Column("alias")]
    public string Alias { get; set; } = string.Empty;

    [Column("direccion_linea1")]
    public string DireccionLinea1 { get; set; } = string.Empty;

    [Column("referencia")]
    public string? Referencia { get; set; }

    [Column("codigo_postal")]
    public string? CodigoPostal { get; set; }

    [Column("es_principal")]
    public bool EsPrincipal { get; set; }

    [Column("responsable_modulo")]
    public string ResponsableModulo { get; set; } = "G2 Identidad";
}

[Table("medios_pago_guardados")]
public class MedioPagoGuardado
{
    [Key]
    [Column("id_referencia")]
    public Guid IdReferencia { get; set; }

    [Column("cliente_id")]
    public Guid ClienteId { get; set; }

    [Column("medio_pago_id")]
    public Guid MedioPagoId { get; set; }

    [Column("marca_tarjeta")]
    public string MarcaTarjeta { get; set; } = string.Empty;

    [Column("ultimos_cuatro_digitos")]
    public string UltimosCuatroDigitos { get; set; } = string.Empty;

    [Column("mes_expiracion")]
    public int MesExpiracion { get; set; }

    [Column("anio_expiracion")]
    public int AnioExpiracion { get; set; }

    [Column("token_pasarela")]
    public string TokenPasarela { get; set; } = string.Empty;

    [Column("es_predeterminado")]
    public bool EsPredeterminado { get; set; }

    [Column("responsable_modulo")]
    public string ResponsableModulo { get; set; } = "G2 Identidad";
}

// Referencia de solo lectura para enlazar FK medio_pago_id al registrar tarjetas
[Table("medios_pago")]
public class MedioPagoCatalogo
{
    [Key]
    [Column("id_medio_pago")]
    public Guid IdMedioPago { get; set; }

    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;
}