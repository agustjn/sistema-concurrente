namespace SistemaConcurrente.Core.Cache.CacheMonitores
{
    //  Cache de estados resuelta como LECTORES/ESCRITORES con MONITORES, disciplina
    //  SIGNAL-AND-CONTINUE y politica de PREFERENCIA A LECTORES.
    //
    //  Que significa signal-and-continue: cuando un hilo hace Monitor.PulseAll, NO le entrega el
    //  monitor al que despierta; sigue el mismo, y recien suelta el lock al salir del lock(){}. El
    //  despertado queda compitiendo por reentrar. Por eso la condicion se reevalua SIEMPRE con
    //  while (...) Monitor.Wait(...) y nunca con if: entre que lo despiertan y que consigue el
    //  monitor de nuevo, la condicion pudo volver a ser falsa (se la pudo "robar" otro hilo).
    //  Esta es la diferencia con la version de semaforos (CacheSemaforosJusta), donde el baton se
    //  pasa directo y el despertado ya arranca con la condicion garantizada, sin reevaluar.
    //
    //  Preferencia a lectores: el lector solo se frena si hay un escritor ACTIVO; no mira si hay
    //  escritores esperando. Entonces, mientras siga entrando un lector atras de otro, el escritor
    //  demorado nunca ve lectoresActivos == 0 y puede morir de hambre (inanicion). Eso no es un bug:
    //  es justamente el comportamiento que hay que mostrar y medir para contrastarlo con la politica
    //  justa (ver CacheSemaforosJusta, que suma la condicion de escritoresDemorados == 0).
    //
    //  OJO con el alcance del monitor: el lock protege SOLO los contadores, no el diccionario. El
    //  acceso real a _estados se hace afuera del lock; si se hiciera adentro, los lectores se
    //  serializarian entre si y se perderia todo el sentido de lectores/escritores.
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

            // ---------- seccion critica de escritura (EXCLUSIVA) ----------
            _estados[ordenId] = estado;
            // --------------------------------------------------------------

            this.LiberaEscribir();
        }

        public EstadoOrden? Leer(int ordenId)
        {
            this.PedidoLeer();

            // ---------- seccion de lectura (CONCURRENTE entre lectores) ----------
            // TryGetValue y no _estados[ordenId]: el contrato de ICache dice que si la orden
            // todavia no fue registrada se devuelve null, no que explote con KeyNotFoundException.
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
