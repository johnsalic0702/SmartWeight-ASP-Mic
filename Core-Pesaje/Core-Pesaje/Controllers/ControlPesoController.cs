using CorePesaje.Application.DTOs;
using CorePesaje.Application.Services;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CorePesaje.Controllers;

[ApiController]
[Route("api/controlpeso")]
public sealed class ControlPesoController(ControlPesoService service, TicketService tickets) : ControllerBase
{
    private static readonly JsonSerializerOptions SaveJsonOptions = CreateSaveJsonOptions();

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] JsonElement body, CancellationToken ct)
    {
        ControlPesoDTO? dto;
        try { dto = body.Deserialize<ControlPesoDTO>(SaveJsonOptions); }
        catch (JsonException e) { return BadRequest(new ProblemDetails { Title = "JSON inválido", Detail = e.Message, Status = 400 }); }
        if (dto is null) return BadRequest(new ProblemDetails { Title = "El campo dto es obligatorio", Status = 400 });
        if (!TryValidateModel(dto)) return ValidationProblem(ModelState);
        try { return Ok(await service.SaveAsync(dto, ct)); }
        catch (Exception e) { return NotFound("Problemas al Registrar el Control de Peso: " + e.Message); }
    }

    private static JsonSerializerOptions CreateSaveJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null
        };
        options.Converters.Add(new ControlPesoDateTimeConverter());
        return options;
    }

    private sealed class ControlPesoDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly string[] DateTimeFormats =
        [
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF"
        ];

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("Los campos de fecha/hora deben ser cadenas con formato yyyy-MM-dd o yyyy-MM-ddTHH:mm:ss[.fffffff].");

            var value = reader.GetString();
            if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date.ToDateTime(TimeOnly.MinValue);
            if (DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified);

            throw new JsonException("El campo de fecha/hora debe tener formato yyyy-MM-dd o yyyy-MM-ddTHH:mm:ss[.fffffff].");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
    }

    [HttpGet("listporid/id/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        try { return new JsonResult(await service.GetByIdAsync(id, ct)) { StatusCode = 200 }; }
        catch (Exception e) { return NotFound(e.Message); }
    }

    [HttpGet("listporfecha/empresa/{empresaId:long}/sucursal/{sucursalId:long}/almacen/{almacenId:long}/fecha/{fecha}/estado/{estado:int}")]
    public async Task<IActionResult> GetByDate(long empresaId, long sucursalId, long almacenId, string fecha, int estado, CancellationToken ct)
    {
        try
        {
            var rows = await service.GetByDateAsync(empresaId, sucursalId, almacenId, fecha, estado, ct);
            return rows.Count == 0 ? NotFound("Service: No se encontró por FECHA") : Ok(rows);
        }
        catch (Exception e) { return NotFound("Service: " + e.Message); }
    }

    [HttpPut("estado")]
    public async Task<IActionResult> UpdateState([FromQuery] long id, [FromQuery] int estado, [FromQuery] long usuarioId, CancellationToken ct)
    {
        try { await service.UpdateStateAsync(id, estado, usuarioId, ct); return Ok(new { success = true, mensaje = "Estado actualizado correctamente" }); }
        catch (Exception e) { return StatusCode(500, "Error al actualizar estado: " + e.Message); }
    }

    [HttpGet("combo/emp/{empresaId:long}/suc/{sucursalId:long}/alm/{almacenId:long}/fec/{fecha}")]
    public async Task<IActionResult> Combo(long empresaId, long sucursalId, long almacenId, string fecha, CancellationToken ct)
    {
        try { return Ok(await service.GetComboAsync(empresaId, sucursalId, almacenId, fecha, ct)); }
        catch (Exception e) { return StatusCode(500, "Error: " + e.Message); }
    }

    [HttpGet("{id:long}/ticketpdf")]
    public async Task<IActionResult> TicketPdf(long id, CancellationToken ct) =>
        File(await tickets.GetPdfAsync(id, ct), "application/pdf");

    [HttpGet("{id:long}/ticketid")]
    public async Task<IActionResult> Ticket(long id, CancellationToken ct)
    {
        try { return Ok(await tickets.GetTicketAsync(id, ct)); }
        catch (Exception e) { return StatusCode(500, "Error: " + e.Message); }
    }
}
