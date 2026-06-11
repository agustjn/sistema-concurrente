namespace SistemaConcurrente.DTOs
{
    public record ApisResponse
    {
        
            public required int RunId { get; init; }
            public required string Tecnica { get; init; }
            //public required ConfiguracionRun Configuracion { get; init; }
            public required int OrdenesProcesadas { get; init; }
            public required double TiempoTotalSegundos { get; init; }
            public required double ThroughputOrdenesPorSegundo { get; init; }

            // Latencia extremo a extremo: PutAt → CompletedAt
            public required double LatenciaPromedioMs { get; init; }
            public required double LatenciaMinimaMs { get; init; }
            public required double LatenciaMaximaMs { get; init; }
            public required double LatenciaP95Ms { get; init; }

            // Tiempo de espera en buffer: PutAt → TakeAt (null para secuencial)
            public required double? EsperaEnBufferPromedioMs { get; init; }

            // Tiempo de procesamiento del consumidor: TakeAt → CompletedAt
            public required double TiempoProcesamientoPromedioMs { get; init; }
        
    }
}
