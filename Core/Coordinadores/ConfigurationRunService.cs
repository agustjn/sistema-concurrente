using SistemaConcurrente.Core.Buffer;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SistemaConcurrente.Core.Coordinadores
{
    public class ConfigurationRunService
    {
        private int CantProductores;
        private int CantConsumidores;
        private int TamanioBuffer;
        private int CantIteraciones;
        // Manera de manejar el indice/clave/id de cada productor/consumidor, ya que en el generarConsumidor del Thread(...) no se puede enviar el indice como parametro
        private readonly BufferSemaforos Buffer;
        // Objeto utilizado para decrementar de manera atomica las ordenes, y luego asi poder cortar en los hilos productores
        private readonly ContadorOrdenes _contadorOrdenes;
        // Bolsa compartida donde los consumidores acumulan las ordenes reales completadas, para calcular metricas
        private readonly ConcurrentBag<Orden> _ordenesCompletadas = new();


        public ConfigurationRunService(int cantProductores, int cantConsumidores, int tamanioBuffer, int cantIteraciones, int totalOrdenes)
        {
            CantConsumidores = cantConsumidores;
            CantProductores = cantProductores;
            TamanioBuffer = tamanioBuffer;
            CantIteraciones = cantIteraciones;
            Buffer = new BufferSemaforos(tamanioBuffer);
            _contadorOrdenes = new ContadorOrdenes(totalOrdenes);
        }

        public async Task<ResultadoEjecucion> EjecutarSemaforos()
        {
            var sw = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                var hilosProductores = this.GenerarProductores();
                var hilosConsumidores = this.GenerarConsumidores();

                // Espero a los hilos productores inicialmente, y cuando finalizen ya tengo garantizada toda la produccion de las N ordenes
                foreach (var h in hilosProductores) h.Join();

                // En este paso hay una parte que a mi parecer es interesante: se depositan las ordenes "veneno", las cuales sirven para que posteriormente los hilos consumidores
                // tomen esa "orden veneno" y puedan cortar la ejecucion del while(true) a traves de tomar una orden, ya que sino se quedarian en el semaforo de _lleno.Wait()
                // dormidos, y se necesita que tomen una "orden ficticia" para cortar el bucle
                for (int c = 0; c < CantConsumidores; c++)
                    Buffer.DepositarDato(Orden.PoisonPill());

                // Espero que los hilos consumidores finalizen su ejecucion, el fin va a ser cuando cada hilo consuma la "orden veneno"
                foreach (var h in hilosConsumidores) h.Join();


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
                    Productor productor = new Productor(i, "Prod #" + i.ToString(), Buffer, CantIteraciones, _contadorOrdenes);
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
                    Consumidor consumidor = new Consumidor(i, "Consumidor #" + i.ToString(), Buffer, CantIteraciones, _ordenesCompletadas);
                    Console.Write(consumidor.ToString());
                    consumidor.ProcesarOrden();
                });

                nuevoHilo.Start();
                hilos.Add(item: nuevoHilo);
            }

            return hilos;
        }




    }
}
