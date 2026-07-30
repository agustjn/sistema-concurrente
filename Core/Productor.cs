using Microsoft.Extensions.Diagnostics.HealthChecks;
using SistemaConcurrente.Core.Buffer;
using SistemaConcurrente.Core.Cache;
using SistemaConcurrente.Core.Coordinadores;
using System.Security.Cryptography;

namespace SistemaConcurrente.Core
{
    public class Productor
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly IBuffer _buffer;

        // Cache compartida: el productor actua como ESCRITOR, registrando cada orden generada.
        private readonly ICache _cache;

        public int CantIteraciones { get; }

        private ContadorOrdenes _contadorOrdenes;

        // Generador de Id compartido por TODOS los productores de la corrida. Es el que garantiza
        // que cada orden tenga un Id unico
        private readonly GeneradorIdsOrden _generadorIds;


        public Productor(int id,string name, IBuffer buffer, int cantIteraciones, ContadorOrdenes contadorOrdenes, ICache cache, GeneradorIdsOrden generadorIds)
        {

            Id = id;
            Name = name;
            _buffer = buffer;
            CantIteraciones = cantIteraciones;
            _contadorOrdenes = contadorOrdenes;
            _cache = cache;
            _generadorIds = generadorIds;

        }

        public void GenerarOrdenes()
        {
            while (true) {
                if (!_contadorOrdenes.DescontarOrden())
                    { break; }
                // El Id sale del generador atomico compartido: es unico en toda la corrida.
                Orden orden = new Orden(_generadorIds.ProximoId(), _buffer.EstrategiaBuffer);
                var resultado = this.SimularProcesamiento(orden.Monto);
                orden.ValorCalculado = resultado;
                // ESCRITURA en cache: la orden queda registrada como "Generada".
                _cache.Escribir(orden.Id, EstadoOrden.Generada);
                _buffer.DepositarDato(orden);
                
            }


        }

        public double SimularProcesamiento(double monto)
        {
            double x = monto;
            for (int i = 1; i <= CantIteraciones; i++)
                x = Math.Sqrt(x * x + Math.Sin(i)) + Math.Cos(x);
            return x;
        }









    }
}
