namespace SistemaConcurrente.Core.Coordinadores
{
    public record ConfiguracionRun
    {
        public int CapacidadBuffer { get; set; }
        public int CantOrdenes { get; set; }
        public int CantConsumidores { get; set; }
        public int CantProductores { get; set; }
        public int CantIteraciones { get; set; }
    }
}
