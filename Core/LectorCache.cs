using SistemaConcurrente.Core.Cache;

namespace SistemaConcurrente.Core
{
    // Hilo LECTOR de la cache. A diferencia de productores y consumidores (que ESCRIBEN el
    // estado de las ordenes), el lector solo CONSULTA: cada cierto intervalo pide un resumen
    // de la cache y lo muestra. Sirve para ejercitar el lado "lector" del problema
    // lectores/escritores mientras productores y consumidores escriben en paralelo.
    public class LectorCache
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly ICache _cache;

        // Cada cuanto (ms) el lector vuelve a consultar la cache.
        private readonly int _intervaloMs;

        public LectorCache(int id, string name, ICache cache, int intervaloMs)
        {
            Id = id;
            Name = name;
            _cache = cache;
            _intervaloMs = intervaloMs;
        }

        // Lee la cache periodicamente hasta que la corrida pide cancelar (cuando ya no quedan
        // ordenes por procesar). El token es la senial de "fin de corrida".
        public void LeerPeriodicamente(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ResumenCache resumen = _cache.LeerResumen();   // operacion de LECTOR
                Console.WriteLine(
                    $"[Lector #{Id}] {resumen.Generadas} generadas / {resumen.EnProceso} en proceso / " +
                    $"{resumen.Finalizadas} finalizadas (total {resumen.Total})");

                Thread.Sleep(_intervaloMs);   // espera entre lecturas: lectura "periodica"
            }
        }
    }
}
