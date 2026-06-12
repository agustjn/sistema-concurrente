using SistemaConcurrente.Core.Buffer;

namespace SistemaConcurrente.Core
{
    public class Consumidor
    {
        public int Id { get; set; }
        public string Name { get; set; }

        private readonly IBuffer _buffer;

        public int CantIteraciones { get; }


        public Consumidor(int id,string name, IBuffer buffer, int cantIteraciones)
        {
            
            Id = id;
            Name = name;
            _buffer = buffer;
            CantIteraciones = cantIteraciones;
        }

        public void ProcesarOrden()
        {
            while (true) 
            {
                Orden orden = _buffer.RetirarDato();
                orden.ValorCalculado = this.SimularProcesamiento(orden.Monto);
                orden.CompletadoEn = DateTime.Now;
                // actualizar cache
               
            }
            
        }

        private double SimularProcesamiento(double monto)
        {
            double x = monto;
            for (int i = 1; i <= CantIteraciones; i++)
                x = Math.Sqrt(x * x + Math.Sin(i)) + Math.Cos(x);
            return x;
        }

    }
}
