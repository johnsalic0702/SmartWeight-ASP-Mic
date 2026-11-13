var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<Maestros.Repositories.IListMaestroRepository, Maestros.Repositories.ListMaestroRepository>();
builder.Services.AddScoped<Maestros.Services.IListMaestroService, Maestros.Services.ListMaestroService>();

var app = builder.Build();

app.UsePathBase("/api");
app.MapControllers();

app.Run();
