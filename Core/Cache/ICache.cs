namespace SistemaConcurrente.Core.Cache
{
    // Contrato de la cache compartida del estado de las ordenes.
    //
    // Es agnostico de la tecnica de sincronizacion: la implementacion concreta decide
    // como resolver el problema de lectores/escritores (semaforos con passing the baton,
    // monitores, etc.). Asi, mas adelante pueden convivir varias politicas detras de
    // esta misma interfaz.
    public interface ICache
    {
        // Operacion de ESCRITOR: registra o actualiza el estado de una orden.
        // Requiere acceso EXCLUSIVO (ningun otro lector ni escritor en simultaneo).
        void Escribir(int ordenId, EstadoOrden estado);

        // Operacion de LECTOR: consulta el estado de una orden puntual.
        // Varios lectores pueden ejecutarla concurrentemente entre si.
        // Devuelve null si la orden todavia no fue registrada.
        EstadoOrden? Leer(int ordenId);

        // Operacion de LECTOR: devuelve un resumen (conteo por estado) de toda la cache.
        ResumenCache LeerResumen();
    }
}
