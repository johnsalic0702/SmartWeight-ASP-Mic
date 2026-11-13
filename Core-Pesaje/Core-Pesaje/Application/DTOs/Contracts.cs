namespace CorePesaje.Application.DTOs;

public sealed class ControlPesoDTO
{
    public long controlpesoId { get; set; }
    public long empresaId { get; set; }
    public long sucursalId { get; set; }
    public long almacenId { get; set; }
    public long documentoId { get; set; }
    public long serieId { get; set; }
    public long clienteId { get; set; }
    public string? numero { get; set; }
    public DateTime? fecha { get; set; }
    public int tipoCliente { get; set; }
    public string? cliente { get; set; }
    public string? glosa { get; set; }
    public string? ptoPartida { get; set; }
    public string? ptoLlegada { get; set; }
    public long transportistaId { get; set; }
    public long vehiculoId { get; set; }
    public long conductorId { get; set; }
    public int estado { get; set; }
    public long usuarioId { get; set; }
    public long pedidoId { get; set; }
    public string? horaLlegada { get; set; }
    public string? horaPartida { get; set; }
    public long sectorId { get; set; }
    public int afectaMerma { get; set; }
    public long guiaRecepcionId { get; set; }
    public long pesajeId { get; set; }
    public string? identidad { get; set; }
    public string? telefono { get; set; }
    public double? cantEnvase { get; set; }
    public double? unidades { get; set; }
    public double? pesoTara { get; set; }
    public double? pesoBruto { get; set; }
    public double? taraCliente { get; set; }
    public double? brutoCliente { get; set; }
    public double? pesoNeto { get; set; }
    public double? promedio { get; set; }
    public List<ControlPesoDetalleDTO>? detalles { get; set; }
}

public sealed class ControlPesoDetalleDTO
{
    public long detalleId { get; set; }
    public long controlpesoId { get; set; }
    public long productoId { get; set; }
    public string? producto { get; set; }
    public long envaseId { get; set; }
    public string? envase { get; set; }
    public int item { get; set; }
    public double? cantEnvase { get; set; }
    public double? unidades { get; set; }
    public double? precio { get; set; }
    public double? pesoTara { get; set; }
    public double? pesoBruto { get; set; }
    public double? taraCliente { get; set; }
    public double? brutoCliente { get; set; }
    public double? pesoNeto { get; set; }
    public double? promedio { get; set; }
    public string? sexo { get; set; }
    public int tipoTara { get; set; }
    public int tipoBruto { get; set; }
    public DateTime? horaTara { get; set; }
    public DateTime? horaBruto { get; set; }
    public int despachoStock { get; set; }
    public string? dispositivo { get; set; }
    public long usuarioId { get; set; }
}

public sealed class GuiaDTO
{
    public long guiaId { get; set; }
    public long sucursalId { get; set; }
    public long documentoId { get; set; }
    public long serieId { get; set; }
    public string? numero { get; set; }
    public long almacenId { get; set; }
    public long clienteId { get; set; }
    public string? cliente { get; set; }
    public double diaCredito { get; set; }
    public DateTime? fecha { get; set; }
    public long movimientoId { get; set; }
    public string? glosa { get; set; }
    public long monedaId { get; set; }
    public double tipoCambio { get; set; }
    public long vendedorId { get; set; }
    public string? ptoPartida { get; set; }
    public string? ptoLlegada { get; set; }
    public long transportistaId { get; set; }
    public long vehiculoId { get; set; }
    public long conductorId { get; set; }
    public long motivoId { get; set; }
    public string? motivo { get; set; }
    public double costoTraslado { get; set; }
    public int estado { get; set; }
    public string? nomEstado { get; set; }
    public long usuarioId { get; set; }
    public double pesoTara { get; set; }
    public double pesoBruto { get; set; }
    public long empresaId { get; set; }
    public double unidades { get; set; }
    public double cantidad { get; set; }
    public double promedio { get; set; }
    public List<GuiaDetalleDTO>? detalle { get; set; }
}

public sealed class GuiaDetalleDTO
{
    public long detalleId { get; set; }
    public long guiaId { get; set; }
    public int item { get; set; }
    public long productoId { get; set; }
    public double unidades { get; set; }
    public double cantidad { get; set; }
}

public sealed class PedidoDTO
{
    public long pedidoId { get; set; }
    public long clienteId { get; set; }
    public DateTime? fecha { get; set; }
    public int orden { get; set; }
    public double? cantidad { get; set; }
    public int estado { get; set; }
    public string? obs { get; set; }
    public long usuarioId { get; set; }
    public List<PedidoDetalleDTO>? detalle { get; set; }
}

public sealed class PedidoDetalleDTO
{
    public long detalleId { get; set; }
    public long pedidoId { get; set; }
    public long clienteId { get; set; }
    public long productoId { get; set; }
    public double? cantidadPedida { get; set; }
    public double? cantidadAtendida { get; set; }
    public string? sexo { get; set; }
}

public sealed class TicketControlPesoDTO
{
    public string? numero { get; set; }
    public string? fecha { get; set; }
    public string? cliente { get; set; }
    public string? llegada { get; set; }
    public string? usuario { get; set; }
    public string? token { get; set; }
    public string? qrUrl { get; set; }
    public List<TicketDetalleDTO>? detalles { get; set; }
}

public sealed class TicketDetalleDTO
{
    public string? producto { get; set; }
    public int? envase { get; set; }
    public int? cantidad { get; set; }
    public decimal? tara { get; set; }
    public decimal? bruto { get; set; }
    public decimal? neto { get; set; }
    public decimal? promedio { get; set; }
}

public sealed record ComboGuiaProjection(long? id, string? nombre);
