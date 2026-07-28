namespace SistemaConcurrente.Core.Cache.CacheMonitores
{
    public class CacheMonitoresJusta : ICache
    {
        private readonly object _monitor = new object();
        private readonly Dictionary<int, EstadoOrden> _estados = new();
        private int escritoresDemorados, escritoresActivos, lectoresDemorados, lectoresActivos = 0;

        public void Escribir(int ordenId, EstadoOrden estado)
        {
            this.PedidoEscribir();
            _estados[ordenId] = estado;
            this.LiberaEscribir();
        }

        private void PedidoEscribir()
        {
            lock (_monitor)
            {
                escritoresDemorados++;
                while (lectoresActivos > 0 || escritoresActivos > 0)
                {
                    Monitor.Wait(_monitor);
                }
                escritoresDemorados--;
                escritoresActivos++;
            }
        }

        private void LiberaEscribir()
        {
            lock (_monitor)
            {
                escritoresActivos--;
                Monitor.PulseAll(_monitor);
            }
        }

        public EstadoOrden? Leer(int ordenId)
        {
            this.PedidoLeer();
            EstadoOrden? estado = _estados.TryGetValue(ordenId, out var encontrado) ? encontrado : null;
            this.LiberaLeer();
            return estado;
        }

        private void PedidoLeer()
        {
            lock (_monitor)
            {
                lectoresDemorados++;
                while (escritoresActivos > 0 || escritoresDemorados > 0)
                    Monitor.Wait(_monitor);

                lectoresDemorados--;
                lectoresActivos++;
            }
        }

        private void LiberaLeer()
        {
            lock (_monitor)
            {
                lectoresActivos--;
                if (lectoresActivos == 0) // 
                    Monitor.PulseAll(_monitor);  // Debo obligatoriamente hacer PulseaLL debido a que al no tener distintas variables condicion (colas de espera) para los procesos escritores y lectores, 
                                                    // me encuentro en la obligacion de despertar a TODOS los procesos y que cada uno rechequee la condicion, aun asi en ese chequeo, si hay un escritor
                                                    // esperando, va a tener el acceso exclusivo
            }
        }

        public ResumenCache LeerResumen()
        {
            throw new NotImplementedException();
        }
    }
}
