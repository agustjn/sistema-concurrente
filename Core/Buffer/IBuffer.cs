namespace SistemaConcurrente.Core.Buffer
{
    public interface IBuffer
    {
        public string EstrategiaBuffer { get; }
        public Orden DepositarDato(Orden orden);

        public Orden RetirarDato();

    }
}
