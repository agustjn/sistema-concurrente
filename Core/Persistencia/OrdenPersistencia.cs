namespace SistemaConcurrente.Core.Persistencia
{
    // Fila de la tabla Ordenes. El Id es la PK y no es identity: es el que reparte
    // GeneradorIdsOrden (1..N), asi que si una orden se guardara dos veces el insert falla
    // y el duplicado lo detecta la propia base.
    public class OrdenPersistencia
    {
        public int OrdenId { get; set; }

        // Con que endpoint/variante se corrio (Secuencial, SemaforosJusta, etc.)
        public string Tecnica { get; set; } = "";

        public double Monto { get; set; }
        public double? ValorCalculado { get; set; }

        // Mismos tiempos que Orden. Los dos del buffer quedan en null en la corrida secuencial.
        public DateTime CreadoEn { get; set; }
        public DateTime? InsertadoEnBufferEn { get; set; }
        public DateTime? RetiradoDeBufferEn { get; set; }
        public DateTime? CompletadoEn { get; set; }
        public double? LatenciaMs { get; set; }
    }
}
