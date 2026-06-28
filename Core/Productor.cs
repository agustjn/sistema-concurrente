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


        public Productor(int id,string name, IBuffer buffer, int cantIteraciones, ContadorOrdenes contadorOrdenes, ICache cache)
        {

            Id = id;
            Name = name;
            _buffer = buffer;
            CantIteraciones = cantIteraciones;
            _contadorOrdenes = contadorOrdenes;
            _cache = cache;

        }

        public void GenerarOrdenes()
        {
            while (true) {
                if (!_contadorOrdenes.DescontarOrden()) 
                    { break; }                   
                Random rand = new Random();
                Orden orden = new Orden(rand.Next(1,100000), _buffer.EstrategiaBuffer);
                var resultado = this.SimularProcesamiento(orden.Monto);
                orden.ValorCalculado = resultado;
                // ESCRITURA en cache: la orden queda registrada como "Generada".
                _cache.Escribir(orden.Id, EstadoOrden.Generada);
                _buffer.DepositarDato(orden);
                Console.WriteLine("Depositada en el buffer orden #" + orden.Id + " / Productor #" + Id.ToString());
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
