namespace SistemaConcurrente.Core.Coordinadores
{
    // Se instancia UNA VEZ POR CORRIDA para que cada ejecucion
    // arranque de nuevo en 1 y las corridas no se contaminen entre si
    public class GeneradorIdsOrden
    {
        private int _ultimoId = 0;

        // Entrega el proximo Id de manera ATOMICA.
        public int ProximoId()
        {
            return Interlocked.Increment(ref _ultimoId);
        }
    }
}
