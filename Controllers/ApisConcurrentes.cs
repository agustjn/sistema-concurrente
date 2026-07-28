using Microsoft.AspNetCore.Mvc;
using SistemaConcurrente.Core;
using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Cache.CacheMonitores;
using SistemaConcurrente.Core.Cache.CacheSemaforos;
using SistemaConcurrente.Core.Coordinadores;
using SistemaConcurrente.DTOs;
using SistemaConcurrente.Utils;

namespace SistemaConcurrente.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApisConcurrentes : ControllerBase
    {
        

        public ApisConcurrentes()
        {

        }

        [HttpPost("proceso-secuencial")]
        public ApisResponse ProcesoSecuencial([FromBody] ConfiguracionRun parametros)
        {
            ProcesadorSecuencial procesador = new ProcesadorSecuencial(parametros.CantOrdenes, parametros.CantIteraciones);

            ResultadoEjecucion resultado = procesador.Ejecutar();

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "Secuencial");
        }

        [HttpPost("semaforos-cache-justa")]
        public async Task<ApisResponse> SemaforosCacheJusta([FromBody] ConfiguracionRun parametros)
        {
            ICache cache = new CacheSemaforosJusta();
            IBuffer buffer = new BufferSemaforos(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.intervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "SemaforosJusta");
        }

        // Esta API utiliza semaforos:
        // El buffer limitado se gestiona a traves de semaforos contadores para la sincronizacion por condicion y semaforos binarios (mutex) para la exclusion mutua
        [HttpPost("semaforos-cache-sin-prioridad")]
        public async Task<ApisResponse> SemaforosCacheSinPrioridad([FromBody] ConfiguracionRun parametros)
        {
            ICache cache = new CacheSemaforos();
            IBuffer buffer = new BufferSemaforos(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.intervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "SemaforosSinPrioridad");
        }

        // Esta API utiliza monitores, en el buffer limitado la disciplina signal-and-continue y en la cache el acceso concurrente de lectores sin prioridad a escritores
        [HttpPost("monitores-cache-sin-prioridad")]
        public async Task<ApisResponse> MonitoresCacheSinPrioridad([FromBody] ConfiguracionRun parametros)
        {
            ICache cache = new CacheMonitoresLectores();
            IBuffer buffer = new BufferMonitores(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.intervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "MonitoresSinPrioridad");
        }

        // Esta API utiliza monitores en buffer y cache; la cache aplica la politica justa (los lectores ceden si hay escritores demorados)
        [HttpPost("monitores-cache-justa")]
        public async Task<ApisResponse> MonitoresCacheJusta([FromBody] ConfiguracionRun parametros)
        {
            ICache cache = new CacheMonitoresJusta();
            IBuffer buffer = new BufferMonitores(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.intervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "MonitoresJusta");
        }








    }
}
