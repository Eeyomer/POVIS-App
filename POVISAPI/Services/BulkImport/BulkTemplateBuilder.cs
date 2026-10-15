using System.Globalization;
using ClosedXML.Excel;

namespace POVISAPI.Services.BulkImport
{
    /// <summary>Builds the downloadable template for a module in the same layout as the ordinance template:
    /// Instructions, the data sheet, Example (not imported) and Lists.</summary>
    public static class BulkTemplateBuilder
    {
        private static readonly XLColor Navy = XLColor.FromHtml("#1E3A63");
        private static readonly XLColor Steel = XLColor.FromHtml("#5B7BA8");

        public static byte[] Build(BulkModule m)
        {
            using var wb = new XLWorkbook();
            Instructions(wb.Worksheets.Add("Instructions"), m);
            DataSheet(wb.Worksheets.Add(m.SheetName), m, withExample: false);
            DataSheet(wb.Worksheets.Add("Example"), m, withExample: true);
            Lists(wb.Worksheets.Add("Lists"), m);
            wb.Worksheet("Instructions").SetTabColor(Navy);
            wb.Worksheet(m.SheetName).SetTabColor(XLColor.FromHtml("#2E8B57"));
            wb.Worksheet("Example").SetTabColor(XLColor.FromHtml("#E0A100"));
            wb.Worksheet("Lists").SetTabColor(XLColor.FromHtml("#888888"));
            wb.Worksheet(m.SheetName).SetTabActive();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void Instructions(IXLWorksheet ws, BulkModule m)
        {
            ws.Column(1).Width = 30;
            ws.Column(2).Width = 100;
            ws.ShowGridLines = false;

            ws.Cell(1, 1).Value = $"POVIS - {m.Title} Bulk Upload Template";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = Navy;

            var row = 3;
            void Heading(string text)
            {
                ws.Cell(row, 1).Value = text;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 12;
                ws.Cell(row, 1).Style.Font.FontColor = Navy;
                row++;
            }
            void Line(string a, string b)
            {
                ws.Cell(row, 1).Value = a;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                ws.Cell(row, 1).Style.Alignment.WrapText = true;
                ws.Cell(row, 2).Value = b;
                ws.Cell(row, 2).Style.Alignment.WrapText = true;
                ws.Cell(row, 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                ws.Row(row).Height = 15.5 * Math.Max(1, (int)Math.Ceiling(b.Length / 105.0));
                row++;
            }

            Heading("How it works");
            var step = 1;
            Line((step++).ToString(), $"Fill in the \"{m.SheetName}\" sheet. Each row is ONE record (see the \"Example\" sheet for reference rows).");
            if (m.Prerequisites != null) Line((step++).ToString(), m.Prerequisites);
            Line((step++).ToString(), $"Save as .xlsx and upload it in the {m.Title} page > Bulk Upload (MIS Personnel only).");
            Line((step++).ToString(), "The upload is all-or-nothing: if any row has an error, nothing is saved and the row numbers with problems are shown.");
            Line((step).ToString(), "Dates: use yyyy-mm-dd (e.g. 2026-08-15). Yes/No columns have a dropdown. Columns marked * are required.");
            row++;

            Heading("Columns");
            foreach (var c in m.Columns)
                Line(c.Header + (c.Required && !c.Header.EndsWith('*') ? " *" : string.Empty), c.Description);
            row++;

            Heading("Good to know");
            foreach (var (title, text) in m.Notes) Line(title, text);
            Line("Limits", $"Up to {m.MaxRows:N0} rows per upload, .xlsx only. Do not rename the \"{m.SheetName}\" sheet or the column headers in row 1.");
        }

        private static void DataSheet(IXLWorksheet ws, BulkModule m, bool withExample)
        {
            var cols = m.Columns;
            for (var i = 0; i < cols.Count; i++)
            {
                var c = cols[i];
                var col = ws.Column(i + 1);
                col.Width = Math.Clamp(c.Header.Length + 6, 14, 28);
                if (c.Type == ColType.Text) col.Style.NumberFormat.Format = "@";
                else if (c.Type == ColType.Date) col.Style.NumberFormat.Format = "yyyy-mm-dd";
                else if (c.Type == ColType.Time) col.Style.NumberFormat.Format = "hh:mm";

                var cell = ws.Cell(1, i + 1);
                cell.Value = c.Header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = c.Required ? Navy : Steel;
                cell.Style.Alignment.WrapText = true;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(1).Height = 32;
            ws.SheetView.FreezeRows(1);

            if (!withExample)
            {
                for (var i = 0; i < cols.Count; i++)
                    if (cols[i].Options is { Length: > 0 } opts)
                    {
                        var dv = ws.Range(2, i + 1, 1000, i + 1).CreateDataValidation();
                        dv.List(string.Join(",", opts));
                        dv.IgnoreBlanks = true;
                    }
                return;
            }

            for (var r = 0; r < m.Examples.Count; r++)
                for (var i = 0; i < cols.Count; i++)
                {
                    var text = i < m.Examples[r].Length ? m.Examples[r][i] : string.Empty;
                    if (text.Length == 0) continue;
                    var cell = ws.Cell(r + 2, i + 1);
                    switch (cols[i].Type)
                    {
                        case ColType.Date when DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d):
                            cell.Value = d; break;
                        case ColType.Time when TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var t):
                            cell.Value = t; break;
                        case ColType.Number when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var n):
                            cell.Value = n; break;
                        default: cell.Value = text; break;
                    }
                }

            var note = ws.Cell(1, cols.Count + 2);
            note.Value = "SAMPLE VALUES ONLY - illustrative. This sheet is NOT imported.";
            note.Style.Font.Bold = true;
            note.Style.Font.FontColor = XLColor.FromHtml("#C00000");
            ws.Column(cols.Count + 2).Width = 60;
        }

        private static void Lists(IXLWorksheet ws, BulkModule m)
        {
            var withOptions = m.Columns.Where(c => c.Options is { Length: > 0 }).ToList();
            if (withOptions.Count == 0)
            {
                ws.Cell(1, 1).Value = "This sheet has no dropdown lists.";
                return;
            }
            for (var i = 0; i < withOptions.Count; i++)
            {
                ws.Column(i + 1).Width = 24;
                var h = ws.Cell(1, i + 1);
                h.Value = withOptions[i].Header.TrimEnd(' ', '*');
                h.Style.Font.Bold = true;
                h.Style.Font.FontColor = XLColor.White;
                h.Style.Fill.BackgroundColor = Navy;
                for (var j = 0; j < withOptions[i].Options!.Length; j++)
                    ws.Cell(j + 2, i + 1).Value = withOptions[i].Options![j];
            }
        }
    }
}
