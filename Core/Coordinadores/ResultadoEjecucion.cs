namespace SistemaConcurrente.Core.Coordinadores
{
    // Resultado crudo de una corrida: las ordenes reales completadas y el tiempo total medido.
    // A partir de esto, mas adelante, la calculadora arma el ApisResponse.
    public record ResultadoEjecucion(IReadOnlyList<Orden> Ordenes, double TiempoTotalSegundos);
}
