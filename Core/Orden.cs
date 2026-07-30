using SistemaConcurrente.Core.Cache;
using System.Drawing;

namespace SistemaConcurrente.Core
{
    public class Orden
    {
        public int Id { get; init; }

        public string Descripcion { get; }

        // Valor generado random para luego simular el computo pesado
        public double Monto { get; init; }

        // Resultado del procesamiento numérico simulado

        public double? ValorCalculado { get; set; }

        // Momento en el que el productor genero la orden.
        public DateTime CreadoEn { get; }


        // Momento en el que el productor genero y inserto en el buffer (accediendo al recurso compartido)
        public DateTime? InsertadoEnBufferEn { get; set; }

        // RetiradoEn: Momento en el que el consumidor retiro la orden del buffer (accediendo al recurso compartido)
        public DateTime? RetiradoDeBufferEn { get; set; }

        // CompletadoEn: Momento en el que el consumidor finalizo el procesamiento de la orden.
        public DateTime? CompletadoEn { get; set; }

        // Latencia extremo a extremo: CreadoEn -> CompletadoEn
        public double? LatenciaMs { get; set; }

        public string TecnicaBuffer { get; set; }

        // El bool y el metodo estatico se utilizan para poder cortar los threads en los semaforos que quedan en _lleno.Wait(), los cuales una vez finalizadas toads las ordenes,
        // los productores generaran estan "Ordenes veneno" para poder finalizar los procesos consumidores.
        public bool esFin { get; init; }
        public static Orden PoisonPill() => new Orden(-1, "finEjecucion") { esFin = true };

        public Orden(int id, string estrategiaBuffer)
        {
            Id = id;
            TecnicaBuffer = estrategiaBuffer;
            Monto = Random.Shared.Next(1, 60001);
            CreadoEn = DateTime.Now;
        }

        public void calcularLatencia()
        {
            this.LatenciaMs = (CompletadoEn.Value - CreadoEn).TotalMilliseconds;
        }




        //public int? RunId { get; init; }
    }

    
}
