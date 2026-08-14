using System.Diagnostics;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Coordinadores;

namespace SistemaConcurrente.Core
{
    
    // Version secuencial usada como linea base. Hace el mismo computo que la concurrente
    // (dos SimularProcesamiento por orden) y registra los mismos estados y tiempos, pero sin
    // buffer ni hilos lectores, ya que eso solo tiene sentido entre hilos simultaneos.
    public class ProcesadorSecuencial
    {
        private readonly int _cantOrdenes;
        private readonly int _cantIteraciones;

        // Un solo hilo, asi que el diccionario de estados va sin sincronizacion
        private readonly Dictionary<int, EstadoOrden> _estados = new();
        private readonly List<Orden> _ordenesCompletadas = new();

        public ProcesadorSecuencial(int cantOrdenes, int cantIteraciones)
        {
            _cantOrdenes = cantOrdenes;
            _cantIteraciones = cantIteraciones;
        }

        public ResultadoEjecucion Ejecutar()
        {
            var sw = Stopwatch.StartNew();

            for (int id = 1; id <= _cantOrdenes; id++)
            {
                // lado productor
                Orden orden = new Orden(id, "Secuencial");
                orden.ValorCalculado = this.SimularProcesamiento(orden.Monto);
                _estados[orden.Id] = EstadoOrden.Generada;

                // lado consumidor
                _estados[orden.Id] = EstadoOrden.EnProceso;
                orden.ValorCalculado = this.SimularProcesamiento(orden.Monto);
                orden.CompletadoEn = DateTime.Now;
                orden.calcularLatencia();
                _estados[orden.Id] = EstadoOrden.Finalizada;

                _ordenesCompletadas.Add(orden);
            }

            sw.Stop();
            return new ResultadoEjecucion(_ordenesCompletadas, sw.Elapsed.TotalSeconds);
        }

        // Mismo SimularProcesamiento que usan Productor y Consumidor
        private double SimularProcesamiento(double monto)
        {
            double x = monto;
            for (int i = 1; i <= _cantIteraciones; i++)
                x = Math.Sqrt(x * x + Math.Sin(i)) + Math.Cos(x);
            return x;
        }
    }
}
