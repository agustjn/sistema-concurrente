using Microsoft.EntityFrameworkCore;

namespace SistemaConcurrente.Core.Persistencia
{
    public class OrdenesDbContext : DbContext
    {
        public OrdenesDbContext(DbContextOptions<OrdenesDbContext> options) : base(options) { }

        public DbSet<OrdenPersistencia> Ordenes => Set<OrdenPersistencia>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var orden = modelBuilder.Entity<OrdenPersistencia>();
            orden.ToTable("Orden");

            // PK = el id que genera GeneradorIdsOrden. ValueGeneratedNever le avisa a EF que el id
            // viene de la aplicacion, asi un duplicado lo rechaza la PK.
            orden.HasKey(o => o.OrdenId);
            orden.Property(o => o.OrdenId).ValueGeneratedNever();

            orden.Property(o => o.Tecnica).HasMaxLength(50);
        }
    }
}
