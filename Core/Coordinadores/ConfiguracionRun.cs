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
        // Cada cuanto (ms) cada hilo lector vuelve a consultar la cache. Es LA PERILLA de la
        // proporcion lecturas/escrituras del experimento de la secc. 7: mas chico = mas presion
        // de lectores. Default DELIBERADO: 50ms (lectura periodica sin saturar; 0 = lectores
        // sin pausa, que es un caso de estres y se pide explicito, no por omision).
        public int IntervaloMsDeLecturas { get; set; } = 50;
    }
}
