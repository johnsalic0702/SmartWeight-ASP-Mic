using System.Globalization;
using System.Text;
using CorePesaje.Application.DTOs;
using iText.Barcodes;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace CorePesaje.Infrastructure.Reports;

public sealed class PdfTicketService
{
    public byte[] Generate(TicketControlPesoDTO ticket)
    {
        var details = ticket.detalles ?? [];
        var groups = details.GroupBy(d => string.IsNullOrWhiteSpace(d.producto) ? "Producto  Sin Nombre" : d.producto.Trim())
            .ToList();
        var height = 400f + details.Count * 25f + groups.Count * 50f;
        using var stream = new MemoryStream();
        using var writer = new PdfWriter(stream);
        using var pdf = new PdfDocument(writer);
        using (var doc = new Document(pdf, new PageSize(226f, height)))
        {
            doc.SetMargins(10f, 10f, 10f, 10f);
            const float font = 8f;
            doc.Add(new Paragraph("CONTROL PESO N° " + (ticket.numero ?? ""))
                .SetBold().SetTextAlignment(TextAlignment.CENTER).SetFontSize(font + 1.5f).SetMarginBottom(6f));
            AddLabel(doc, "Fecha: ", ticket.fecha, font);
            AddLabel(doc, "Cliente: ", ticket.cliente, font);
            AddLabel(doc, "Llegada: ", ticket.llegada, font);
            AddLabel(doc, "Pesador: ", ticket.usuario, font);
            doc.Add(new Paragraph("").SetMarginBottom(5f));

            var table = new Table(UnitValue.CreatePercentArray(new float[] { 14, 14, 18, 18, 18, 18 })).UseAllAvailableWidth();
            foreach (var title in new[] { "Jaba", "Cant", "Tara", "Bruto", "Neto", "Prom" })
                table.AddHeaderCell(Header(title, font));
            foreach (var group in groups)
            {
                table.AddCell(new Cell(1, 6).Add(new Paragraph("PROD: " + group.Key).SetBold().SetFontSize(font))
                    .SetBorder(Border.NO_BORDER).SetPaddingTop(4).SetPaddingBottom(2));
                var envases = 0;
                var cantidades = 0;
                var tara = 0m; var bruto = 0m; var neto = 0m;
                foreach (var d in group)
                {
                    var envase = d.envase ?? 0; var cantidad = d.cantidad ?? 0;
                    var itemTara = d.tara ?? 0; var itemBruto = d.bruto ?? 0; var itemNeto = d.neto ?? 0;
                    var promedio = cantidad > 0 ? Math.Round(itemNeto / cantidad, 2, MidpointRounding.AwayFromZero) : 0;
                    foreach (var value in new[] { envase.ToString(CultureInfo.InvariantCulture), cantidad.ToString(CultureInfo.InvariantCulture),
                                 itemTara.ToString("F2", CultureInfo.InvariantCulture), itemBruto.ToString("F2", CultureInfo.InvariantCulture),
                                 itemNeto.ToString("F2", CultureInfo.InvariantCulture), promedio.ToString("F2", CultureInfo.InvariantCulture) })
                        table.AddCell(Data(value, font));
                    envases += envase; cantidades += cantidad; tara += itemTara; bruto += itemBruto; neto += itemNeto;
                }
                var average = cantidades > 0 ? Math.Round(neto / cantidades, 2, MidpointRounding.AwayFromZero) : 0;
                foreach (var value in new[] { "T  " + envases, cantidades.ToString(CultureInfo.InvariantCulture),
                             tara.ToString("F2", CultureInfo.InvariantCulture), bruto.ToString("F2", CultureInfo.InvariantCulture),
                             neto.ToString("F2", CultureInfo.InvariantCulture), average.ToString("F2", CultureInfo.InvariantCulture) })
                    table.AddCell(Total(value, font));
            }
            doc.Add(table);
            doc.Add(new Paragraph("").SetMarginBottom(15f));
            try
            {
                if (!string.IsNullOrEmpty(ticket.qrUrl))
                {
                    var qr = new BarcodeQRCode(ticket.qrUrl);
                    var image = new Image(qr.CreateFormXObject(ColorConstants.BLACK, pdf)).SetWidth(80).SetHeight(80)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER).SetMarginBottom(10);
                    doc.Add(image);
                }
            }
            catch { doc.Add(new Paragraph("[Error al cargar QR]").SetFontSize(font).SetTextAlignment(TextAlignment.CENTER)); }
            doc.Add(new Paragraph("Documento válido como constancia interna\n\nGRACIAS, por su preferencia.")
                .SetTextAlignment(TextAlignment.CENTER).SetFontSize(font - .5f).SetMultipliedLeading(1.2f));
        }
        return stream.ToArray();
    }

    private static void AddLabel(Document doc, string label, string? value, float size) =>
        doc.Add(new Paragraph().SetFontSize(size).SetMarginBottom(1).SetMarginTop(1)
            .Add(new Text(label).SetBold()).Add(new Text(value ?? "")));
    private static Cell Header(string value, float size) => new Cell().Add(new Paragraph(value).SetBold()
        .SetFontSize(size).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(Border.NO_BORDER)
        .SetPaddingBottom(3).SetBorderBottom(new SolidBorder(.75f));
    private static Cell Data(string value, float size) => new Cell().Add(new Paragraph(value)
        .SetFontSize(size).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(Border.NO_BORDER)
        .SetPaddingTop(2).SetPaddingBottom(2);
    private static Cell Total(string value, float size) => new Cell().Add(new Paragraph(value).SetBold()
        .SetFontSize(size).SetTextAlignment(TextAlignment.RIGHT)).SetBorder(Border.NO_BORDER)
        .SetPaddingTop(4).SetPaddingBottom(4).SetBorderTop(new SolidBorder(.75f)).SetBorderBottom(new SolidBorder(.75f));
}
