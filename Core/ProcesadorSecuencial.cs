using System.Diagnostics;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Coordinadores;

namespace SistemaConcurrente.Core
{
    
    // Que SE REPLICA del concurrente, para que la comparacion sea justa:
    //   - DOS SimularProcesamiento (proceos q simula el computo complejo) por orden: en el concurrente el productor computa antes de
    //     depositar y el consumidor vuelve a compiutar al retirar: aca tambien se hace dos
    //     veces, o la linea base haria la mitad del computo.
    //   - El registro de estados Generada -> EnProceso -> Finalizada, pero sobre un Dictionary
    //     comun
    //   - La misma instrumentacion de tiempos de Orden (CreadoEn / CompletadoEn / latencia).
    //
    // Que NO se replica, porque solo tiene sentido entre hilos simultaneos:
    //   - El buffer
    //   - Los hilos lectores
    public class ProcesadorSecuencial
    {
        private readonly int _cantOrdenes;
        private readonly int _cantIteraciones;

        // Estados de las ordenes en un diccionario pelado, sin sincronizacion: un solo hilo.
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
                // ---- lado "productor" ----
                Orden orden = new Orden(id, "Secuencial");
                orden.ValorCalculado = this.SimularProcesamiento(orden.Monto);
                _estados[orden.Id] = EstadoOrden.Generada;

                // ---- lado "consumidor" ----
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

        // Copia exacta del SimularProcesamiento de Productor/Consumidor
        private double SimularProcesamiento(double monto)
        {
            double x = monto;
            for (int i = 1; i <= _cantIteraciones; i++)
                x = Math.Sqrt(x * x + Math.Sin(i)) + Math.Cos(x);
            return x;
        }
    }
}
