using System.Diagnostics;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Coordinadores;

namespace SistemaConcurrente.Core
{
    // LINEA BASE SECUENCIAL de la propuesta (secc. 7): el mismo trabajo funcional que el
    // pipeline concurrente, pero en UN solo hilo y SIN ningun costo de coordinacion (sin
    // buffer, sin semaforos/monitores, sin ConcurrentBag). Es la referencia contra la que se
    // comparan las variantes concurrentes con 1.000/5.000/10.000 ordenes, para concluir a
    // partir de que punto la concurrencia se justifica.
    //
    // Que SE REPLICA del concurrente, para que la comparacion sea justa:
    //   - DOS SimularProcesamiento por orden: en el concurrente el productor simula antes de
    //     depositar y el consumidor vuelve a simular al retirar -> aca tambien se hace dos
    //     veces, o la linea base haria la mitad del computo.
    //   - El registro de estados Generada -> EnProceso -> Finalizada, pero sobre un Dictionary
    //     comun: registrar el estado es trabajo funcional, sincronizarlo no (no hay nadie mas).
    //     Usar una ICache con locks aca mediria costo de locks sin contencion, que no es ni
    //     secuencial puro ni concurrente real.
    //   - La misma instrumentacion de tiempos de Orden (CreadoEn / CompletadoEn / latencia).
    //
    // Que NO se replica, porque solo tiene sentido entre hilos simultaneos:
    //   - El buffer: no hay dos ritmos que amortiguar ni nadie a quien frenar. Por eso
    //     InsertadoEnBufferEn y RetiradoDeBufferEn quedan null y la metrica "espera en buffer"
    //     no aplica (null, no cero).
    //   - Los hilos lectores: son parte del experimento de la cache, no de la linea base.
    //     (Por lo mismo, las corridas concurrentes que se comparen contra esta conviene
    //     hacerlas con CantLectores = 0.)
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
