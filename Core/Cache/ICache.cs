namespace SistemaConcurrente.Core.Cache
{
    public interface ICache
    {
        void Escribir(int ordenId, EstadoOrden estado);

        ResumenCache LeerResumen();
    }
}
