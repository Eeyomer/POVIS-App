using ClosedXML.Excel;

namespace POVISAPI.Services.Reports
{
    public static class ExcelReportRenderer
    {
        private const int HeaderRows = 5;
        private const int MinSheetPx = 900;
        private static readonly XLColor Navy = XLColor.FromHtml("#1B2A6B");
        private static readonly XLColor Band = XLColor.FromHtml("#E8ECF5");

        private static double ColPx(double chars) => chars * 7 + 5;

        public static byte[] Render(ReportDocument doc)
        {
            using var wb = new XLWorkbook();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in doc.Sheets)
                RenderSheet(wb.Worksheets.Add(UniqueName(sheet.Name, used)), sheet);

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static string UniqueName(string name, HashSet<string> used)
        {
            var clean = new string(name.Where(c => !"[]:*?/\\".Contains(c)).ToArray()).Trim();
            if (clean.Length == 0) clean = "Report";
            if (clean.Length > 31) clean = clean[..31];
            var candidate = clean;
            for (var i = 2; !used.Add(candidate); i++)
            {
                var suffix = $" ({i})";
                candidate = clean[..Math.Min(clean.Length, 31 - suffix.Length)] + suffix;
            }
            return candidate;
        }

        private static void RenderSheet(IXLWorksheet ws, ReportSheet sheet)
        {
            var colCount = Math.Max(1, sheet.Tables.Count == 0 ? 1 : sheet.Tables.Max(t => t.Columns.Count));
            ws.Style.Font.FontName = "Times New Roman";
            ws.Style.Font.FontSize = 11;
            ws.ShowGridLines = false;

            // ── Column widths: widest requirement across the tables on this sheet ─
            var widths = new double[colCount];
            foreach (var t in sheet.Tables)
                for (var i = 0; i < t.Columns.Count; i++)
                    widths[i] = Math.Max(widths[i], t.Columns[i].Width * 6 + 4);
            for (var i = 0; i < colCount; i++) if (widths[i] == 0) widths[i] = 12;

            var total = widths.Sum(ColPx);
            if (total < MinSheetPx)
            {
                var widest = Array.IndexOf(widths, widths.Max());
                widths[widest] += (MinSheetPx - total) / 7;
            }
            for (var i = 0; i < colCount; i++) ws.Column(i + 1).Width = widths[i];
            var totalPx = widths.Sum(ColPx);

            // ── Header band: logo (left), centered titles, banner (right) ─────────
            for (var r = 1; r <= HeaderRows; r++) ws.Row(r).Height = 18;

            var titleRow = 1;
            foreach (var line in sheet.TitleLines.Take(HeaderRows))
            {
                var range = ws.Range(titleRow, 1, titleRow, colCount).Merge();
                range.Value = line;
                range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                range.Style.Font.Bold = true;
                range.Style.Font.FontSize = titleRow == 1 ? 14 : 12;
                titleRow++;
            }

            const double headerPx = 4 * 18 * 96.0 / 72; // keep pictures within ~4 header rows
            var (lw, lh) = ReportAssets.Size(ReportAssets.Logo);
            var logoH = (int)headerPx;
            var logoW = (int)(lw * logoH / (double)lh);
            ws.AddPicture(new MemoryStream(ReportAssets.Logo)).MoveTo(ws.Cell(1, 1), 6, 4).WithSize(logoW, logoH);

            var (bw, bh) = ReportAssets.Size(ReportAssets.Banner);
            var bannerW = Math.Min(300, (int)(totalPx / 3));
            var bannerH = (int)(bh * bannerW / (double)bw);
            var bx = Math.Max(0, totalPx - bannerW - 6);
            var col = 1;
            double acc = 0;
            while (col < colCount && acc + ColPx(widths[col - 1]) <= bx) { acc += ColPx(widths[col - 1]); col++; }
            ws.AddPicture(new MemoryStream(ReportAssets.Banner)).MoveTo(ws.Cell(1, col), (int)(bx - acc), 8).WithSize(bannerW, bannerH);

            var row = HeaderRows + 2;

            foreach (var info in sheet.InfoLines)
            {
                var range = ws.Range(row, 1, row, colCount).Merge();
                range.Value = info;
                range.Style.Font.Bold = true;
                range.Style.Alignment.WrapText = true;
                ws.Row(row).Height = 16 * Lines(info, widths.Sum());
                row++;
            }
            if (sheet.InfoLines.Count > 0) row++;

            // ── Tables ───────────────────────────────────────────────────────────
            foreach (var table in sheet.Tables)
            {
                if (table.Heading != null)
                {
                    ws.Cell(row, 1).Value = table.Heading;
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 1).Style.Font.FontSize = 12;
                    row++;
                }

                var headerRow = row;
                for (var c = 0; c < table.Columns.Count; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    cell.Value = table.Columns[c].Header;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = Navy;
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Alignment.WrapText = true;
                }
                ws.Row(row).Height = 16 * table.Columns.Select((c, i) => Lines(c.Header, widths[i])).DefaultIfEmpty(1).Max() + 4;
                row++;

                if (table.Rows.Count == 0)
                {
                    var range = ws.Range(row, 1, row, table.Columns.Count).Merge();
                    range.Value = table.EmptyMessage;
                    range.Style.Font.Italic = true;
                    range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }

                foreach (var data in table.Rows)
                {
                    WriteRow(ws, row, table, data, bold: false);
                    ws.Row(row).Height = 16 * table.Columns.Select((c, i) => Lines(data[i]?.ToString() ?? "", widths[i])).Max();
                    row++;
                }

                foreach (var footer in new[] { table.TotalRow, table.SecondTotalRow })
                {
                    if (footer == null) continue;
                    WriteRow(ws, row, table, footer, bold: true);
                    ws.Range(row, 1, row, table.Columns.Count).Style.Fill.BackgroundColor = Band;
                    row++;
                }

                ws.Range(headerRow, 1, row - 1, table.Columns.Count).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(headerRow, 1, row - 1, table.Columns.Count).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                row++;
            }

