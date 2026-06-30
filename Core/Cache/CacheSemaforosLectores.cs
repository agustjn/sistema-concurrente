namespace SistemaConcurrente.Core.Cache
{
    public class CacheSemaforosLectores : ICache
    {

        private readonly SemaphoreSlim _colaLectores = new SemaphoreSlim(1, int.MaxValue);
        private int _lectoresActivos = 0;
        private int _lectoresDemorados = 0;
        private readonly SemaphoreSlim _colaEscritores = new SemaphoreSlim(0, int.MaxValue);
        private int _escritoresDemorados = 0;
        private int _escritoresActivos = 0;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
        private readonly Dictionary<int, EstadoOrden> _estados = new();


        public void Escribir(int ordenId, EstadoOrden estado)
        {

            _mutex.Wait();
            if (_lectoresActivos == 0 || _escritoresActivos == 1)
            {
                _escritoresDemorados++;
                _mutex.Release();
                _colaEscritores.Wait();               
            }
            _escritoresActivos++;
            _estados[ordenId] = estado;
            _mutex.Release();
            if (_escritoresDemorados )
            _colaEscritores.Release();


        }

        public EstadoOrden? Leer(int ordenId)
        {
            _mutex.Wait();
            if (_escritoresActivos == 1)
            {
                _mutex.Release();
                _colaLectores.Wait();
            }
            
        }

        public ResumenCache LeerResumen()
        {
            throw new NotImplementedException();
        }
    }
}
