 using System.Globalization;
using CorePesaje.Application.DTOs;
using CorePesaje.Infrastructure.Data.Repositories;
using CorePesaje.Infrastructure.Reports;

namespace CorePesaje.Application.Services;

public sealed class TicketService(SqlProcedureRepository repository, PdfTicketService pdf)
{
    public async Task<TicketControlPesoDTO> GetTicketAsync(long id, CancellationToken ct)
    {
        var result = await repository.TicketAsync(id, ct);
        var rows = result.resultSets.SelectMany(s => s.rows).ToList();
        var ticket = new TicketControlPesoDTO { detalles = [] };
        if (rows.Count == 0) return ticket;
        var first = rows[0];
        ticket.numero = Text(first, "Numero");
        ticket.fecha = TicketDate(first, "Fecha");
        ticket.cliente = Text(first, "Cliente");
        ticket.llegada = Text(first, "NomLlegada");
        ticket.usuario = Text(first, "Usuario");
        ticket.token = Text(first, "Token");
        ticket.qrUrl = Text(first, "qrUrl");
        ticket.detalles = rows.Select(r => new TicketDetalleDTO
        {
            producto = Text(r, "Producto"),
            envase = Int(r, "Envase"),
            cantidad = Int(r, "Cantidad"),
            tara = Decimal(r, "Tara"),
            bruto = Decimal(r, "Bruto"),
            neto = Decimal(r, "Neto"),
            promedio = Decimal(r, "Promedio")
        }).ToList();
        return ticket;
    }

    public async Task<byte[]> GetPdfAsync(long id, CancellationToken ct) => pdf.Generate(await GetTicketAsync(id, ct));

    private static object? Get(Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var value) ? value : null;
    private static string? Text(Dictionary<string, object?> row, string name) => Get(row, name) is { } v
        ? Convert.ToString(v, CultureInfo.InvariantCulture) : null;
    private static string? TicketDate(Dictionary<string, object?> row, string name) => Get(row, name) switch
    {
        null => null,
        DateOnly value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTimeOffset value => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        var value => Convert.ToString(value, CultureInfo.InvariantCulture)
    };
    private static int? Int(Dictionary<string, object?> row, string name) => Get(row, name) is null
        ? 0 : Convert.ToInt32(Get(row, name), CultureInfo.InvariantCulture);
    private static decimal? Decimal(Dictionary<string, object?> row, string name) => Get(row, name) is { } v
        ? Convert.ToDecimal(v, CultureInfo.InvariantCulture) : null;
}
