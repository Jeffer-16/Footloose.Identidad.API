using Footloose.Identidad.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Footloose.Identidad.API.Data;

public class FootlooseDbContext : DbContext
{
    public FootlooseDbContext(DbContextOptions<FootlooseDbContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CuentaAcceso> CuentasAcceso => Set<CuentaAcceso>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<UbicacionGeografica> UbicacionesGeograficas => Set<UbicacionGeografica>();
    public DbSet<Direccion> Direcciones => Set<Direccion>();
    public DbSet<MedioPagoGuardado> MediosPagoGuardados => Set<MedioPagoGuardado>();
    public DbSet<MedioPagoCatalogo> MediosPagoCatalogo => Set<MedioPagoCatalogo>();
}