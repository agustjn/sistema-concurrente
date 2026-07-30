using Microsoft.EntityFrameworkCore;

namespace SistemaConcurrente.Core.Persistencia
{

    public class PersistenciaOrdenes
    {
        private readonly OrdenesDbContext _db;

        public PersistenciaOrdenes(OrdenesDbContext db)
        {
            _db = db;
        }

        // Al inicio de cada endpoint borra todo lo de la corrida anterior.
        public void Limpiar()
        {
            _db.Ordenes.ExecuteDelete();
        }

        // Al final de cada endpoint persiste todas las ordenes completadas
        public void GuardarCorrida(IReadOnlyList<Orden> ordenes, string tecnica)
        {
            _db.Ordenes.AddRange(ordenes.Select(o => new OrdenPersistencia
            {
                OrdenId = o.Id,
                Tecnica = tecnica,
                Monto = o.Monto,
                ValorCalculado = o.ValorCalculado,
                CreadoEn = o.CreadoEn,
                InsertadoEnBufferEn = o.InsertadoEnBufferEn,
                RetiradoDeBufferEn = o.RetiradoDeBufferEn,
                CompletadoEn = o.CompletadoEn,
                LatenciaMs = o.LatenciaMs
            }));

            _db.SaveChanges();
        }
    }
}
