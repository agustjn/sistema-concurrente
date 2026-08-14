using SistemaConcurrente.Tests.Fixtures;
using Xunit;

namespace SistemaConcurrente.Tests
{
    // Test de estres para el indicador de ausencia de race conditions. Las configuraciones
    // buscan maxima contencion sobre la sincronizacion y no throughput: buffer de capacidad 1,
    // desbalance entre productores y consumidores, y volumen alto de ordenes con computo bajo.
    // Como las race conditions son probabilisticas, cada caso se repite varias veces con
    // buffer y cache nuevos.
    public class EstresTests
    {
        private const int Repeticiones = 2;

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Buffer_minimo_con_muchos_productores(VarianteRun variante)
        {
            const int N = 2000;

            // Cada repeticion es una corrida INDEPENDIENTE
            for (int rep = 0; rep < Repeticiones; rep++)
            {

                var resultado = await Corridas.Ejecutar(
                    variante, ordenes: N, productores: 8, consumidores: 2,
                    capacidadBuffer: 1, iteraciones: 10000);

                Corridas.VerificarExactamenteUnaVez(resultado, N);
            }
        }

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Buffer_minimo_con_muchos_consumidores(VarianteRun variante)
        {
            const int N = 2000;

            for (int rep = 0; rep < Repeticiones; rep++)
            {

                var resultado = await Corridas.Ejecutar(
                    variante, ordenes: N, productores: 2, consumidores: 8,
                    capacidadBuffer: 1, iteraciones: 10000);

                Corridas.VerificarExactamenteUnaVez(resultado, N);
            }
        }

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Volumen_alto_de_ordenes_con_iteraciones_bajas(VarianteRun variante)
        {
            const int N = 10000;

            for (int rep = 0; rep < Repeticiones; rep++)
            {
                // Volumen alto con computo casi nulo
                var resultado = await Corridas.Ejecutar(
                    variante, ordenes: N, productores: 6, consumidores: 6,
                    capacidadBuffer: 5, iteraciones: 10);

                Corridas.VerificarExactamenteUnaVez(resultado, N);
            }
        }
    }
}
