namespace SistemaConcurrente.Core.Persistencia
{
    // Fila de la tabla Ordenes: el espejo persistido de una Orden completada.
    //
    // El Id de la orden es la PK y NO es identity: es el id que repartio
    // GeneradorIdsOrden (1..N). Como cada endpoint limpia la tabla antes de correr,
    // la PK garantiza a nivel base de datos el indicador de correctitud de la
    // propuesta: si un consumidor procesara (y guardara) la misma orden dos veces,
    // el insert duplicado revienta. "Persistida exactamente una vez" lo controla
    // la DB, no el codigo.
    public class OrdenPersistencia
    {
        public int OrdenId { get; set; }

        // Con que endpoint/variante se corrio (Secuencial, SemaforosJusta, etc.)
        public string Tecnica { get; set; } = "";

        public double Monto { get; set; }
        public double? ValorCalculado { get; set; }

        // Misma instrumentacion de tiempos que Orden. Los dos del buffer quedan
        // null en la corrida secuencial (no hay buffer; la metrica "no aplica").
        public DateTime CreadoEn { get; set; }
        public DateTime? InsertadoEnBufferEn { get; set; }
        public DateTime? RetiradoDeBufferEn { get; set; }
        public DateTime? CompletadoEn { get; set; }
        public double? LatenciaMs { get; set; }
    }
}
