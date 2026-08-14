using SistemaConcurrente.Core;
using SistemaConcurrente.DTOs;

namespace SistemaConcurrente.Utils
{
    // Arma el ApisResponse con las metricas de una corrida a partir de las ordenes completadas.
    public static class CalculadoraMetricas
    {
        public static ApisResponse Calcular(IReadOnlyList<Orden> ordenes, double tiempoTotalSeg, string tecnica)
        {
            int n = ordenes.Count;

            // Si no se proceso nada devuelvo el response en cero, para no dividir por cero.
            if (n == 0)
                return new ApisResponse { Tecnica = tecnica };


            // Latencia extremo a extremo (CreadoEn -> CompletadoEn)
            var latencias = ordenes
                .Select(orden => orden.LatenciaMs ?? 0)
                .OrderBy(latenciaMs => latenciaMs)
                .ToList();

            // Espera en buffer: solo aplica en las corridas concurrentes. En la secuencial no hay
            // buffer, asi que los timestamps son null y la metrica queda en null.
            bool huboBuffer = ordenes[0].InsertadoEnBufferEn.HasValue && ordenes[0].RetiradoDeBufferEn.HasValue;

            double? esperaBufferProm = huboBuffer
                ? ordenes.Average(o => (o.RetiradoDeBufferEn!.Value - o.InsertadoEnBufferEn!.Value).TotalMilliseconds)
                : null;

            // Tiempo de procesamiento: en el concurrente va de RetiradoDeBufferEn a CompletadoEn.
            // En el secuencial no hay retiro, asi que se mide de CreadoEn a CompletadoEn.
            double procesamientoProm = huboBuffer
                ? ordenes.Average(o => (o.CompletadoEn!.Value - o.RetiradoDeBufferEn!.Value).TotalMilliseconds)
                : ordenes.Average(o => (o.CompletadoEn!.Value - o.CreadoEn).TotalMilliseconds);

            return new ApisResponse
            {
                Tecnica = tecnica,
                OrdenesProcesadas = n,
                TiempoTotalSegundos = tiempoTotalSeg,
                ThroughputOrdenesPorSegundo = tiempoTotalSeg > 0 ? n / tiempoTotalSeg : 0,
                LatenciaPromedioMs = latencias.Average(),
                LatenciaMinimaMs = latencias.First(),
                LatenciaMaximaMs = latencias.Last(),
                LatenciaP95Ms = Percentil(latencias, 0.95),
                EsperaEnBufferPromedioMs = esperaBufferProm,
                TiempoProcesamientoPromedioMs = procesamientoProm
            };
        }

        // Percentil sobre la lista ya ordenada ascendente. p en [0,1] (ej. 0.95 = P95).
        private static double Percentil(List<double> ordenados, double p)
        {
            int idx = (int)Math.Ceiling(p * ordenados.Count) - 1;
            idx = Math.Clamp(idx, 0, ordenados.Count - 1);
            return ordenados[idx];
        }
    }
}
