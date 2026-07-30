using SistemaConcurrente.Tests.Fixtures;
using Xunit;

namespace SistemaConcurrente.Tests
{
    // Indicador "sin race conditions" de la propuesta (secc. 6): prueba de estres con
    // muchos productores y consumidores concurrentes, sin perdidas ni duplicados.
    //
    // Las configs estan elegidas para MAXIMIZAR LA CONTENCION sobre la sincronizacion
    // (que es donde las races se destapan), no el throughput:
    //
    //  - buffer = 1: cada deposito y cada retiro pelean por el UNICO lugar del arreglo;
    //    los hilos se demoran y despiertan todo el tiempo. Es el peor caso de las
    //    condiciones de sincronizacion: el buffer esta siempre lleno o siempre vacio.
    //  - desbalance P >> C y C >> P: una punta del buffer permanentemente saturada,
    //    con varios hilos del mismo rol compitiendo entre si por la misma condicion
    //    (el caso del while vs if en los monitores, y del contador en los semaforos).
    //  - iteraciones bajas: casi todo el tiempo de cada hilo se va en sincronizar y no
    //    en computar -> mas interleavings distintos por segundo, mas chances de pisar
    //    una ventana mal protegida.
    //
    // Como las races son PROBABILISTICAS (una corrida limpia no prueba nada), cada caso
    // se repite varias veces con buffer y cache nuevos en cada vuelta.
    //
    // Categoria=Estres para poder excluir los tests pesados del ciclo rapido:
    //   dotnet test --filter "Categoria!=Estres"
    [Trait("Categoria", "Estres")]
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
                // 8 productores contra 2 consumidores y un solo lugar de buffer: los
                // productores viven demorados en el buffer LLENO, empujandose entre si.
                var resultado = await Corridas.Ejecutar(
                    variante, ordenes: N, productores: 8, consumidores: 2,
                    capacidadBuffer: 1, iteraciones: 10000);

                // La UNICA comprobacion que importa bajo estres: ni perdidas ni
                // duplicados (N completadas, Ids exactamente 1..N, todas procesadas).
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
                // El espejo del caso anterior: 8 consumidores viven demorados en el buffer
                // VACIO, y cada orden que entra dispara la carrera de todos a la vez por
                // retirarla (aca es donde un despertar mal hecho retira dos veces la misma).
                var resultado = await Corridas.Ejecutar(
                    variante, ordenes: N, productores: 2, consumidores: 8,
                    capacidadBuffer: 1, iteraciones: 10000);

                Corridas.VerificarExactamenteUnaVez(resultado, N);
            }
        }

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Volumen_alto_con_iteraciones_bajas(VarianteRun variante)
        {
            const int N = 10000;

            for (int rep = 0; rep < Repeticiones; rep++)
            {
                // Volumen alto con computo casi nulo: 10.000 ordenes cruzando el buffer lo
                // mas rapido posible. Maximiza la CANTIDAD de pasadas por las barreras de
                // sincronizacion, que es lo que le da chances a una race de manifestarse.
                var resultado = await Corridas.Ejecutar(
                    variante, ordenes: N, productores: 6, consumidores: 6,
                    capacidadBuffer: 5, iteraciones: 10000);

                Corridas.VerificarExactamenteUnaVez(resultado, N);
            }
        }
    }
}
