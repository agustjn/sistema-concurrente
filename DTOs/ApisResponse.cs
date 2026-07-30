namespace SistemaConcurrente.DTOs
{
    public record ApisResponse
    {
        public string Tecnica { get; set; }
        //public required ConfiguracionRun Configuracion { get; init; }
        public int OrdenesProcesadas { get; set; }
        public double TiempoTotalSegundos { get; set; }
        public double ThroughputOrdenesPorSegundo { get; set; }

        // Latencia extremo a extremo: PutAt → CompletedAt
        public double LatenciaPromedioMs { get; set; }
        public double LatenciaMinimaMs { get; set; }
        public double LatenciaMaximaMs { get; set; }
        public double LatenciaP95Ms { get; set; }

        // Tiempo de espera en buffer: PutAt → TakeAt (null para secuencial)
        public double? EsperaEnBufferPromedioMs { get; set; }

        // Tiempo de procesamiento del consumidor: TakeAt → CompletedAt
        public double TiempoProcesamientoPromedioMs { get; set; }

    }
}
