using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace POVISAPI.Services.Reports
{
    public static class PdfReportRenderer
    {
        private const string Navy = "#1B2A6B";
        private const string Band = "#E8ECF5";
        private const string Grid = "#B8C0D4";
        private static readonly CultureInfo Ph = CultureInfo.GetCultureInfo("en-PH");

        static PdfReportRenderer()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] Render(ReportDocument doc) => Build(doc).GeneratePdf();

        /// <summary>PNG pages — used to eyeball the layout in development.</summary>
        public static IEnumerable<byte[]> RenderImages(ReportDocument doc) =>
            Build(doc).GenerateImages(new ImageGenerationSettings { RasterDpi = 110 });

        private static Document Build(ReportDocument doc) => Document.Create(container =>
        {
            foreach (var sheet in doc.Sheets)
            {
                container.Page(page =>
                {
                    page.Size(sheet.Wide ? PageSizes.A3.Landscape() : PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(t => t.FontSize(sheet.Wide ? 7 : 8.5f));

                    page.Header().Element(h => Header(h, sheet));
                    page.Content().PaddingTop(10).Element(c => Content(c, sheet));
                    page.Footer().Row(r =>
                    {
                        r.RelativeItem().Text($"Generated {DateTime.UtcNow.AddHours(8):MMMM d, yyyy h:mm tt} (PHT)")
                            .FontSize(7).FontColor(Colors.Grey.Darken1);
                        r.RelativeItem().AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));
                            t.Span("Page "); t.CurrentPageNumber(); t.Span(" of "); t.TotalPages();
                        });
                    });
                });
            }
        });

        private static void Header(IContainer c, ReportSheet sheet)
        {
            c.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(sheet.Wide ? 190 : 150).AlignLeft().Height(sheet.Wide ? 62 : 56).Image(ReportAssets.Logo).FitArea();
                    row.RelativeItem().AlignCenter().AlignMiddle().Column(t =>
                    {
                        for (var i = 0; i < sheet.TitleLines.Count; i++)
                            t.Item().AlignCenter().Text(sheet.TitleLines[i]).Bold().FontSize(i == 0 ? 14 : 11);
                    });
                    row.ConstantItem(sheet.Wide ? 260 : 220).AlignRight().Height(sheet.Wide ? 40 : 36).Image(ReportAssets.Banner).FitArea();
                });
                col.Item().PaddingTop(4).LineHorizontal(1.2f).LineColor(Navy);
            });
        }

        private static void Content(IContainer c, ReportSheet sheet)
        {
            c.Column(col =>
            {
                col.Spacing(8);

                foreach (var info in sheet.InfoLines)
                    col.Item().Text(info).Bold();

                foreach (var table in sheet.Tables)
                    col.Item().Element(x => Table(x, table));

                foreach (var note in sheet.Notes)
                {
                    var text = col.Item().Text(note);
                    if (note.EndsWith(':')) text.Bold();
                }
            });
        }

        private static void Table(IContainer c, ReportTable table)
        {
            c.Column(col =>
            {
                if (table.Heading != null)
                    col.Item().PaddingBottom(3).Text(table.Heading).Bold().FontSize(10);

                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(d =>
                    {
                        foreach (var column in table.Columns) d.RelativeColumn((float)column.Width);
                    });

                    t.Header(h =>
                    {
                        foreach (var column in table.Columns)
                            h.Cell().Background(Navy).Border(0.5f).BorderColor(Grid).Padding(3).AlignCenter().AlignMiddle()
                                .Text(column.Header).Bold().FontColor(Colors.White);
                    });

                    if (table.Rows.Count == 0)
                    {
                        t.Cell().ColumnSpan((uint)table.Columns.Count).Border(0.5f).BorderColor(Grid).Padding(8)
                            .AlignCenter().Text(table.EmptyMessage).Italic();
                    }

                    foreach (var row in table.Rows)
                        for (var i = 0; i < table.Columns.Count; i++)
                            Cell(t, table.Columns[i].Kind, row[i], bold: false, fill: null);

                    foreach (var footer in new[] { table.TotalRow, table.SecondTotalRow })
                    {
                        if (footer == null) continue;
                        for (var i = 0; i < table.Columns.Count; i++)
                            Cell(t, table.Columns[i].Kind, footer[i], bold: true, fill: Band);
                    }
                });
            });
        }

        private static void Cell(TableDescriptor t, ColumnKind kind, object? value, bool bold, string? fill)
        {
            var text = Format(kind, value);
            var cell = t.Cell().Background(fill ?? Colors.White).Border(0.5f).BorderColor(Grid).Padding(3).AlignMiddle();
            cell = kind switch
            {
                ColumnKind.Money => cell.AlignRight(),
                ColumnKind.Text => cell.AlignLeft(),
                _ => cell.AlignCenter()
            };
            var span = cell.Text(text);
            if (bold) span.Bold();
        }

        private static string Format(ColumnKind kind, object? value) => value switch
        {
            null => string.Empty,
            DateOnly d => d.ToString("MMM d, yyyy", Ph),
            decimal m => "₱" + m.ToString("#,##0.00", Ph),
            double f => f.ToString("0.00%", Ph), // doubles are only ever shares (0–1)
            int i when kind == ColumnKind.Integer => i.ToString("#,##0", Ph),
            _ => value.ToString() ?? string.Empty
        };
    }
}
