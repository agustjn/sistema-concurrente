using SistemaConcurrente.Tests.Fixtures;
using Xunit;

namespace SistemaConcurrente.Tests
{
    
    // Un deadlock no tira excepcion: los hilos quedan dormidos para siempre y la corrida
    // nunca termina. Por eso el detector es un timeout: si Ejecutar() no volvio dentro del
    // plazo, se asume deadlock y el test falla. El plazo es amplio para no dar falsos
    // positivos en una maquina lenta.
    // La config exige la sincronizacion al maximo: volumen alto, buffer chico y varios
    // hilos por rol.

    public class DeadlockTests
    {

        private const int TimeoutMs = 120_000;

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Corrida_larga_termina_sin_trabarse(VarianteRun variante)
        {
            const int N = 100000;

            // No se espera la corrida directamente: se lanza y se la hace competir contra el reloj.
            var corrida = Corridas.Ejecutar(
                variante, ordenes: N, productores: 8, consumidores: 8,
                capacidadBuffer: 2, iteraciones: 20, 8, 5);

            // WhenAny devuelve la primera de las dos que termine.
            var ganador = await Task.WhenAny(corrida, Task.Delay(TimeoutMs));

            // Si gano el reloj, los hilos siguen dormidos en algun Wait -> deadlock
            Assert.True(ganador == corrida,
                $"Posible DEADLOCK en {variante.Tecnica}: la corrida de {N} ordenes " +
                $"no termino en {TimeoutMs / 1000} segundos.");

            // No alcanza con terminar, hay que terminar BIEN: se chequea la correctitud de los datos

            Corridas.VerificarExactamenteUnaVez(await corrida, N);
        }
    }
}
