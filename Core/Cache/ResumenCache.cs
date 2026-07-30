namespace SistemaConcurrente.Core.Cache
{
    // Foto (snapshot) del contenido de la cache en un instante: cuantas ordenes hay
    // en cada estado. Es lo que devuelve una operacion de LECTURA del resumen, y lo
    // que los hilos lectores imprimen periodicamente para mostrar el avance.
    public record ResumenCache(int Generadas, int EnProceso, int Finalizadas)
    {
        public int Total => Generadas + EnProceso + Finalizadas;
    }
}
