using SistemaConcurrente.Core.Cache;

namespace SistemaConcurrente.Core
{
    // Hilo lector de la cache. A diferencia de productores y consumidores, que escriben el
    // estado de las ordenes, el lector solo consulta el resumen cada cierto intervalo.
    public class LectorCache
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly ICache _cache;

        // Cada cuanto (ms) el lector vuelve a consultar la cache.
        private readonly int _intervaloMs;

        public LectorCache(int id, string name, ICache cache, int intervaloMsDeLecturas)
        {
            Id = id;
            Name = name;
            _cache = cache;
            _intervaloMs = intervaloMsDeLecturas;
        }

        // Lee la cache periodicamente hasta que la corrida pide cancelar.
        public void LeerPeriodicamente(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ResumenCache resumen = _cache.LeerResumen();   

                Thread.Sleep(_intervaloMs);   // espera entre lecturas (parametrizado para poder jugar con la cantidad de lecturas)
            }
        }
    }
}
