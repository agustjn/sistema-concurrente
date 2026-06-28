namespace SistemaConcurrente.Core.Cache
{
    // ============================================================================================
    //  Cache de estados de ordenes resuelta como problema de LECTORES / ESCRITORES,
    //  usando SEMAFOROS con la tecnica "PASSING THE BATON" (pasaje de testigo).
    //
    //  Politica: JUSTA (fair). Ni los lectores ni los escritores sufren inanicion:
    //    - Un escritor que llega y encuentra lectores leyendo queda demorado, pero los
    //      lectores que lleguen DESPUES tambien se demoran (ceden ante el escritor que espera).
    //      Asi el escritor entra apenas terminan los lectores que ya estaban adentro.
    //    - Un lector que llega y encuentra escritores (activos o esperando) se demora,
    //      pero en cuanto se vacian los escritores se libera en cascada a todos los lectores.
    //
    //  Reglas del acceso:
    //    - Varios LECTORES pueden leer a la vez (no se pisan entre si).
    //    - Un ESCRITOR necesita acceso EXCLUSIVO (ni lectores ni otros escritores en simultaneo).
    //
    //  Idea de "passing the baton": existe un unico "testigo" (el permiso para tocar los
    //  contadores de sincronizacion). Quien tiene el testigo, al terminar su tramo, en vez de
    //  liberar la entrada se lo PASA directamente a un proceso demorado que ya puede avanzar
    //  (haciendo Release de su cola). El que despierta hereda el testigo: NO vuelve a pedir la
    //  entrada. Si nadie puede avanzar, recien ahi se libera la entrada.
    // ============================================================================================
    public class CacheSemaforosJusta : ICache
    {
        // ----- Datos protegidos: el estado de cada orden (id -> estado) -----
        private readonly Dictionary<int, EstadoOrden> _estados = new();

        // ----- Contadores de sincronizacion (solo se tocan teniendo el testigo "_entrada") -----
        private int lectoresActivos = 0;       // (nr) lectores leyendo ahora mismo
        private int escritoresActivos = 0;     // (nw) escritores escribiendo ahora mismo (0 o 1)
        private int lectoresDemorados = 0;     // (dr) lectores dormidos esperando su turno
        private int escritoresDemorados = 0;   // (dw) escritores dormidos esperando su turno

        // ----- Semaforos -----
        // Testigo / entrada a la region de sincronizacion. Funciona como mutex sobre los contadores.
        private readonly SemaphoreSlim _entrada = new SemaphoreSlim(1, 1);
        // Cola donde se duermen los lectores demorados (semaforo de senalizacion, arranca en 0).
        private readonly SemaphoreSlim _colaLectores = new SemaphoreSlim(0, int.MaxValue);
        // Cola donde se duermen los escritores demorados (semaforo de senalizacion, arranca en 0).
        private readonly SemaphoreSlim _colaEscritores = new SemaphoreSlim(0, int.MaxValue);

        // ------------------------------------------------------------------------------------
        //  SIGNAL (pasar el testigo).
        //  Lo invoca quien tiene el testigo cuando termina de tocar los contadores. Decide a
        //  quien pasarselo. Se evalua EXACTAMENTE una rama (passing the baton pasa el testigo a
        //  UN solo proceso):
        //
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
        // ------------------------------------------------------------------------------------
        private void PasarElTestigo()
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

        // ------------------------------------------------------------------------------------
        //  ESCRITOR: acceso exclusivo para registrar/actualizar el estado de una orden.
        // ------------------------------------------------------------------------------------
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
            PasarElTestigo();

            // ---------- seccion critica de escritura (EXCLUSIVA) ----------
            _estados[ordenId] = estado;
            // --------------------------------------------------------------

            _entrada.Wait();                                  // P(e): retomo el testigo para salir
            escritoresActivos--;
            PasarElTestigo();                                 // paso el testigo al proximo (lector o escritor)
        }

        // ------------------------------------------------------------------------------------
        //  LECTOR: consulta el estado de una orden. Varios lectores conviven en paralelo.
        // ------------------------------------------------------------------------------------
        public EstadoOrden? Leer(int ordenId)
        {
            EntrarComoLector();

            // ---------- seccion de lectura (CONCURRENTE entre lectores) ----------
            EstadoOrden? resultado = _estados.TryGetValue(ordenId, out var estado)
                ? estado
                : (EstadoOrden?)null;
            // ---------------------------------------------------------------------

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

        // ------------------------------------------------------------------------------------
        //  Protocolo de ENTRADA de un lector (compartido por Leer y LeerResumen).
        // ------------------------------------------------------------------------------------
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
            PasarElTestigo();
        }

        // ------------------------------------------------------------------------------------
        //  Protocolo de SALIDA de un lector.
        // ------------------------------------------------------------------------------------
        private void SalirComoLector()
        {
            _entrada.Wait();                                  // P(e): retomo el testigo para salir
            lectoresActivos--;
            // Si fui el ultimo lector, el SIGNAL podra habilitar a un escritor demorado.
            PasarElTestigo();
        }
    }
}
