namespace SistemaConcurrente.Core.Cache.CacheSemaforos
{
    public class CacheSemaforos : ICache
    {
        private readonly Dictionary<int, EstadoOrden> _estados = new();

        private readonly SemaphoreSlim _colaLectores = new SemaphoreSlim(1, int.MaxValue);
        private int _lectoresActivos = 0;
        private readonly SemaphoreSlim _colaEscritores = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);


        public void Escribir(int ordenId, EstadoOrden estado)
        {

            _colaEscritores.Wait();
            _estados[ordenId] = estado;
            _colaEscritores.Release();

        }

        public EstadoOrden? Leer(int ordenId)
        {
            _mutex.Wait();
            _lectoresActivos++;
            if (_lectoresActivos == 1)
                _colaEscritores.Wait();
            _mutex.Release();
            var estadoOrden = _estados[ordenId];
            _mutex.Wait();
            _lectoresActivos--;
            if (_lectoresActivos == 0)
                _colaEscritores.Release();
            _mutex.Release();

            return estadoOrden;
        }

        public ResumenCache LeerResumen()
        {
            throw new NotImplementedException();
        }
    }
}
