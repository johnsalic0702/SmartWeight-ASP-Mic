using CorePesaje.Application.Services;
using CorePesaje.Infrastructure.Data.Repositories;
using CorePesaje.Infrastructure.Reports;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://0.0.0.0:8082");
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.DictionaryKeyPolicy = null;
});
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<SqlProcedureRepository>();
builder.Services.AddScoped<ControlPesoService>();
builder.Services.AddScoped<GuiaService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<PdfTicketService>();

var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers();
app.Run();

public partial class Program;
