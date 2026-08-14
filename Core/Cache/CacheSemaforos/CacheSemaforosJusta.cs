namespace SistemaConcurrente.Core.Cache.CacheSemaforos
{

    public class CacheSemaforosJusta : ICache
    {
        
        private readonly Dictionary<int, EstadoOrden> _estados = new();

        private int lectoresActivos = 0;       // (nr) lectores leyendo ahora mismo
        private int escritoresActivos = 0;     // (nw) escritores escribiendo ahora mismo (0 o 1)
        private int lectoresDemorados = 0;     // (dr) lectores dormidos esperando su turno
        private int escritoresDemorados = 0;   // (dw) escritores dormidos esperando su turno

        // Testigo. Funciona como mutex sobre los contadores.
        private readonly SemaphoreSlim _entrada = new SemaphoreSlim(1, 1);
        // Cola donde se duermen los lectores demorados (arranca en 0).
        private readonly SemaphoreSlim _colaLectores = new SemaphoreSlim(0, int.MaxValue);
        // Cola donde se duermen los escritores demorados (arranca en 0).
        private readonly SemaphoreSlim _colaEscritores = new SemaphoreSlim(0, int.MaxValue);


        public void Escribir(int ordenId, EstadoOrden estado)
        {
            _entrada.Wait();                                  
            
            if (lectoresActivos > 0 || escritoresActivos > 0)
            {
                escritoresDemorados++;
                _entrada.Release();                           
                _colaEscritores.Wait();                       
            }
            escritoresActivos++;
            
            Signal();

            // seccion critica
            _estados[ordenId] = estado;


            _entrada.Wait();                                  
            escritoresActivos--;
            Signal();                                 
        }

        public EstadoOrden? Leer(int ordenId)
        {
            EntrarComoLector();

            // seccion critica
            EstadoOrden? resultado = _estados.TryGetValue(ordenId, out var encontrado) ? encontrado : null;

            SalirComoLector();
            return resultado;
        }

        // LECTOR: resumen (conteo por estado) de toda la cache. Misma sincronizacion de lector.
        public ResumenCache LeerResumen()
        {
            EntrarComoLector();

            // seccion de lectura (CONCURRENTE entre lectores)
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

            SalirComoLector();
            return new ResumenCache(generadas, enProceso, finalizadas);
        }

        // SIGNAL: al soltar el testigo decide a quien le pasa el baton.
        //  1) Si no hay escritor activo ni escritores esperando y hay lectores demorados,
        //     despierta a UN lector, que al salir repite este SIGNAL (cascada de lectores).
        //  2) Si no hay nadie activo y hay escritores demorados, despierta a UN escritor.
        //  3) Si nadie puede avanzar, libera la entrada.
        // La condicion de (1) es la de la politica justa: mientras haya un escritor esperando no
        // entran lectores nuevos, y asi los escritores no quedan en inanicion.
        private void Signal()
        {
            if (escritoresActivos == 0 && escritoresDemorados == 0 && lectoresDemorados > 0)
            {
                lectoresDemorados--;
                _colaLectores.Release();      
            }
            else if (lectoresActivos == 0 && escritoresActivos == 0 && escritoresDemorados > 0)
            {
                escritoresDemorados--;
                _colaEscritores.Release();    
            }
            else
            {
                _entrada.Release();           
            }
        }

        private void EntrarComoLector()
        {
            _entrada.Wait();                                  // P(e): tomo el testigo
            // JUSTA: me demoro si hay un escritor activo O escritores esperando (les cedo el paso).
            if (escritoresActivos > 0 || escritoresDemorados > 0)
            {
                lectoresDemorados++;
                _entrada.Release();                           // suelto el testigo antes de dormir
                _colaLectores.Wait();                         // P(r): duermo; despierto CON el testigo
            }
            lectoresActivos++;
            // Cascada: si quedan lectores demorados los voy despertando de a uno.
            Signal();
        }

        private void SalirComoLector()
        {
            _entrada.Wait();                                  // P(e): retomo el testigo para salir
            lectoresActivos--;
            // Si fui el ultimo lector, el SIGNAL podra habilitar a un escritor demorado.
            Signal();
        }
    }
}
