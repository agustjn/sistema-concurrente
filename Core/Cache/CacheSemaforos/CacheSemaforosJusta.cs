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
            _entrada.Wait();                                  // P(e): tomo el testigo
            // Me demoro si hay alguien usando la cache (lectores o un escritor).
            if (lectoresActivos > 0 || escritoresActivos > 0)
            {
                escritoresDemorados++;
                _entrada.Release();                           // suelto el testigo antes de dormir
                _colaEscritores.Wait();                       // P(w): duermo; despierto CON el testigo
            }
            escritoresActivos++;
            // Con un escritor activo nadie mas puede entrar: este SIGNAL terminara liberando la
            // entrada (no hay a quien pasarle el testigo todavia).
            Signal();

            // ---------- seccion critica de escritura (EXCLUSIVA) ----------
            _estados[ordenId] = estado;
            // --------------------------------------------------------------

            _entrada.Wait();                                  // P(e): retomo el testigo para salir
            escritoresActivos--;
            Signal();                                 // paso el testigo al proximo (lector o escritor)
        }

        public EstadoOrden? Leer(int ordenId)
        {
            EntrarComoLector();

           
            EstadoOrden? resultado = _estados.TryGetValue(ordenId, out var encontrado) ? encontrado : null;

            SalirComoLector();
            return resultado;
        }

        // ------------------------------------------------------------------------------------
        //  LECTOR: resumen (conteo por estado) de toda la cache. Misma sincronizacion de lector.
        // ------------------------------------------------------------------------------------
        public ResumenCache LeerResumen()
        {
            EntrarComoLector();

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

            SalirComoLector();
            return new ResumenCache(generadas, enProceso, finalizadas);
        }

        //    1) Si pueden leer (no hay escritor activo NI escritores esperando) y hay lectores
        //       demorados -> despierta a UN lector. (En cascada, ese lector volvera a llamar a
        //       este SIGNAL y despertara al siguiente, hasta vaciar la cola de lectores.)
        //    2) Si no, si puede escribir (no hay lectores ni escritores activos) y hay escritores
        //       demorados -> despierta a UN escritor.
        //    3) Si nadie puede avanzar -> libera la entrada (suelta el testigo).
        //
        //  La condicion de la rama (1) es la clave de la politica JUSTA: incluye
        //  "escritoresDemorados == 0", de modo que mientras haya un escritor esperando NO se
        //  habilitan lectores nuevos, evitando que los escritores mueran de hambre.
        private void Signal()
        {
            if (escritoresActivos == 0 && escritoresDemorados == 0 && lectoresDemorados > 0)
            {
                lectoresDemorados--;
                _colaLectores.Release();      // baton a un lector demorado
            }
            else if (lectoresActivos == 0 && escritoresActivos == 0 && escritoresDemorados > 0)
            {
                escritoresDemorados--;
                _colaEscritores.Release();    // baton a un escritor demorado
            }
            else
            {
                _entrada.Release();           // nadie puede avanzar: se suelta el testigo
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
            // Cascada: si quedan mas lectores demorados (y se puede leer), los voy despertando uno
            // a uno; cada uno repite este SIGNAL hasta vaciar la cola de lectores.
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
