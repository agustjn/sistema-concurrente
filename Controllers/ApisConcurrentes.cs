using Microsoft.AspNetCore.Mvc;
using SistemaConcurrente.Core.Coordinadores;
using SistemaConcurrente.DTOs;
using SistemaConcurrente.Utils;

namespace SistemaConcurrente.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApisConcurrentes : ControllerBase
    {
        // Identificador incremental de corrida (atomico para soportar requests concurrentes).
        private static int _runIdSeed = 0;

        public ApisConcurrentes()
        {

        }

        [HttpPost("semaforos-cache-justa")]
        public async Task<ApisResponse> SemaforosCacheJusta([FromBody] ConfiguracionRun parametros)
        {
            ConfigurationRunService service = new ConfigurationRunService(parametros.CantProductores, parametros.CantConsumidores, parametros.CapacidadBuffer, parametros.CantIteraciones, parametros.CantOrdenes, parametros.CantLectores);

            ResultadoEjecucion resultado = await service.EjecutarSemaforos();

            return CalculadoraMetricas.Calcular(resultado.Ordenes, resultado.TiempoTotalSegundos, "SemaforosJusta");
        }
    }
}
