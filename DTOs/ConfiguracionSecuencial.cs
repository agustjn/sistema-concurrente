namespace SistemaConcurrente.DTOs
{
    public record ConfiguracionSecuencial
    {
        public int CantOrdenes { get; set; }
        public int CantIteraciones { get; set; }
    }
}
