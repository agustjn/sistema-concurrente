namespace SistemaConcurrente.Core.Coordinadores
{
    // Reparte los Id de las ordenes de una corrida.
    //
    // Antes cada productor se armaba el Id solo, con rand.Next(1, 100000). El problema es que el
    // random NO garantiza unicidad: con 1.000 ordenes ya se esperan repetidos, y con 10.000 hay
    // cientos. Como la cache se indexa por ordenId, dos ordenes distintas con el mismo Id se
    // pisaban el estado entre si, y ademas quedaba sin sentido el indicador de "cada orden se
    // procesa exactamente una vez", porque sin identidad unica no hay forma confiable de contarlas.
    //
    // Se instancia UNA VEZ POR CORRIDA (no es estatico a proposito) para que cada ejecucion
    // arranque de nuevo en 1 y las corridas no se contaminen entre si.
    public class GeneradorIdsOrden
    {
        private int _ultimoId = 0;

        // Entrega el proximo Id de manera ATOMICA. Interlocked.Increment garantiza que dos
        // productores concurrentes nunca se lleven el mismo numero. El primer Id entregado es el 1.
        public int ProximoId()
        {
            return Interlocked.Increment(ref _ultimoId);
        }
    }
}
