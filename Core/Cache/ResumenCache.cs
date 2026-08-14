namespace SistemaConcurrente.Core.Cache
{
    // Foto del contenido de la cache en un instante: cuantas ordenes hay en cada estado.
    // Es lo que devuelve una lectura del resumen.
    public record ResumenCache(int Generadas, int EnProceso, int Finalizadas)
    {
        public int Total => Generadas + EnProceso + Finalizadas;
    }
}
