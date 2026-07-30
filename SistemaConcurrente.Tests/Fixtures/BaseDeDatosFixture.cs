using Microsoft.EntityFrameworkCore;
using SistemaConcurrente.Core.Persistencia;

namespace SistemaConcurrente.Tests.Fixtures
{
    // Conexion a la MISMA instancia SQL Server Express que usa la API, pero contra una base
    // SEPARADA (SistemaConcurrenteTests). Por que separada: los tests hacen Limpiar() igual
    // que los endpoints, y como la base real guarda UNA SOLA corrida (el esquema sin runId),
    // correr los tests contra ella te pisaria la ultima corrida guardada.
    //
    // Los tests ASUMEN el Express local corriendo, sin skip (decision 2026-07-30): si la
    // instancia no responde, fallan, que es el comportamiento buscado. Si la instancia
    // cambia de nombre, ajustar aca (es la misma cadena que appsettings.json, solo cambia
    // el Database).
    public class BaseDeDatosFixture
    {
        public const string ConnectionString =
            //"Server=DESKTOP-4ATKEAP\\SQLEXPRESS;Database=SistemaConcurrenteTests;Trusted_Connection=True;TrustServerCertificate=True";
            "Server=SI-DESARROLLO45\\SQLEXPRESS;Database=SistemaConcurrenteTests;Trusted_Connection=True;TrustServerCertificate=True";

        // Las opciones se arman UNA vez y se comparten: lo que no se comparte es el
        // DbContext (ver CrearContexto).
        private readonly DbContextOptions<OrdenesDbContext> _opciones;

        // xUnit crea este fixture UNA sola vez para toda la clase de tests (IClassFixture):
        // la creacion de la base es costosa y no hace falta repetirla por test.
        public BaseDeDatosFixture()
        {
            _opciones = new DbContextOptionsBuilder<OrdenesDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            // Igual que Program.cs: crea la base y la tabla Orden si no existen
            // (EnsureCreated, sin el aparato de migrations).
            using var db = CrearContexto();
            db.Database.EnsureCreated();
        }

        // Un contexto NUEVO por cada uso, imitando el ciclo de vida scoped de la API
        // (una instancia por request = una por corrida). Ademas, verificar con un contexto
        // nuevo garantiza que se lee de la BASE y no del change tracker que ya tenia las
        // entidades cacheadas en memoria.
        public OrdenesDbContext CrearContexto() => new OrdenesDbContext(_opciones);
    }
}
