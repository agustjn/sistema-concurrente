using SistemaConcurrente.Tests.Fixtures;
using Xunit;

namespace SistemaConcurrente.Tests
{
    
    // Un deadlock no tira excepcion: los hilos quedan dormidos para siempre (en un Wait
    // de semaforo o de monitor del que nadie los va a despertar) y la corrida simplemente
    // NO TERMINA. Por eso el detector es un TIMEOUT: si Ejecutar() no volvio dentro del
    // plazo, se asume deadlock y el test falla
    //
    // El plazo es generoso para no dar falsos positivos en una maquina
    // lenta: una corrida correcta de esta config termina en una fraccion de ese tiempo
    //
    // La config estresa justo lo que un deadlock necesita para aparecer:
    //  - volumen alto -> muchisimos ciclos de demorarse/despertar 
    //  - buffer chico -> bloqueos constantes en las DOS puntas a la vez
    //  - varios hilos por rol -> seniales que pueden tocarle al hilo equivocado 

    public class DeadlockTests
    {

        private const int TimeoutMs = 120_000;

        [Theory]
        [MemberData(nameof(Variantes.Todas), MemberType = typeof(Variantes))]
        public async Task Corrida_larga_termina_sin_trabarse(VarianteRun variante)
        {
            const int N = 100000;

            // aca NO se espera la corrida directamente. Se lanza y se guarda la
            // Task para hacerla competir contra el reloj
            var corrida = Corridas.Ejecutar(
                variante, ordenes: N, productores: 8, consumidores: 8,
                capacidadBuffer: 2, iteraciones: 20, 8, 5);

            // La carrera: la corrida contra un timer de TimeoutMs. WhenAny devuelve la
            // primera de las dos que termine.
            var ganador = await Task.WhenAny(corrida, Task.Delay(TimeoutMs));

            // COMPROBACION 1: gano la corrida, no el reloj. Si gano el reloj, los hilos
            // siguen dormidos en algun Wait -> deadlock 
            Assert.True(ganador == corrida,
                $"Posible DEADLOCK en {variante.Tecnica}: la corrida de {N} ordenes " +
                $"no termino en {TimeoutMs / 1000} segundos.");

            // COMPROBACION 2: no alcanza con TERMINAR, hay que terminar BIEN. Se chequea la correctitud de los datos 

            Corridas.VerificarExactamenteUnaVez(await corrida, N);
        }
    }
}
