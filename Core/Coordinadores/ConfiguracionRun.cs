namespace SistemaConcurrente.Core.Coordinadores
{
    public record ConfiguracionRun
    {
        public int CapacidadBuffer { get; set; }
        public int CantOrdenes { get; set; }
        public int CantConsumidores { get; set; }
        public int CantProductores { get; set; }
        // Cantidad de hilos lectores que consultan la cache periodicamente (default: 2).
        public int CantLectores { get; set; } = 2;
        public int CantIteraciones { get; set; }
        // Rango entre:
        public int intervaloMsDeLecturas { get; set; }
    }
}
