using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using System.Collections.Concurrent;

namespace SistemaConcurrente.Core
{
    public class Consumidor
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly IBuffer _buffer;
        private readonly ICache _cache;

        public int CantIteraciones { get; }

        // Bolsa compartida (misma instancia para todos los consumidores) donde se acumulan las
        // ordenes reales completadas, para luego calcular las metricas.
        private readonly ConcurrentBag<Orden> _ordenesCompletadas;


        public Consumidor(int id,string name, IBuffer buffer, int cantIteraciones, ConcurrentBag<Orden> ordenesCompletadas, ICache cache)
        {

            Id = id;
            Name = name;
            _buffer = buffer;
            CantIteraciones = cantIteraciones;
            _ordenesCompletadas = ordenesCompletadas;
            _cache = cache;
        }

        public void ProcesarOrden()
        {
            while (true)
            {
                Orden orden = _buffer.RetirarDato();
                if (orden.esFin) { break; }

                // ESCRITURA en cache: la orden pasa a "EnProceso" apenas la tomo del buffer.
                _cache.Escribir(orden.Id, EstadoOrden.EnProceso);

                orden.ValorCalculado = this.SimularProcesamiento(orden.Monto);
                orden.CompletadoEn = DateTime.Now;
                orden.calcularLatencia();

                _ordenesCompletadas.Add(orden);

                // ESCRITURA en cache: termine de procesar -> "Finalizada".
                _cache.Escribir(orden.Id, EstadoOrden.Finalizada);
                // Comentado a proposito: un print por orden serializa los hilos (la consola tiene su
                // propio lock interno) y arruina la medicion de performance.
                //Console.WriteLine("Orden #" + orden.Id + " completada / Consumidor #" + Id.ToString());
            }

        }

        private double SimularProcesamiento(double monto)
        {
            double x = monto;
            for (int i = 1; i <= CantIteraciones; i++)
                x = Math.Sqrt(x * x + Math.Sin(i)) + Math.Cos(x);
            return x;
        }

    }
}
