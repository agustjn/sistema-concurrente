namespace SistemaConcurrente.Core.Cache.CacheSemaforos
{
    public class CacheSemaforos : ICache
    {
        private readonly Dictionary<int, EstadoOrden> _estados = new();

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
            EntrarComoLector();

            // Contrato de ICache: null si la orden todavia no fue registrada (el indexer directo tiraba KeyNotFoundException)
            EstadoOrden? estadoOrden = _estados.TryGetValue(ordenId, out var encontrado) ? encontrado : null;

            SalirComoLector();
            return estadoOrden;
        }

        public ResumenCache LeerResumen()
        {
            EntrarComoLector();

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

        private void EntrarComoLector()
        {
            _mutex.Wait();
            _lectoresActivos++;
            if (_lectoresActivos == 1)
                _colaEscritores.Wait();
            _mutex.Release();
        }

        private void SalirComoLector()
        {
            _mutex.Wait();
            _lectoresActivos--;
            if (_lectoresActivos == 0)
                _colaEscritores.Release();
            _mutex.Release();
        }
    }
}
