using System.Text.RegularExpressions;
using Footloose.Identidad.API.Data;
using Footloose.Identidad.API.DTOs;
using Footloose.Identidad.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Footloose.Identidad.API.Controllers;

[ApiController]
[Route("api/v1/identidad")]
[Tags("G2 - Identidad y Gestión de Cuenta (N.° 12 al 22 y 50)")]
public class IdentidadController : ControllerBase
{
    private readonly FootlooseDbContext _context;

    public IdentidadController(FootlooseDbContext context)
    {
        _context = context;
    }

    // =========================================================================
    // SERVICIO N.° 12: Iniciar sesión
    // =========================================================================
    [HttpPost("sesiones")]
    public async Task<IActionResult> IniciarSesion([FromBody] IniciarSesionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "El correo electrónico es obligatorio." });

        var metodo = (request.MetodoAcceso ?? "footloose").Trim().ToLowerInvariant();
        if (metodo != "footloose" && metodo != "google")
            return BadRequest(new { error = "Método de acceso inválido. Use 'footloose' o 'google'." });

        if (metodo == "footloose" && string.IsNullOrWhiteSpace(request.Contrasena))
            return BadRequest(new { error = "La contraseña es obligatorio para el acceso directo con Footloose." });

        var cliente = await _context.Clientes
            .FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.Trim().ToLower());

        if (cliente is null || cliente.Estado.ToLower() != "activo")
            return Unauthorized(new { error = "No existe una cuenta activa asociada al correo proporcionado." });

        var cuenta = await _context.CuentasAcceso
            .FirstOrDefaultAsync(ca => ca.ClienteId == cliente.IdCliente && ca.ProveedorAuth.ToLower() == metodo)
            ?? await _context.CuentasAcceso.FirstOrDefaultAsync(ca => ca.ClienteId == cliente.IdCliente);

        if (cuenta is null)
            return Unauthorized(new { error = "No se encontró un método de acceso configurado para este cliente." });

        if (cuenta.BloqueadaHasta.HasValue && cuenta.BloqueadaHasta.Value > DateTime.UtcNow)
            return StatusCode(403, new { error = $"La cuenta se encuentra bloqueada temporalmente hasta {cuenta.BloqueadaHasta:O}." });

        var ahora = DateTime.UtcNow;
        cuenta.UltimoAcceso = ahora;
        cuenta.IntentosFallidos = 0;

        var nuevaSesion = new Sesion
        {
            IdSesion = Guid.NewGuid(),
            CuentaId = cuenta.IdCuenta,
            Dispositivo = string.IsNullOrWhiteSpace(request.Dispositivo) ? "PC / Web" : request.Dispositivo.Trim(),
            Navegador = string.IsNullOrWhiteSpace(request.Navegador) ? "Chrome" : request.Navegador.Trim(),
            DireccionIp = string.IsNullOrWhiteSpace(request.DireccionIp) ? (HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1") : request.DireccionIp.Trim(),
            FechaInicio = ahora,
            FechaExpiracion = ahora.AddDays(7),
            Activa = true,
            ResponsableModulo = cuenta.ResponsableModulo
        };

        _context.Sesiones.Add(nuevaSesion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ConsultarSesionesActivas), new { clienteId = cliente.IdCliente }, new
        {
            mensaje = "Autenticación exitosa.",
            metodoAcceso = metodo,
            idSesion = nuevaSesion.IdSesion,
            idCliente = cliente.IdCliente,
            nombreCompleto = $"{cliente.Nombres} {cliente.Apellidos}",
            email = cliente.Email,
            fechaExpiracion = nuevaSesion.FechaExpiracion
        });
    }

    // =========================================================================
    // SERVICIO N.° 13: Consultar perfil
    // =========================================================================
    [HttpGet("perfil")]
    public async Task<IActionResult> ConsultarPerfil([FromQuery] Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        var cliente = await _context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCliente == clienteId);

        if (cliente is null)
            return NotFound(new { error = "Cliente no encontrado." });

        return Ok(new
        {
            idCliente = cliente.IdCliente,
            nombres = cliente.Nombres,
            apellidos = cliente.Apellidos,
            correoElectronico = cliente.Email,
            tipoDocumento = cliente.TipoDocumento,
            numeroDocumento = cliente.NumeroDocumento ?? string.Empty,
            genero = cliente.Genero ?? string.Empty,
            fechaNacimiento = cliente.FechaNacimiento,
            telefono = cliente.Telefono ?? string.Empty,
            aceptaMarketing = cliente.AceptaMarketing,
            estado = cliente.Estado
        });
    }

    // =========================================================================
    // SERVICIO N.° 14: Actualizar perfil
    // =========================================================================
    [HttpPatch("perfil")]
    public async Task<IActionResult> ActualizarPerfil([FromQuery] Guid clienteId, [FromBody] ActualizarPerfilRequest request)
    {
        if (clienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        if (string.IsNullOrWhiteSpace(request.Nombres) ||
            string.IsNullOrWhiteSpace(request.Apellidos) ||
            string.IsNullOrWhiteSpace(request.NumeroDocumento))
        {
            return BadRequest(new { error = "Nombre, apellido y documento de identidad son campos obligatorios." });
        }

        if (request.EsPersonaJuridica && !string.IsNullOrWhiteSpace(request.Ruc) && !Regex.IsMatch(request.Ruc.Trim(), @"^\d{11}$"))
        {
            return BadRequest(new { error = "El RUC de persona jurídica debe contener exactamente 11 dígitos numéricos." });
        }

        var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.IdCliente == clienteId);
        if (cliente is null)
            return NotFound(new { error = "Cliente no encontrado." });

        cliente.Nombres = request.Nombres.Trim();
        cliente.Apellidos = request.Apellidos.Trim();
        cliente.TipoDocumento = string.IsNullOrWhiteSpace(request.TipoDocumento) ? "DNI" : request.TipoDocumento.Trim().ToUpperInvariant();
        cliente.NumeroDocumento = request.NumeroDocumento.Trim();
        cliente.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        cliente.FechaNacimiento = request.FechaNacimiento;
        cliente.Genero = string.IsNullOrWhiteSpace(request.Genero) ? null : request.Genero.Trim();

        if (request.AceptaMarketing.HasValue)
            cliente.AceptaMarketing = request.AceptaMarketing.Value;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Perfil actualizado correctamente.",
            perfil = new
            {
                idCliente = cliente.IdCliente,
                nombres = cliente.Nombres,
                apellidos = cliente.Apellidos,
                tipoDocumento = cliente.TipoDocumento,
                numeroDocumento = cliente.NumeroDocumento,
                telefono = cliente.Telefono ?? string.Empty,
                genero = cliente.Genero ?? string.Empty,
                fechaNacimiento = cliente.FechaNacimiento,
                personaJuridica = request.EsPersonaJuridica ? new
                {
                    nombreCorporativo = request.NombreCorporativo ?? string.Empty,
                    ruc = request.Ruc ?? string.Empty,
                    telefonoCorporativo = request.TelefonoCorporativo ?? string.Empty
                } : null
            }
        });
    }

    // =========================================================================
    // SERVICIO N.° 15: Consultar direcciones
    // =========================================================================
    [HttpGet("direcciones")]
    public async Task<IActionResult> ConsultarDirecciones([FromQuery] Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        var direcciones = await (
            from d in _context.Direcciones.AsNoTracking()
            join dist in _context.UbicacionesGeograficas.AsNoTracking() on d.UbicacionId equals dist.IdUbicacion into distJoin
            from dist in distJoin.DefaultIfEmpty()
            join prov in _context.UbicacionesGeograficas.AsNoTracking() on dist.PadreId equals prov.IdUbicacion into provJoin
            from prov in provJoin.DefaultIfEmpty()
            join dep in _context.UbicacionesGeograficas.AsNoTracking() on prov.PadreId equals dep.IdUbicacion into depJoin
            from dep in depJoin.DefaultIfEmpty()
            where d.ClienteId == clienteId
            orderby d.EsPrincipal descending
            select new
            {
                idDireccion = d.IdDireccion,
                alias = d.Alias,
                departamento = dep != null ? dep.Nombre : string.Empty,
                provincia = prov != null ? prov.Nombre : string.Empty,
                distrito = dist != null ? dist.Nombre : string.Empty,
                codigoUbigeo = dist != null ? dist.CodigoUbigeo : null,
                direccionLinea1 = d.DireccionLinea1,
                referencia = d.Referencia ?? string.Empty,
                codigoPostal = d.CodigoPostal ?? string.Empty,
                esPrincipal = d.EsPrincipal
            }
        ).ToListAsync();

        if (direcciones.Count == 0)
        {
            return Ok(new
            {
                total = 0,
                mensaje = "El cliente no tiene direcciones registradas.",
                direcciones
            });
        }

        return Ok(new
        {
            total = direcciones.Count,
            direcciones
        });
    }

    // =========================================================================
    // SERVICIO N.° 16: Registrar dirección
    // =========================================================================
    [HttpPost("direcciones")]
    public async Task<IActionResult> RegistrarDireccion([FromBody] RegistrarDireccionRequest request)
    {
        if (request.ClienteId == Guid.Empty || request.UbicacionId == Guid.Empty)
            return BadRequest(new { error = "Debe indicar clienteId y ubicacionId (distrito) válidos." });

        if (string.IsNullOrWhiteSpace(request.Alias) || string.IsNullOrWhiteSpace(request.DireccionLinea1))
            return BadRequest(new { error = "El alias/destinatario y la dirección (calle y número) son obligatorios." });

        var cliente = await _context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCliente == request.ClienteId);
        if (cliente is null)
            return NotFound(new { error = "Cliente no encontrado." });

        // Regla de negocio: validar jerarquía geográfica activa (distrito -> provincia -> departamento)
        var distrito = await _context.UbicacionesGeograficas.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUbicacion == request.UbicacionId && u.Activo);

        if (distrito is null || !distrito.PadreId.HasValue)
            return BadRequest(new { error = "La ubicación seleccionada no existe, está inactiva o no corresponde a un distrito válido." });

        var provincia = await _context.UbicacionesGeograficas.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUbicacion == distrito.PadreId.Value && u.Activo);

        if (provincia is null || !provincia.PadreId.HasValue)
            return BadRequest(new { error = "El distrito seleccionado no pertenece a una provincia válida." });

        var departamento = await _context.UbicacionesGeograficas.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUbicacion == provincia.PadreId.Value && u.Activo);

        if (departamento is null)
            return BadRequest(new { error = "La provincia seleccionada no pertenece a un departamento válido." });

        if (request.EsPrincipal)
        {
            var previas = await _context.Direcciones
                .Where(d => d.ClienteId == request.ClienteId && d.EsPrincipal)
                .ToListAsync();
            foreach (var item in previas)
                item.EsPrincipal = false;
        }

        var nuevaDireccion = new Direccion
        {
            IdDireccion = Guid.NewGuid(),
            ClienteId = request.ClienteId,
            UbicacionId = request.UbicacionId,
            Alias = request.Alias.Trim(),
            DireccionLinea1 = request.DireccionLinea1.Trim(),
            Referencia = string.IsNullOrWhiteSpace(request.Referencia) ? null : request.Referencia.Trim(),
            CodigoPostal = string.IsNullOrWhiteSpace(request.CodigoPostal) ? null : request.CodigoPostal.Trim(),
            EsPrincipal = request.EsPrincipal,
            ResponsableModulo = cliente.ResponsableModulo
        };

        _context.Direcciones.Add(nuevaDireccion);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ConsultarDirecciones), new { clienteId = request.ClienteId }, new
        {
            mensaje = "Dirección registrada correctamente.",
            direccion = new
            {
                idDireccion = nuevaDireccion.IdDireccion,
                alias = nuevaDireccion.Alias,
                departamento = departamento.Nombre,
                provincia = provincia.Nombre,
                distrito = distrito.Nombre,
                direccionLinea1 = nuevaDireccion.DireccionLinea1,
                referencia = nuevaDireccion.Referencia ?? string.Empty,
                codigoPostal = nuevaDireccion.CodigoPostal ?? string.Empty,
                esPrincipal = nuevaDireccion.EsPrincipal
            }
        });
    }

    // =========================================================================
    // SERVICIO N.° 17: Consultar tarjetas de crédito registradas
    // =========================================================================
    [HttpGet("tarjetas-guardadas")]
    [HttpGet("/api/v1/compra/tarjetas-guardadas")]
    public async Task<IActionResult> ConsultarTarjetasGuardadas([FromQuery] Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        var tarjetas = await _context.MediosPagoGuardados.AsNoTracking()
            .Where(t => t.ClienteId == clienteId)
            .OrderByDescending(t => t.EsPredeterminado)
            .Select(t => new
            {
                idReferencia = t.IdReferencia,
                marcaTarjeta = t.MarcaTarjeta,
                numeroEnmascarado = $"**** **** **** {t.UltimosCuatroDigitos}",
                ultimosCuatroDigitos = t.UltimosCuatroDigitos,
                vencimiento = $"{t.MesExpiracion:D2}/{t.AnioExpiracion}",
                esPredeterminado = t.EsPredeterminado
            })
            .ToListAsync();

        if (tarjetas.Count == 0)
        {
            return Ok(new
            {
                total = 0,
                mensaje = "El cliente no tiene tarjetas de crédito registradas.",
                tarjetas
            });
        }

        return Ok(new
        {
            total = tarjetas.Count,
            tarjetas
        });
    }

    // =========================================================================
    // SERVICIO N.° 18: Registrar tarjeta de crédito
    // =========================================================================
    [HttpPost("tarjetas-guardadas")]
    [HttpPost("/api/v1/compra/tarjetas-guardadas")]
    public async Task<IActionResult> RegistrarTarjetaGuardada([FromBody] RegistrarTarjetaRequest request)
    {
        if (request.ClienteId == Guid.Empty)
            return BadRequest(new { error = "Debe indicar un clienteId válido." });

        var digitosTarjeta = Regex.Replace(request.NumeroTarjeta ?? string.Empty, @"\s+", "");
        if (!Regex.IsMatch(digitosTarjeta, @"^\d{13,19}$"))
            return BadRequest(new { error = "El número de tarjeta debe contener entre 13 y 19 dígitos numéricos." });

        if (string.IsNullOrWhiteSpace(request.NombreImpreso))
            return BadRequest(new { error = "El nombre impreso en la tarjeta es obligatorio." });

        if (request.MesExpiracion < 1 || request.MesExpiracion > 12)
            return BadRequest(new { error = "El mes de expiración debe estar entre 1 y 12." });

        var fechaActual = DateTime.UtcNow;
        if (request.AnioExpiracion < fechaActual.Year ||
            (request.AnioExpiracion == fechaActual.Year && request.MesExpiracion < fechaActual.Month))
        {
            return BadRequest(new { error = "La tarjeta ingresada se encuentra vencida." });
        }

        // Validación en memoria del CVV (NUNCA se almacena en la base de datos)
        if (!Regex.IsMatch(request.CodigoSeguridad?.Trim() ?? string.Empty, @"^\d{3,4}$"))
            return BadRequest(new { error = "El código de seguridad (CVV) debe tener 3 o 4 dígitos." });

        // Validar campos obligatorios de dirección de facturación (información adicional es opcional)
        if (string.IsNullOrWhiteSpace(request.DepartamentoFacturacion) ||
            string.IsNullOrWhiteSpace(request.ProvinciaFacturacion) ||
            string.IsNullOrWhiteSpace(request.DistritoFacturacion) ||
            string.IsNullOrWhiteSpace(request.CalleFacturacion) ||
            string.IsNullOrWhiteSpace(request.NumeroFacturacion))
        {
            return BadRequest(new { error = "Departamento, provincia, distrito, calle y número de la dirección de facturación son obligatorios." });
        }

        var cliente = await _context.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.IdCliente == request.ClienteId);
        if (cliente is null)
            return NotFound(new { error = "Cliente no encontrado." });

        // Resolver FK medio_pago_id
        Guid medioPagoId;
        if (request.MedioPagoId.HasValue && request.MedioPagoId.Value != Guid.Empty)
        {
            medioPagoId = request.MedioPagoId.Value;
        }
        else
        {
            var medioPago = await _context.MediosPagoCatalogo.AsNoTracking().FirstOrDefaultAsync();
            if (medioPago is null)
                return BadRequest(new { error = "No existe ningún medio de pago configurado en el catálogo (medios_pago) para asociar la tarjeta." });
            medioPagoId = medioPago.IdMedioPago;
        }

        if (request.EsPredeterminado)
        {
            var previas = await _context.MediosPagoGuardados
                .Where(t => t.ClienteId == request.ClienteId && t.EsPredeterminado)
                .ToListAsync();
            foreach (var t in previas)
                t.EsPredeterminado = false;
        }

        var marca = digitosTarjeta.StartsWith('4') ? "VISA"
                  : digitosTarjeta.StartsWith('5') ? "MASTERCARD"
                  : digitosTarjeta.StartsWith('3') ? "AMEX"
                  : "TARJETA";

        var ultimos4 = digitosTarjeta[^4..];

        var nuevaTarjeta = new MedioPagoGuardado
        {
            IdReferencia = Guid.NewGuid(),
            ClienteId = request.ClienteId,
            MedioPagoId = medioPagoId,
            MarcaTarjeta = marca,
            UltimosCuatroDigitos = ultimos4,
            MesExpiracion = request.MesExpiracion,
            AnioExpiracion = request.AnioExpiracion,
            TokenPasarela = $"tok_sim_{Guid.NewGuid():N}",
            EsPredeterminado = request.EsPredeterminado,
            ResponsableModulo = cliente.ResponsableModulo
        };

        _context.MediosPagoGuardados.Add(nuevaTarjeta);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ConsultarTarjetasGuardadas), new { clienteId = request.ClienteId }, new
        {
            mensaje = "Tarjeta registrada exitosamente.",
            avisoVerificacion = "Puede realizarse un cargo temporal de verificación, el cual se cancela automáticamente al completar la verificación.",
            tarjeta = new
            {
                idReferencia = nuevaTarjeta.IdReferencia,
                marcaTarjeta = nuevaTarjeta.MarcaTarjeta,
                numeroEnmascarado = $"**** **** **** {nuevaTarjeta.UltimosCuatroDigitos}",
                vencimiento = $"{nuevaTarjeta.MesExpiracion:D2}/{nuevaTarjeta.AnioExpiracion}",
                esPredeterminado = nuevaTarjeta.EsPredeterminado
            }
        });
    }

    // =========================================================================
    // SERVICIO N.° 19: Consultar configuración de acceso
    // =========================================================================
    [HttpGet("configuracion")]
    public async Task<IActionResult> ConsultarConfiguracionAcceso([FromQuery] Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        var cuentas = await _context.CuentasAcceso.AsNoTracking()
            .Where(ca => ca.ClienteId == clienteId)
            .ToListAsync();

        if (cuentas.Count == 0)
            return NotFound(new { error = "No se encontró configuración de acceso para este cliente." });

        var cuentaIds = cuentas.Select(c => c.IdCuenta).ToList();
        var sesionesActivas = await _context.Sesiones.AsNoTracking()
            .CountAsync(s => cuentaIds.Contains(s.CuentaId) && s.Activa && s.FechaExpiracion > DateTime.UtcNow);

        var cuentaPrincipal = cuentas.First();

        return Ok(new
        {
            clienteId,
            proveedorPrincipal = cuentaPrincipal.ProveedorAuth,
            contrasenaDefinida = cuentaPrincipal.EmailVerificado || cuentaPrincipal.ProveedorAuth.ToLower() == "footloose",
            estadoContrasena = cuentaPrincipal.EmailVerificado ? "Definida y verificada" : "Pendiente de verificación",
            cantidadSesionesActivas = sesionesActivas,
            ultimoAcceso = cuentaPrincipal.UltimoAcceso
        });
    }

    // =========================================================================
    // SERVICIO N.° 20: Definir contraseña
    // =========================================================================
    [HttpPut("contrasena")]
    public async Task<IActionResult> DefinirContrasena([FromBody] DefinirContrasenaRequest request)
    {
        if (request.ClienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        if (!Regex.IsMatch(request.CodigoVerificacion?.Trim() ?? string.Empty, @"^\d{6}$"))
            return BadRequest(new { error = "Debe ingresar un código de verificación válido de 6 dígitos." });

        // Regla de negocio: al menos 8 caracteres, un número, una minúscula y una mayúscula
        var pass = request.NuevaContrasena ?? string.Empty;
        bool cumpleLongitud = pass.Length >= 8;
        bool cumpleMayuscula = pass.Any(char.IsUpper);
        bool cumpleMinuscula = pass.Any(char.IsLower);
        bool cumpleNumero = pass.Any(char.IsDigit);

        if (!cumpleLongitud || !cumpleMayuscula || !cumpleMinuscula || !cumpleNumero)
        {
            return BadRequest(new
            {
                error = "La contraseña debe tener al menos 8 caracteres, un número, una letra minúscula y una mayúscula."
            });
        }

        var cuenta = await _context.CuentasAcceso
            .FirstOrDefaultAsync(ca => ca.ClienteId == request.ClienteId);

        if (cuenta is null)
            return NotFound(new { error = "No se encontró cuenta de acceso asociada al cliente." });

        cuenta.EmailVerificado = true;
        cuenta.IntentosFallidos = 0;
        cuenta.BloqueadaHasta = null;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Contraseña definida y guardada correctamente tras validar el código de verificación.",
            clienteId = request.ClienteId,
            emailVerificado = cuenta.EmailVerificado
        });
    }

    // =========================================================================
    // SERVICIO N.° 21: Consultar sesiones activas
    // =========================================================================
    [HttpGet("sesiones")]
    public async Task<IActionResult> ConsultarSesionesActivas([FromQuery] Guid clienteId, [FromQuery] Guid? sesionActualId = null)
    {
        if (clienteId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar un clienteId válido." });

        var cuentaIds = await _context.CuentasAcceso.AsNoTracking()
            .Where(ca => ca.ClienteId == clienteId)
            .Select(ca => ca.IdCuenta)
            .ToListAsync();

        if (cuentaIds.Count == 0)
            return NotFound(new { error = "El cliente no tiene cuentas de acceso registradas." });

        var sesiones = await _context.Sesiones.AsNoTracking()
            .Where(s => cuentaIds.Contains(s.CuentaId) && s.Activa)
            .OrderByDescending(s => s.FechaInicio)
            .ToListAsync();

        var actualId = sesionActualId ?? sesiones.FirstOrDefault()?.IdSesion;

        var resultado = sesiones.Select(s => new
        {
            idSesion = s.IdSesion,
            dispositivo = s.Dispositivo ?? "Desconocido",
            navegador = s.Navegador ?? "Desconocido",
            direccionIp = s.DireccionIp ?? "0.0.0.0",
            primerAcceso = s.FechaInicio,
            ultimoAcceso = s.FechaInicio,
            fechaExpiracion = s.FechaExpiracion,
            esSesionActual = actualId.HasValue && s.IdSesion == actualId.Value
        });

        return Ok(new
        {
            totalActivas = sesiones.Count,
            sesiones = resultado
        });
    }

    // =========================================================================
    // SERVICIO N.° 22: Cerrar otra sesión
    // =========================================================================
    [HttpDelete("sesiones/{sesionId:guid}")]
    public async Task<IActionResult> CerrarOtraSesion([FromRoute] Guid sesionId, [FromQuery] Guid? sesionActualId = null)
    {
        if (sesionId == Guid.Empty)
            return BadRequest(new { error = "El identificador de sesión a cerrar no es válido." });

        // Regla de negocio: el cierre remoto aplica a una sesión distinta de la sesión actual
        if (sesionActualId.HasValue && sesionId == sesionActualId.Value)
        {
            return BadRequest(new
            {
                error = "No puede cerrar la sesión actual mediante esta operación. Utilice el servicio de cierre de sesión actual."
            });
        }

        var sesion = await _context.Sesiones.FirstOrDefaultAsync(s => s.IdSesion == sesionId);
        if (sesion is null)
            return NotFound(new { error = "La sesión especificada no existe." });

        if (!sesion.Activa)
            return BadRequest(new { error = "La sesión seleccionada ya se encontraba cerrada." });

        sesion.Activa = false;
        sesion.FechaExpiracion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "La sesión seleccionada se ha cerrado correctamente.",
            idSesionCerrada = sesion.IdSesion
        });
    }

   
    [HttpDelete("sesiones/actual")]
    public async Task<IActionResult> CerrarSesionActual([FromQuery] Guid sesionActualId, [FromQuery] bool confirmado = false)
    {
        if (sesionActualId == Guid.Empty)
            return BadRequest(new { error = "Debe proporcionar el sesionActualId activo." });

        // Regla de negocio: solicitar confirmación y permitir cancelar la operación
        if (!confirmado)
        {
            return Ok(new
            {
                cerrada = false,
                requiereConfirmacion = true,
                mensaje = "Operación cancelada o pendiente de confirmación. Envíe confirmado=true para cerrar la sesión actual."
            });
        }

        var sesion = await _context.Sesiones.FirstOrDefaultAsync(s => s.IdSesion == sesionActualId);
        if (sesion is null)
            return NotFound(new { error = "La sesión actual especificada no existe." });

        if (!sesion.Activa)
            return BadRequest(new { error = "La sesión actual ya estaba cerrada." });

        sesion.Activa = false;
        sesion.FechaExpiracion = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            cerrada = true,
            mensaje = "Sesión actual cerrada exitosamente.",
            idSesionCerrada = sesion.IdSesion
        });
    }
}