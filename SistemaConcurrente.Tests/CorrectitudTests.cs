using SistemaConcurrente.Core.Persistencia;
using SistemaConcurrente.Tests.Fixtures;
using Xunit;

namespace SistemaConcurrente.Tests
{
    
    // Un solo test por combinacion buffer x cache: corre la corrida, verifica
    // exactamente-una-vez en memoria, repite el flujo de persistencia del endpoint
    // (Limpiar + GuardarCorrida) y despues verifica CONTRA LA TABLA, no contra lo que el
    // codigo dice que guardo.
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

            // PARTE 1 - "se procesa exactamente una vez": las tres comprobaciones estan en
            // Corridas.VerificarExactamenteUnaVez (N completadas, Ids exactamente 1..N,
            // todas realmente procesadas).
            Corridas.VerificarExactamenteUnaVez(resultado, N);

            // PARTE 2 - "queda persistida": mismo flujo que el endpoint, Limpiar() antes y
            // GuardarCorrida() despues. Aca hay una comprobacion IMPLICITA: la PK de la
            // tabla es OrdenId sin identity (ValueGeneratedNever), asi que si la corrida
            // hubiera generado un Id duplicado, el insert REVIENTA y el test falla solo.
            // "Persistida exactamente una vez" lo controla la base, no el codigo.
            using (var db = _db.CrearContexto())
            {
                var persistencia = new PersistenciaOrdenes(db);
                persistencia.Limpiar();
                persistencia.GuardarCorrida(resultado.Ordenes, variante.Tecnica);
            }

            // PARTE 3 - verificacion contra la TABLA, con un contexto NUEVO (nada cacheado
            // en memoria: esto lee la base de verdad).
            using (var db = _db.CrearContexto())
            {
                // Traigo solo los Ids persistidos, ordenados.
                var idsPersistidos = db.Ordenes
                    .Select(o => o.OrdenId)
                    .OrderBy(id => id)
                    .ToList();

                // COMPROBACION FINAL: en la tabla quedaron EXACTAMENTE los Ids 1..N.
                // Igual que en memoria: si falta uno se perdio al persistir, si sobra uno
                // quedo basura de otra corrida (Limpiar() fallo), y la cuenta total tiene
                // que dar N. Las tres cosas en una sola igualdad.
                Assert.Equal(Enumerable.Range(1, N), idsPersistidos);
            }
        }
    }
}
