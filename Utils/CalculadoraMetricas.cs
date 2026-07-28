using SistemaConcurrente.Core;
using SistemaConcurrente.DTOs;

namespace SistemaConcurrente.Utils
{
    // Toma las ordenes reales completadas de una corrida y arma el ApisResponse (el JSON de metricas).
    public static class CalculadoraMetricas
    {
        public static ApisResponse Calcular(IReadOnlyList<Orden> ordenes, double tiempoTotalSeg, string tecnica)
        {
            int n = ordenes.Count;

            // Si no se proceso nada, devolvemos un response en cero (evita dividir por 0 / promedios
            // sobre vacio, y no convierte una corrida vacia en un 500 de la API).
            if (n == 0)
                return new ApisResponse { Tecnica = tecnica };


            // Latencia extremo a extremo (CreadoEn -> CompletadoEn)
            var latencias = ordenes
                .Select(orden => orden.LatenciaMs ?? 0)
                .OrderBy(latenciaMs => latenciaMs)
                .ToList();

            // Espera en buffer (InsertadoEnBufferEn -> RetiradoDeBufferEn): solo aplica si la
            // corrida paso por un buffer. En la secuencial esos timestamps son null y la metrica
            // no corresponde: queda null ("la pregunta no aplica"), no cero ("espero 0 ms").
            bool huboBuffer = ordenes[0].InsertadoEnBufferEn.HasValue && ordenes[0].RetiradoDeBufferEn.HasValue;

            double? esperaBufferProm = huboBuffer
                ? ordenes.Average(o => (o.RetiradoDeBufferEn!.Value - o.InsertadoEnBufferEn!.Value).TotalMilliseconds)
                : null;

            // Tiempo de procesamiento: en el concurrente es RetiradoDeBufferEn -> CompletadoEn
            // (lo que trabajo el consumidor); en el secuencial no hay retiro, asi que es
            // CreadoEn -> CompletadoEn (la orden entera, sus dos simulaciones).
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

        //  Percentil sobre una lista YA ordenada ascendente. p en [0,1] (ej. 0.95 = P95).

        //  ¿Qué es el P95(percentil 95)?

        //  Es el valor de latencia por debajo del cual cae el 95% de las órdenes. Dicho de otra forma: solo el 5% más lento supera ese número.
        //  Es una métrica clásica de rendimiento porque el promedio engaña. Mirá este ejemplo con 20 latencias (en ms):
        //  10, 11, 11, 12, 12, 12, 13, 13, 13, 14, 14, 14, 15, 15, 16, 16, 17, 18, 19, 350
        //  Promedio ≈ 27 ms → parece "ok", pero ninguna orden real tardó eso; el 350 lo infla.
        //  P95 = 19 ms → el 95% de las órdenes terminó en 19 ms o menos.
        //  Máximo = 350 ms → el peor caso puntual.
        private static double Percentil(List<double> ordenados, double p)
        {
            int idx = (int)Math.Ceiling(p * ordenados.Count) - 1;
            idx = Math.Clamp(idx, 0, ordenados.Count - 1);
            return ordenados[idx];
        }
    }
}
