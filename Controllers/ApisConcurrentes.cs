using Microsoft.AspNetCore.Mvc;
using SistemaConcurrente.Core;
using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Cache.CacheMonitores;
using SistemaConcurrente.Core.Cache.CacheSemaforos;
using SistemaConcurrente.Core.Coordinadores;
using SistemaConcurrente.Core.Persistencia;
using SistemaConcurrente.DTOs;
using SistemaConcurrente.Utils;

namespace SistemaConcurrente.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApisConcurrentes : ControllerBase
    {
        private readonly PersistenciaOrdenes _persistencia;
        public ApisConcurrentes(PersistenciaOrdenes persistencia)
        {
            _persistencia = persistencia;
        }

        [HttpPost("proceso-secuencial")]
        public ApisResponse ProcesoSecuencial([FromBody] ConfiguracionSecuencial parametros)
        {
            _persistencia.Limpiar();

            ProcesadorSecuencial procesador = new ProcesadorSecuencial(parametros.CantOrdenes, parametros.CantIteraciones);

            ResultadoEjecucion resultado = procesador.Ejecutar();

            _persistencia.GuardarCorrida(resultado.Ordenes, "Secuencial");

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "Secuencial");
        }

        [HttpPost("semaforos-cache-justa")]
        public async Task<ApisResponse> SemaforosCacheJusta([FromBody] ConfiguracionRun parametros)
        {
            _persistencia.Limpiar();

            ICache cache = new CacheSemaforosJusta();
            IBuffer buffer = new BufferSemaforos(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.IntervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            _persistencia.GuardarCorrida(resultado.Ordenes, "SemaforosJusta");

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "SemaforosJusta");
        }

        // Esta API utiliza semaforos:
        // El buffer limitado se gestiona a traves de semaforos contadores para la sincronizacion por condicion y semaforos binarios (mutex) para la exclusion mutua.
        // La cache aplica la politica de PREFERENCIA A LECTORES: lectores concurrentes entre si, aunque eso pueda postergar a los escritores
        [HttpPost("semaforos-cache-preferencia-lectores")]
        public async Task<ApisResponse> SemaforosCachePreferenciaLectores([FromBody] ConfiguracionRun parametros)
        {
            _persistencia.Limpiar();

            ICache cache = new CacheSemaforosLectores();
            IBuffer buffer = new BufferSemaforos(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.IntervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            _persistencia.GuardarCorrida(resultado.Ordenes, "SemaforosPreferenciaLectores");

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "SemaforosPreferenciaLectores");
        }

        // Esta API utiliza monitores, en el buffer limitado la disciplina signal-and-continue y en la cache la politica de PREFERENCIA A LECTORES:
        // los lectores entran concurrentes entre si mientras no haya un escritor adentro, aunque eso pueda postergar a los escritores
        [HttpPost("monitores-cache-preferencia-lectores")]
        public async Task<ApisResponse> MonitoresCachePreferenciaLectores([FromBody] ConfiguracionRun parametros)
        {
            _persistencia.Limpiar();

            ICache cache = new CacheMonitoresLectores();
            IBuffer buffer = new BufferMonitores(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.IntervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            _persistencia.GuardarCorrida(resultado.Ordenes, "MonitoresPreferenciaLectores");

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "MonitoresPreferenciaLectores");
        }

        // Esta API utiliza monitores en buffer y cache; la cache aplica la politica justa (los lectores ceden si hay escritores demorados)
        [HttpPost("monitores-cache-justa")]
        public async Task<ApisResponse> MonitoresCacheJusta([FromBody] ConfiguracionRun parametros)
        {
            _persistencia.Limpiar();

            ICache cache = new CacheMonitoresJusta();
            IBuffer buffer = new BufferMonitores(parametros.CapacidadBuffer);

            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores, cache, buffer, parametros.IntervaloMsDeLecturas);

            ResultadoEjecucion resultado = await service.Ejecutar();

            _persistencia.GuardarCorrida(resultado.Ordenes, "MonitoresJusta");

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "MonitoresJusta");
        }








    }
}
