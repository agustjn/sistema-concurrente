namespace SistemaConcurrente.Core.Buffer
{
    //  Buffer limitado (productor/consumidor) resuelto con MONITORES, disciplina
    //  SIGNAL-AND-CONTINUE. Es la variante (b) del trabajo: misma interfaz IBuffer que
    //  BufferSemaforos, misma estructura de buffer circular, pero otra tecnica de sincronizacion.
    //
    //  Que significa signal-and-continue: el que hace Monitor.PulseAll NO le cede el monitor al que
    //  despierta. Sigue el, y recien lo suelta al salir del lock(){}. El despertado pasa a competir
    //  por reentrar. Por eso la espera va SIEMPRE con while (...) Monitor.Wait(...) y jamas con if:
    //  entre que lo despiertan y que consigue el monitor devuelta, otro hilo pudo entrar y dejar la
    //  condicion falsa otra vez (por ejemplo, otro consumidor se llevo la unica orden que habia).
    //  Con un if, ese hilo seguiria de largo y leeria una posicion invalida.
    //
    //  Diferencia principal contra BufferSemaforos (esto es lo que hay que contar en la comparacion
    //  cualitativa del punto 6 de la propuesta):
    //   - Alla la condicion se maneja con dos semaforos contadores (_vacio y _lleno) y CADA UNO
    //     lleva su cuenta solo; ademas hay dos mutex separados (depositar / retirar), asi que un
    //     productor y un consumidor pueden estar trabajando a la vez sobre puntas distintas del
    //     buffer circular.
    //   - Aca hay UN solo monitor para todo: la condicion se escribe explicita y a la vista
    //     (while ordenesEnBuffer == Capacidad), que se lee mucho mas facil, pero productores y
    //     consumidores se serializan entre si porque comparten el mismo lock.
    //     Se gana claridad y se pierde algo de paralelismo.
    public class BufferMonitores : IBuffer
    {
        // Capacidad = N = Tamanio del buffer
        public int Capacidad { get; }

        // Buffer para almacenar las órdenes
        private readonly Orden[] _bufferCircular;

        // Monitor unico: protege los indices y el contador. Aca no hay un mutex por operacion como
        // en la version de semaforos, porque la condicion de lleno/vacio se evalua sobre el mismo
        // estado compartido y tiene que mirarse de forma atomica.
        private readonly object _monitor = new object();

        // Cuantas ordenes hay AHORA en el buffer. Es el equivalente explicito a lo que en
        // BufferSemaforos llevaban los semaforos _vacio y _lleno: aca la cuenta se ve.
        private int ordenesEnBuffer = 0;

        public int Libre { get; set; }
        public int Ocupado { get; set; }
        public string EstrategiaBuffer { get => "Monitores"; }



        public BufferMonitores(int capacidad)
        {
            Capacidad = capacidad;
            Libre = 0;
            Ocupado = 0;
            _bufferCircular = new Orden[Capacidad];

        }

        public Orden DepositarDato(Orden orden)
        {
            lock (_monitor)
            {
                // Me demoro mientras el buffer este LLENO. while: al despertar puede haberse llenado
                // otra vez porque otro productor se me adelanto y ocupo el hueco.
                while (ordenesEnBuffer == Capacidad)
                    Monitor.Wait(_monitor);

                _bufferCircular[Libre] = orden;
                orden.InsertadoEnBufferEn = DateTime.Now;
                Libre = (Libre + 1) % Capacidad;
                ordenesEnBuffer++;

                // PulseAll y no Pulse: productores y consumidores esperan sobre el MISMO monitor
                // pero con condiciones distintas (lleno / vacio). Con Pulse podria tocarle el aviso
                // a otro productor que no puede avanzar, ese se vuelve a dormir, y la senial se
                // pierde para el consumidor que si podia -> deadlock con el buffer sin vaciarse.
                Monitor.PulseAll(_monitor);
            }

            return orden;
        }

        public Orden RetirarDato()
        {
            lock (_monitor)
            {
                // Me demoro mientras el buffer este VACIO. while: al despertar la orden pudo
                // habersela llevado otro consumidor que gano la carrera por el monitor.
                while (ordenesEnBuffer == 0)
                    Monitor.Wait(_monitor);

                var orden = _bufferCircular[Ocupado];
                orden.RetiradoDeBufferEn = DateTime.Now;
                Ocupado = (Ocupado + 1) % Capacidad;
                ordenesEnBuffer--;

                // Aviso que se libero un lugar: puede haber productores demorados por buffer lleno.
                Monitor.PulseAll(_monitor);

                return orden;
            }
        }




    }
}
