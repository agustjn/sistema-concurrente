using Microsoft.Extensions.Diagnostics.HealthChecks;
using SistemaConcurrente.Core.Buffer;

namespace SistemaConcurrente.Core
{
    public class Productor
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly IBufferGestion _buffer;

        public int CantIteraciones { get; }

        public static int OrdenIdIncremental { get; set; } = 1;

        public Productor(int id, string name, IBufferGestion buffer, int cantIteraciones)
        {
            Id = id;
            Name = name;
            _buffer = buffer;
            CantIteraciones = cantIteraciones;
        }

        public Orden GenerarOrden()
        {
            var resultado = this.SimularProcesamiento();
            Orden orden = new Orden(OrdenIdIncremental, _buffer.EstrategiaBuffer);
            orden.ValorCalculado = this.SimularProcesamiento();
            _buffer.DepositarDato(orden);
            return orden;
        }

        public double SimularProcesamiento()
        {
            double x = 1.0;
            for (int i = 1; i <= CantIteraciones; i++)
                x = Math.Sqrt(x * x + Math.Sin(i)) + Math.Cos(x);
            return x;
        }





    }
}
