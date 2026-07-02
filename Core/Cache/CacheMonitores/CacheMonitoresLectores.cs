namespace SistemaConcurrente.Core.Cache.CacheMonitores
{
    public class CacheMonitores : ICache
    {
        private readonly Dictionary<int, EstadoOrden> _estados = new();

        private readonly object _monitor = new object();
        // recursos protegidos por el monitor
        private int escritoresActivos = 0;
        private int lectoresActivos = 0;
        private int escritoresDormidos = 0;
        private int lectoresDormidos = 0;

        public void Escribir(int ordenId, EstadoOrden estado)
        {
            lock (_monitor)
            {
                while (escritoresActivos > 0 || lectoresActivos > 0)
                    Monitor.Wait(_monitor);
                _estados[ordenId] = estado;
                Monitor.PulseAll(_monitor);
            }
        }

        public EstadoOrden? Leer(int ordenId)
        {
            lock (_monitor)
            {
                while (escritoresActivos > 0)
                    Monitor.Wait(_monitor);
                var estado = _estados[ordenId];
                Monitor.PulseAll(_monitor);
                return estado;
            }
        }

        public ResumenCache LeerResumen()
        {
            throw new NotImplementedException();
        }
    }
}
