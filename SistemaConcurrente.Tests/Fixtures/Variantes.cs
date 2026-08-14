using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Cache.CacheMonitores;
using SistemaConcurrente.Core.Cache.CacheSemaforos;
using SistemaConcurrente.Core.Coordinadores;
using Xunit;

namespace SistemaConcurrente.Tests.Fixtures
{
    // Una combinacion de buffer y cache, la misma que arma cada endpoint.
    // Se pasan fabricas (Func) y no instancias, ya que cada corrida necesita buffer y cache
    // nuevos porque ambos guardan estado y se contaminarian entre corridas.
    public record VarianteRun(string Tecnica, Func<int, IBuffer> CrearBuffer, Func<ICache> CrearCache)
    {
        // Para que el runner muestre el nombre de la variante en cada caso del Theory.
        public override string ToString() => Tecnica;
    }

    public static class Variantes
    {
        // Las 4 combinaciones de la API. Como los actores dependen solo de IBuffer e ICache,
        // el mismo test corre contra todas sin cambiar nada.
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

    // Lanza una corrida por el mismo camino que un endpoint pero sin HTTP, directo contra
    // ConfigurationRunService.
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
            // Armo el servicio igual que el endpoint, con cache y buffer recien creados.
            var service = new ConfigurationRunService(
                productores,
                consumidores,
                iteraciones,
                ordenes,
                lectores,
                variante.CrearCache(),
                variante.CrearBuffer(capacidadBuffer),
                intervaloMsDeLecturas);

            // Ejecutar() lanza los hilos, espera los Join y devuelve las completadas y el tiempo.
            return service.Ejecutar();
        }

        // Verificacion de correctitud compartida por los tests de correctitud, estres y deadlock:
        // las N ordenes ingresadas tienen que salir procesadas exactamente una vez.
        public static void VerificarExactamenteUnaVez(ResultadoEjecucion resultado, int n)
        {
            // No se perdio ninguna orden: se pidieron N y tienen que haber N completadas.
            Assert.Equal(n, resultado.Ordenes.Count);

            // Sin perdidas ni duplicados: como GeneradorIdsOrden reparte los Ids 1..N de forma
            // atomica, los Ids de las completadas ordenados tienen que dar exactamente ese rango.
            Assert.Equal(Enumerable.Range(1, n),
                         resultado.Ordenes.Select(o => o.Id).OrderBy(id => id));

            // Cada orden fue realmente procesada.
            Assert.All(resultado.Ordenes, o =>
            {
                // Ninguna orden veneno llego a la bolsa de completadas.
                Assert.False(o.esFin);

                // El consumidor sello CompletadoEn al terminar de procesarla.
                Assert.NotNull(o.CompletadoEn);

                // Y quedo el resultado del computo simulado.
                Assert.NotNull(o.ValorCalculado);
            });
        }
    }
}
