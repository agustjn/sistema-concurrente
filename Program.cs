using Microsoft.EntityFrameworkCore;
using SistemaConcurrente.Core.Persistencia;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Persistencia SQL Server (secc. 4 y 6): DbContext + servicio de guardado.
// Scoped: una instancia por request, que es exactamente el ciclo de vida de una corrida.
builder.Services.AddDbContext<OrdenesDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("SistemaConcurrente")));
builder.Services.AddScoped<PersistenciaOrdenes>();

var app = builder.Build();

// Crea la base y la tabla Ordenes si no existen (EnsureCreated: suficiente para el
// TP, sin el aparato de migrations).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<OrdenesDbContext>().Database.EnsureCreated();
}

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
