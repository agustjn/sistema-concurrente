using Microsoft.AspNetCore.Components.Forms;

namespace SistemaConcurrente.Core.Buffer
{
    public class BufferSemaforos : IBuffer
    {
        // Capacidad = N = Tamanio del buffer
        public int Capacidad { get; }

        // Buffer para almacenar las órdenes
        private readonly Orden[] _bufferCircular;

        // Semaforo de 0 a N-1, inicia en N
        private readonly SemaphoreSlim _vacio;

        // Semaforo de 0 a N-1, inicia en 0
        private readonly SemaphoreSlim _lleno;
        // Semaforo que representa la exclusion mutua para depositar en el buffer circular
        private readonly SemaphoreSlim _mutexDepositar = new SemaphoreSlim(1, 1);

        // Semaforo que representa la exclusion mutua para retirar del buffer circular
        private readonly SemaphoreSlim _mutexRetirar = new SemaphoreSlim(1, 1);

        public int Libre { get; set; }
        public int Ocupado { get; set; }
        public string EstrategiaBuffer { get => "Semaforos"; }



        //  Indi
        public BufferSemaforos(int capacidad)
        {
            Capacidad = capacidad;
            _vacio = new SemaphoreSlim(Capacidad, Capacidad);
            _lleno = new SemaphoreSlim(0, Capacidad);
            Libre = 0;
            Ocupado = 0;
            _bufferCircular = new Orden[Capacidad];
            
        }

        public Orden DepositarDato(Orden orden)
        {
            _vacio.Wait();
            _mutexDepositar.Wait();
            _bufferCircular[Libre] = orden;
            orden.InsertadoEnBufferEn = DateTime.Now;
            Libre = (Libre + 1) % Capacidad;
            _mutexDepositar.Release();
            _lleno.Release();
            return orden;
        }

        public Orden RetirarDato()
        {
            _lleno.Wait();
            _mutexRetirar.Wait();
            var orden = _bufferCircular[Ocupado];
            orden.RetiradoDeBufferEn = DateTime.Now;
            Ocupado = (Ocupado + 1) % Capacidad;
            _mutexRetirar.Release();
            return orden;
        }



        
        





    }
}
