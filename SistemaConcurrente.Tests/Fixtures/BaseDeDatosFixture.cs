using Microsoft.EntityFrameworkCore;
using SistemaConcurrente.Core.Persistencia;

namespace SistemaConcurrente.Tests.Fixtures
{
    // Misma instancia de SQL Server Express que usa la API pero contra una base separada,
    // ya que los tests hacen Limpiar() igual que los endpoints y pisarian la ultima corrida
    // guardada. Si la instancia no responde los tests fallan, no se saltean.
    public class BaseDeDatosFixture
    {
        public const string ConnectionString =
        //"Server=DESKTOP-4ATKEAP\\SQLEXPRESS;Database=SistemaConcurrenteTests;Trusted_Connection=True;TrustServerCertificate=True";
        "Server=SI-DESARROLLO45\\SQLEXPRESS;Database=SistemaConcurrenteTests;Trusted_Connection=True;TrustServerCertificate=True";

        // Las opciones se arman una vez; lo que no se comparte es el DbContext.
        private readonly DbContextOptions<OrdenesDbContext> _opciones;

        // xUnit crea el fixture una sola vez para toda la clase.
        public BaseDeDatosFixture()
        {
            _opciones = new DbContextOptionsBuilder<OrdenesDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            // Igual que Program.cs: crea la base y la tabla si no existen.
            using var db = CrearContexto();
            db.Database.EnsureCreated();
        }

        // Un contexto nuevo por uso, imitando el scoped de la API, y ademas asi se lee de la
        // base y no del change tracker.
        public OrdenesDbContext CrearContexto() => new OrdenesDbContext(_opciones);
    }
}
