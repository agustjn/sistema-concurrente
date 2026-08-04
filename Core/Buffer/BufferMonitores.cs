namespace SistemaConcurrente.Core.Buffer
{
    public class BufferMonitores : IBuffer
    {
        // Capacidad = N = Tamanio del buffer
        public int Capacidad { get; }

        // Buffer para almacenar las ordenes
        private readonly Orden[] _bufferCircular;

        private readonly object _monitor = new object();

        // Cuantas ordenes hay AHORA en el buffer
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
                // otra vez porque otro productor se me adelanto y ocupo el hueco
                while (ordenesEnBuffer == Capacidad)
                    Monitor.Wait(_monitor);

                _bufferCircular[Libre] = orden;
                orden.InsertadoEnBufferEn = DateTime.Now;
                Libre = (Libre + 1) % Capacidad;
                ordenesEnBuffer++;

                Monitor.PulseAll(_monitor);
            }

            return orden;
        }

        public Orden RetirarDato()
        {
            lock (_monitor)
            {
                // Me demoro mientras el buffer este VACIO. while: al despertar la orden pudo
                // habersela llevado otro consumidor que gano la carrera por el monitor
                while (ordenesEnBuffer == 0)
                    Monitor.Wait(_monitor);

                var orden = _bufferCircular[Ocupado];
                orden.RetiradoDeBufferEn = DateTime.Now;
                Ocupado = (Ocupado + 1) % Capacidad;
                ordenesEnBuffer--;

                // Aviso que se libero un lugar: puede haber productores demorados por buffer lleno
                Monitor.PulseAll(_monitor);

                return orden;
            }
        }




    }
}
