namespace SistemaConcurrente.Core.Coordinadores
{
    public class ContadorOrdenes
    {
        private int _ordenesRestantes;

        public ContadorOrdenes(int totalOrdenes)
        {
            _ordenesRestantes = totalOrdenes;
        }

        // Realiza una resta de manera ATOMICA. Devuelve siempre true cuando las ordenes restantes sean mayores a 0, cuando sea 0 retorna false
        public bool DescontarOrden()
        {
            return Interlocked.Decrement(ref _ordenesRestantes) >= 0;
        }
    }
}
