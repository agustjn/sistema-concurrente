using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Cache.CacheMonitores;
using SistemaConcurrente.Core.Cache.CacheSemaforos;
using SistemaConcurrente.Core.Coordinadores;
using Xunit;

namespace SistemaConcurrente.Tests.Fixtures
{
    // Una combinacion buffer x cache, la misma que arma cada endpoint de ApisConcurrentes.
    //
    // Los tests reciben FABRICAS (Func) y no instancias: cada corrida necesita
    // buffer y cache NUEVOS, porque los dos guardan estado (indices, contadores, semaforos)
    // y si se reutilizaran entre corridas se contaminarian entre si, igual que pasa con
    // GeneradorIdsOrden, que se instancia una vez por corrida.
    public record VarianteRun(string Tecnica, Func<int, IBuffer> CrearBuffer, Func<ICache> CrearCache)
    {
        // Para que el runner muestre el nombre de la variante en cada caso del [Theory],
        // en vez de un "VarianteRun { ... }" ilegible.
        public override string ToString() => Tecnica;
    }

    public static class Variantes
    {
        // Las 4 combinaciones de la API, con las mismas etiquetas de tecnica que usan los
        // endpoints. LA IDEA ARQUITECTONICA CENTRAL del sistema (los actores dependen SOLO
        // de IBuffer e ICache) es lo que permite que UN MISMO test corra contra todas sin
        // tocar una linea: se cambia la tecnica de sincronizacion y el codigo de alrededor
        // es exactamente el mismo.
        public static TheoryData<VarianteRun> Todas => new()
        {
            new VarianteRun("SemaforosJusta",
                cap => new BufferSemaforos(cap), () => new CacheSemaforosJusta()),
            new VarianteRun("SemaforosPreferenciaLectores",
                cap => new BufferSemaforos(cap), () => new CacheSemaforosLectores()),
            new VarianteRun("MonitoresJusta",
                cap => new BufferMonitores(cap), () => new CacheMonitoresJusta()),
            new VarianteRun("MonitoresPreferenciaLectores",
                cap => new BufferMonitores(cap), () => new CacheMonitoresLectores()),
        };
    }

    // Helper unico para lanzar corridas: el mismo camino que recorre un endpoint pero sin
    // HTTP, directo contra ConfigurationRunService. Asi los tests miden la sincronizacion
    // y no el aparato web de alrededor.
    public static class Corridas
    {
        public static Task<ResultadoEjecucion> Ejecutar(
            VarianteRun variante,
            int ordenes,
            int productores,
            int consumidores,
            int capacidadBuffer,
            int iteraciones,
            int lectores = 2,
            int intervaloMsDeLecturas = 10)
        {
            // Armo el servicio igual que lo arma el endpoint: cache y buffer RECIEN creados
            // por las fabricas de la variante, y el resto de la config como parametros.
            var service = new ConfigurationRunService(
                productores,
                consumidores,
                iteraciones,
                ordenes,
                lectores,
                variante.CrearCache(),
                variante.CrearBuffer(capacidadBuffer),
                intervaloMsDeLecturas);

            // Ejecutar() lanza los hilos de verdad (Thread, no Task), espera los Join de
            // productores y consumidores, y devuelve las completadas + el tiempo medido.
            return service.Ejecutar();
        }

        // EL indicador de correctitud de la propuesta: N ingresadas -> N
        // procesadas exactamente una vez, sin perdidas ni duplicados. Lo comparten
        // los tests correctitud, estres y deadlock: en los tres casos "terminar bien" es esto
        public static void VerificarExactamenteUnaVez(ResultadoEjecucion resultado, int n)
        {
            // COMPROBACION 1: no se PERDIO ninguna orden. Se pidieron N y la bolsa de
            // completadas tiene que tener exactamente N. Si un consumidor pisara una
            // orden de otro, o una orden quedara colgada en el buffer, aca da distinto
            Assert.Equal(n, resultado.Ordenes.Count);

            // COMPROBACION 2: sin perdidas NI DUPLICADOS, las dos a la vez. GeneradorIdsOrden
            // reparte los Ids 1..N de forma atomica, asi que los Ids de las completadas,
            // ordenados, tienen que ser EXACTAMENTE el rango 1..N:
            //  - si falta un Id -> se perdio una orden (la secuencia salta un numero)
            //  - si un Id aparece dos veces -> se proceso dos veces (y ademas faltaria otro,
            //    porque la cuenta total ya dio N en la comprobacion 1)
            Assert.Equal(Enumerable.Range(1, n),
                         resultado.Ordenes.Select(o => o.Id).OrderBy(id => id));

            // COMPROBACION 3: cada una de las N ordenes fue REALMENTE procesada.
            Assert.All(resultado.Ordenes, o =>
            {
                // Ninguna orden veneno se colo como orden real: las poison pills existen
                // solo para cortar a los consumidores y jamas deben llegar a la bolsa
                Assert.False(o.esFin);

                // El consumidor sello CompletadoEn al terminar de procesarla. Si esta en
                // null, la orden entro a la bolsa sin pasar por el procesamiento
                Assert.NotNull(o.CompletadoEn);

                // Y el computo simulado dejo su resultado: la orden no salteo el
                // SimularProcesamiento del consumidor
                Assert.NotNull(o.ValorCalculado);
            });
        }
    }
}
