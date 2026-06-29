namespace SistemaConcurrente.Core.Cache
{
    public class CacheSemaforosLectores : ICache
    {
        public void Escribir(int ordenId, EstadoOrden estado)
        {
            throw new NotImplementedException();
        }

        public EstadoOrden? Leer(int ordenId)
        {
            throw new NotImplementedException();
        }

        public ResumenCache LeerResumen()
        {
            throw new NotImplementedException();
        }
    }
}
