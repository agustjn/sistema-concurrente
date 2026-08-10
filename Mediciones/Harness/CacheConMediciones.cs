using System.Collections.Concurrent;
using System.Diagnostics;
using SistemaConcurrente.Core.Cache;

namespace SistemaConcurrente.Mediciones
{
    // Decorador de ICache para el experimento de la secc. 7 (proporcion lecturas/escrituras).
    //
    // Envuelve cualquiera de las 4 caches reales y mide cuanto tarda cada operacion COMPLETA
    // (espera por sincronizacion + la operacion en si). Como la operacion en si es minima
    // (asignar en un Dictionary / recorrerlo), la duracion medida es, en la practica, la
    // ESPERA que la politica de la cache le impone a ese hilo. Esa espera es justamente el
    // dato que muestra (o descarta) la inanicion de escritores.
    //
    // Es un decorador a proposito: no toca las implementaciones que se estan evaluando,
    // asi la medicion no altera los protocolos de sincronizacion.
    public class CacheConMediciones : ICache
    {
        private readonly ICache _interna;

        // Bolsas thread-safe, una duracion por operacion. Se llenan desde muchos hilos.
        public ConcurrentBag<double> DuracionEscriturasMs { get; } = new();
        public ConcurrentBag<double> DuracionLecturasMs { get; } = new();

        public CacheConMediciones(ICache interna)
        {
            _interna = interna;
        }

        public void Escribir(int ordenId, EstadoOrden estado)
        {
            long inicio = Stopwatch.GetTimestamp();
            _interna.Escribir(ordenId, estado);
            DuracionEscriturasMs.Add(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
        }

        public EstadoOrden? Leer(int ordenId)
        {
            long inicio = Stopwatch.GetTimestamp();
            var resultado = _interna.Leer(ordenId);
            DuracionLecturasMs.Add(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
            return resultado;
        }

        public ResumenCache LeerResumen()
        {
            long inicio = Stopwatch.GetTimestamp();
            var resultado = _interna.LeerResumen();
            DuracionLecturasMs.Add(Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
            return resultado;
        }
    }
}
