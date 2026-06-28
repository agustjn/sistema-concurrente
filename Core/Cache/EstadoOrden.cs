namespace SistemaConcurrente.Core.Cache
{
    // Estado de una orden dentro de la cache compartida.
    // El ciclo de vida es: Generada (la crea el productor) -> EnProceso (la toma el
    // consumidor) -> Finalizada (el consumidor termino de procesarla).
    public enum EstadoOrden
    {
        Generada,
        EnProceso,
        Finalizada
    }
}
