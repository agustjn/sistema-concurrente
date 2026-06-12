namespace SistemaConcurrente.Core.Cache
{
    public class OrdenEstado
    {
        public enum Estado { Generada, EnProceso, Finalizada }

        public Estado EstadoActual { get; private set; }

        public OrdenEstado() 
        {
            EstadoActual = Estado.Generada;
        }

        public void AvanzarEstado()
        {
            switch (EstadoActual)
            {
                case Estado.Generada:
                    EstadoActual = Estado.EnProceso;
                    break;
                case Estado.EnProceso:
                    EstadoActual = Estado.Finalizada;
                    break;
                case Estado.Finalizada:
                    // No se puede avanzar más allá de Finalizada
                    break;
            }
        }
    }
}
