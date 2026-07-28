using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Cache.CacheSemaforos;
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
        private int _intervaloEnMsDeLecturas;
        // Manera de manejar el indice/clave/id de cada productor/consumidor, ya que en el generarConsumidor del Thread(...) no se puede enviar el indice como parametro
        private readonly IBuffer _buffer;
        // Cache compartida del estado de las ordenes (lectores/escritores, passing the baton, justa).
        private readonly ICache _cache;
        // Objeto utilizado para decrementar de manera atomica las ordenes, y luego asi poder cortar en los hilos productores
        private readonly ContadorOrdenes _contadorOrdenes;
        // Objeto utilizado para repartir Id unicos entre todos los productores de la corrida
        private readonly GeneradorIdsOrden _generadorIds;
        // Bolsa compartida donde los consumidores acumulan las ordenes reales completadas, para calcular metricas
        private readonly ConcurrentBag<Orden> _ordenesCompletadas = new();
        



        public ConfigurationRunService(int cantProductores, int cantConsumidores, int cantIteraciones, int totalOrdenes, int cantLectores, ICache cache, IBuffer buffer, int intervaloMsDeLecturas)
        {
            CantConsumidores = cantConsumidores;
            CantProductores = cantProductores;
            CantLectores = cantLectores;
            CantIteraciones = cantIteraciones;
            _buffer = buffer;
            _cache = cache;
            _contadorOrdenes = new ContadorOrdenes(totalOrdenes);
            _generadorIds = new GeneradorIdsOrden();
            _intervaloEnMsDeLecturas = intervaloMsDeLecturas;
        }

        public async Task<ResultadoEjecucion> Ejecutar()
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
                    _buffer.DepositarDato(Orden.PoisonPill());

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

        // Lanza los hilos productores, los cuales van a generar las ordenes, almacenandolas en el buffer y actualizando la cache, para que posteriormente los consumidores puedan trabajar con ellas.
        public List<Thread> GenerarProductores()
        {

            var hilos = new List<Thread>();
            foreach (var i in Enumerable.Range(0, CantProductores))
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    Productor productor = new Productor(i, "Prod #" + i.ToString(), _buffer, CantIteraciones, _contadorOrdenes, _cache, _generadorIds);
                    //Console.Write(productor.ToString());
                    productor.GenerarOrdenes();
                });

                nuevoHilo.Start();
                hilos.Add(item: nuevoHilo);
            }

            return hilos;
        }

        // Lanza los hilos consumidores, los cuales van a trabajar con las ordenes tanto en el buffer como en la cache
        public List<Thread> GenerarConsumidores()
        {
            var hilos = new List<Thread>();

            foreach (var i in Enumerable.Range(0, CantConsumidores))
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    Consumidor consumidor = new Consumidor(i, "Consumidor #" + i.ToString(), _buffer, CantIteraciones, _ordenesCompletadas, _cache);
                    //Console.Write(consumidor.ToString());
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
                    LectorCache lector = new LectorCache(i, "Lector #" + i.ToString(), _cache, _intervaloEnMsDeLecturas);
                    lector.LeerPeriodicamente(token);
                });

                nuevoHilo.Start();
                hilos.Add(item: nuevoHilo);
            }

            return hilos;
        }




    }
}
