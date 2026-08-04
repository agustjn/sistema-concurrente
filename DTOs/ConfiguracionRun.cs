namespace SistemaConcurrente.DTOs
{
    public record ConfiguracionRun
    {
        public int CapacidadBuffer { get; set; }
        public int CantOrdenes { get; set; }
        public int CantConsumidores { get; set; }
        public int CantProductores { get; set; }
        // Cantidad de hilos lectores que consultan la cache periodicamente (default: 2)
        public int CantLectores { get; set; } = 2;
        public int CantIteraciones { get; set; }
        // Cada cuanto (ms) cada hilo lector vuelve a consultar la cache
        public int IntervaloMsDeLecturas { get; set; } = 50;
    }
}
