namespace SistemaConcurrente.Core.Buffer
{
    public interface IBufferGestion
    {
        public string EstrategiaBuffer { get; }
        public Orden DepositarDato(Orden orden);

        public Orden RetirarDato();

    }
}