            foreach (var note in sheet.Notes)
            {
                var range = ws.Range(row, 1, row, colCount).Merge();
                range.Value = note;
                range.Style.Font.Bold = note.EndsWith(':');
                range.Style.Alignment.WrapText = true;
                ws.Row(row).Height = 16 * Lines(note, widths.Sum());
                row++;
            }

            // Landscape, fit to one page wide when printed.
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.PaperSize = sheet.Wide ? XLPaperSize.A3Paper : XLPaperSize.A4Paper;
            ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.Margins.Left = ws.PageSetup.Margins.Right = 0.4;
        }

        private static int Lines(string text, double widthChars) =>
            Math.Max(1, (int)Math.Ceiling(text.Length * 1.1 / Math.Max(4, widthChars)));

        private static void WriteRow(IXLWorksheet ws, int row, ReportTable table, object?[] data, bool bold)
        {
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var cell = ws.Cell(row, c + 1);
                var kind = table.Columns[c].Kind;
                var value = data[c];

                switch (value)
                {
                    case null: break;
                    case DateOnly d: cell.Value = d.ToDateTime(TimeOnly.MinValue); break;
                    case int i: cell.Value = i; break;
                    case decimal m: cell.Value = m; break;
                    case double f: cell.Value = f; break;
                    default: cell.Value = value.ToString(); break;
                }

                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Font.Bold = bold;
                if (value is double) kind = ColumnKind.Percent; // doubles are only ever shares (0–1)
                switch (kind)
                {
                    case ColumnKind.Integer: cell.Style.NumberFormat.Format = "#,##0"; cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; break;
                    case ColumnKind.Money: cell.Style.NumberFormat.Format = "\"₱\"#,##0.00"; cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right; break;
                    case ColumnKind.Percent: cell.Style.NumberFormat.Format = "0.00%"; cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; break;
                    case ColumnKind.Date: cell.Style.NumberFormat.Format = "mmm d, yyyy"; cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; break;
                    case ColumnKind.Center: cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; break;
                    default: cell.Style.Alignment.WrapText = true; break;
                }
            }
        }
    }
}
