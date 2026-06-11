namespace SistemaConcurrente.Core.Coordinadores
{
    public record ConfiguracionRun
    {
        public int CapacidadBuffer { get; }
        public int CantConsumidores { get; }
        public int CantProductores { get; }

    }
}
