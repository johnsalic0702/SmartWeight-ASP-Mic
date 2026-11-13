using CorePesaje.Application.DTOs;
using CorePesaje.Application.Services;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CorePesaje.Controllers;

[ApiController]
[Route("api/guias")]
public sealed class GuiaController(GuiaService service) : ControllerBase
{
    private static readonly JsonSerializerOptions SaveJsonOptions = CreateSaveJsonOptions();

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] JsonElement body, CancellationToken ct)
    {
        GuiaDTO? dto;
        try { dto = body.Deserialize<GuiaDTO>(SaveJsonOptions); }
        catch (JsonException e) { return BadRequest(new ProblemDetails { Title = "JSON inválido", Detail = e.Message, Status = 400 }); }
        if (dto is null) return BadRequest(new ProblemDetails { Title = "El campo dto es obligatorio", Status = 400 });
        if (!TryValidateModel(dto)) return ValidationProblem(ModelState);
        try { return Ok(await service.SaveAsync(dto, ct)); }
        catch (Exception e) { return NotFound("Problemas al Registrar la Guia: " + e.Message); }
    }

    private static JsonSerializerOptions CreateSaveJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null
        };
        options.Converters.Add(new GuiaDateTimeConverter());
        return options;
    }

    private sealed class GuiaDateTimeConverter : JsonConverter<DateTime>
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

    [HttpGet("id/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        try { return Content(await service.GetJsonAsync(1, id, null, 1, ct), "application/json"); }
        catch (Exception e) { return StatusCode(500, "Error: " + e.Message); }
    }

    [HttpGet("fecha")]
    public async Task<IActionResult> GetByDate([FromQuery] string fecha, [FromQuery] int estado, CancellationToken ct)
    {
        try { return Content(await service.GetJsonAsync(2, 0, fecha, estado, ct), "application/json"); }
        catch (Exception e) { return StatusCode(500, "Error: " + e.Message); }
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
}
