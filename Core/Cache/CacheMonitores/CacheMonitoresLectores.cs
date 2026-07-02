namespace SistemaConcurrente.Core.Cache.CacheMonitores
{
    public class CacheMonitores : ICache
    {
        private readonly Dictionary<int, EstadoOrden> _estados = new();

        private readonly object _monitor = new object();
        // recursos protegidos por el monitor
        private int escritoresActivos = 0;
        private int lectoresActivos = 0;

        public void Escribir(int ordenId, EstadoOrden estado)
        {
            this.PedidoEscribir();
            _estados[ordenId] = estado;
            this.LiberaEscribir();
        }

        public EstadoOrden? Leer(int ordenId)
        {
            this.PedidoLeer();
            var estado = _estados[ordenId];
            this.LiberaLeer();
            return estado;
        }

        private void PedidoEscribir()
        {
            lock (_monitor)
            {
                while (escritoresActivos > 0 || lectoresActivos > 0)
                    Monitor.Wait(_monitor);

                
            }
        }

        public ResumenCache LeerResumen()
        {
            throw new NotImplementedException();
        }
    }
}
