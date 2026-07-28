namespace SistemaConcurrente.Core.Cache.CacheMonitores
{
    public class CacheMonitoresLectores : ICache
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

            EstadoOrden? estado = _estados.TryGetValue(ordenId, out var encontrado) ? encontrado : null;

            this.LiberaLeer();
            return estado;
        }

        //  LECTOR: resumen (conteo por estado) de toda la cache. Misma sincronizacion de lector.
        public ResumenCache LeerResumen()
        {
            this.PedidoLeer();

            // ---------- seccion de lectura (CONCURRENTE entre lectores) ----------
            int generadas = 0, enProceso = 0, finalizadas = 0;
            foreach (var estado in _estados.Values)
            {
                switch (estado)
                {
                    case EstadoOrden.Generada: generadas++; break;
                    case EstadoOrden.EnProceso: enProceso++; break;
                    case EstadoOrden.Finalizada: finalizadas++; break;
                }
            }
            // ---------------------------------------------------------------------

            this.LiberaLeer();
            return new ResumenCache(generadas, enProceso, finalizadas);
        }

        
        //  Protocolo de ENTRADA de un escritor: espera a que no quede NADIE usando la cache.
        private void PedidoEscribir()
        {
            lock (_monitor)
            {
                while (escritoresActivos > 0 || lectoresActivos > 0)
                    Monitor.Wait(_monitor);

                // Me marco activo ANTES de soltar el monitor: desde aca cualquier otro que pida
                // entrar (lector o escritor) va a ver escritoresActivos > 0 y se va a demorar.
                escritoresActivos++;
            }
        }

        //  Protocolo de SALIDA de un escritor.
        private void LiberaEscribir()
        {
            lock (_monitor)
            {
                escritoresActivos--;
                Monitor.PulseAll(_monitor);
            }
        }

        
        //  PREFERENCIA A LECTORES: solo miro escritoresActivos. No me importa si hay escritores
        //  esperando, les paso por arriba. De ahi sale la posible inanicion de escritores.
        private void PedidoLeer()
        {
            lock (_monitor)
            {
                while (escritoresActivos > 0)
                    Monitor.Wait(_monitor);

                lectoresActivos++;
            }
        }

        //  Protocolo de SALIDA de un lector.
        private void LiberaLeer()
        {
            lock (_monitor)
            {
                lectoresActivos--;
                // Solo tiene sentido avisar si fui el ULTIMO lector: recien ahi un escritor demorado
                // puede llegar a pasar su while
                if (lectoresActivos == 0)
                    Monitor.PulseAll(_monitor);
            }
        }
    }
}
