using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SistemaConcurrente.Core.Coordinadores
{
    public class ConfigurationRunService
    {
        private int CantProductores;
        private int CantConsumidores;
        private int CantLectores;
        private int TamanioBuffer;
        private int CantIteraciones;
        // Intervalo (ms) entre lecturas de cada hilo lector de la cache.
        private const int IntervaloLecturaMs = 50;
        // Manera de manejar el indice/clave/id de cada productor/consumidor, ya que en el generarConsumidor del Thread(...) no se puede enviar el indice como parametro
        private readonly BufferSemaforos Buffer;
        // Cache compartida del estado de las ordenes (lectores/escritores, passing the baton, justa).
        private readonly ICache _cache;
        // Objeto utilizado para decrementar de manera atomica las ordenes, y luego asi poder cortar en los hilos productores
        private readonly ContadorOrdenes _contadorOrdenes;
        // Bolsa compartida donde los consumidores acumulan las ordenes reales completadas, para calcular metricas
        private readonly ConcurrentBag<Orden> _ordenesCompletadas = new();


        public ConfigurationRunService(int cantProductores, int cantConsumidores, int tamanioBuffer, int cantIteraciones, int totalOrdenes, int cantLectores)
        {
            CantConsumidores = cantConsumidores;
            CantProductores = cantProductores;
            CantLectores = cantLectores;
            TamanioBuffer = tamanioBuffer;
            CantIteraciones = cantIteraciones;
            Buffer = new BufferSemaforos(tamanioBuffer);
            _cache = new CacheSemaforosJusta();
            _contadorOrdenes = new ContadorOrdenes(totalOrdenes);
        }

        public async Task<ResultadoEjecucion> EjecutarSemaforos()
        {
            var sw = Stopwatch.StartNew();

            // Senial para frenar a los hilos lectores cuando la corrida termina.
            using var ctsLectores = new CancellationTokenSource();

            await Task.Run(() =>
            {
                var hilosProductores = this.GenerarProductores();
                var hilosConsumidores = this.GenerarConsumidores();
                // Los lectores corren en paralelo consultando la cache mientras se produce/consume.
                var hilosLectores = this.GenerarLectores(ctsLectores.Token);

                // Espero a los hilos productores inicialmente, y cuando finalizen ya tengo garantizada toda la produccion de las N ordenes
                foreach (var h in hilosProductores) h.Join();

                // En este paso hay una parte que a mi parecer es interesante: se depositan las ordenes "veneno", las cuales sirven para que posteriormente los hilos consumidores
                // tomen esa "orden veneno" y puedan cortar la ejecucion del while(true) a traves de tomar una orden, ya que sino se quedarian en el semaforo de _lleno.Wait()
                // dormidos, y se necesita que tomen una "orden ficticia" para cortar el bucle
                for (int c = 0; c < CantConsumidores; c++)
                    Buffer.DepositarDato(Orden.PoisonPill());

                // Espero que los hilos consumidores finalizen su ejecucion, el fin va a ser cuando cada hilo consuma la "orden veneno"
                foreach (var h in hilosConsumidores) h.Join();

                // Ya no hay mas escrituras: corto los lectores y espero a que terminen su ciclo actual.
                ctsLectores.Cancel();
                foreach (var h in hilosLectores) h.Join();
            });

            sw.Stop();

            // Devuelvo las ordenes completadas y el tiempo total medido.
            return new ResultadoEjecucion(_ordenesCompletadas.ToList(), sw.Elapsed.TotalSeconds);
        }

        public List<Thread> GenerarProductores()
        {
            var hilos = new List<Thread>();
            foreach (var i in Enumerable.Range(0, CantProductores))
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    Productor productor = new Productor(i, "Prod #" + i.ToString(), Buffer, CantIteraciones, _contadorOrdenes, _cache);
                    Console.Write(productor.ToString());
                    productor.GenerarOrdenes();
                });

                nuevoHilo.Start();
                hilos.Add(item: nuevoHilo);
            }

            return hilos;
        }

        public List<Thread> GenerarConsumidores()
        {
            var hilos = new List<Thread>();

            foreach (var i in Enumerable.Range(0, CantConsumidores))
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    Consumidor consumidor = new Consumidor(i, "Consumidor #" + i.ToString(), Buffer, CantIteraciones, _ordenesCompletadas, _cache);
                    Console.Write(consumidor.ToString());
                    consumidor.ProcesarOrden();
                });

                nuevoHilo.Start();
                hilos.Add(item: nuevoHilo);
            }

            return hilos;
        }

        // Lanza los hilos lectores de la cache. Cada uno consulta el resumen periodicamente
        // hasta que el token se cancela (al terminar la corrida).
        public List<Thread> GenerarLectores(CancellationToken token)
        {
            var hilos = new List<Thread>();

            foreach (var i in Enumerable.Range(0, CantLectores))
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    LectorCache lector = new LectorCache(i, "Lector #" + i.ToString(), _cache, IntervaloLecturaMs);
                    lector.LeerPeriodicamente(token);
                });

                nuevoHilo.Start();
                hilos.Add(item: nuevoHilo);
            }

            return hilos;
        }




    }
}
