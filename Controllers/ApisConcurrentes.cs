using Microsoft.AspNetCore.Mvc;
using SistemaConcurrente.Core.Coordinadores;
using SistemaConcurrente.DTOs;
using System.Net.Http.Headers;

namespace SistemaConcurrente.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApisConcurrentes : ControllerBase
    {
        

        //private readonly ILogger<WeatherForecastController> _logger;

        public ApisConcurrentes()
        {
            
        }

        [HttpPost("semaforos-cache-justa")]
        public Task<ApisResponse> SemaforosCacheJusta([FromBody] ConfiguracionRun parametros)
        {
            
        }
    }
}
