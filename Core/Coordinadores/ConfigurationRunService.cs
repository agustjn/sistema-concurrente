using SistemaConcurrente.Core.Buffer;

namespace SistemaConcurrente.Core.Coordinadores
{
    public class ConfigurationRunService
    {
        private int CantProductores;
        private int CantConsumidores;
        private int TamanioBuffer;
        private int CantIteraciones;
        // Manera de manejar el indice/clave/id de cada productor/consumir, ya que en el generarConsumidor del Thread(...) no se puede enviar el indice como parametro
        public static int indiceGlobal = 0;
        private readonly BufferSemaforos Buffer;


        public ConfigurationRunService(int cantProductores, int cantConsumidores, int tamanioBuffer, int cantIteraciones) 
        { 
            CantConsumidores = cantConsumidores;
            CantProductores = cantProductores;
            TamanioBuffer = tamanioBuffer;
            CantIteraciones = cantIteraciones;
            Buffer = new BufferSemaforos(tamanioBuffer);
        }

        public async Task EjecutarSemaforos()
        {
            this.GenerarProductores();
            this.GenerarConsumidores();
        }

        public void GenerarProductores()
        {
            for (int i = 0; i <= CantProductores - 1; i++)
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    Productor productor = new Productor(i, "Prod #" + i.ToString(), Buffer, CantIteraciones);
                    productor.GenerarOrdenes();
                });
            }
        }

        public void GenerarConsumidores()
        {
            for (int i = 0; i <= CantProductores - 1; i++)
            {
                Thread nuevoHilo = new Thread(() =>
                {
                    Productor productor = new Productor(i, "Prod #" + i.ToString(), Buffer, CantIteraciones);
                    productor.GenerarOrdenes();
                });
            }
        }




    }
}
