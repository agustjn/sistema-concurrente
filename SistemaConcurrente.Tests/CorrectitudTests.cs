using SistemaConcurrente.Core.Persistencia;
using SistemaConcurrente.Tests.Fixtures;
using Xunit;

namespace SistemaConcurrente.Tests
{
    
    // Un test por combinacion de buffer y cache: corre la corrida, verifica en memoria,
    // persiste igual que el endpoint y despues verifica contra la tabla.
    public class CorrectitudTests : IClassFixture<BaseDeDatosFixture>
    {
        private readonly BaseDeDatosFixture _db;

        // xUnit inyecta el fixture (compartido entre los 4 casos del Theory).
        public CorrectitudTests(BaseDeDatosFixture db)
        {
            _db = db;
        }

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Procesamiento_Unico_Y_Persistencia(VarianteRun variante)
        {
            // Config chica y balanceada
            const int N = 500; 

            var resultado = await Corridas.Ejecutar(
                variante, ordenes: N, productores: 3, consumidores: 3,
                capacidadBuffer: 10, iteraciones: 100, 10, 10);

            // Verificacion en memoria: N completadas, Ids 1..N y todas realmente procesadas.
            Corridas.VerificarExactamenteUnaVez(resultado, N);

            // Persistencia: mismo flujo que el endpoint, Limpiar() antes y GuardarCorrida() despues.
            // Como la PK es OrdenId sin identity, si hubiera un Id duplicado el insert falla y el
            // test se cae solo.
            using (var db = _db.CrearContexto())
            {
                var persistencia = new PersistenciaOrdenes(db);
                persistencia.Limpiar();
                persistencia.GuardarCorrida(resultado.Ordenes, variante.Tecnica);
            }

            // Verificacion contra la tabla, con un contexto nuevo para leer de la base.
            using (var db = _db.CrearContexto())
            {
                // Traigo solo los Ids persistidos, ordenados.
                var idsPersistidos = db.Ordenes
                    .Select(o => o.OrdenId)
                    .OrderBy(id => id)
                    .ToList();

                // En la tabla tienen que quedar exactamente los Ids 1..N.
                Assert.Equal(Enumerable.Range(1, N), idsPersistidos);
            }
        }
    }
}
