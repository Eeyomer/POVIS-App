using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Services.BulkImport;

namespace POVISAPI.Services
{
    public class BulkImportService : IBulkImportService
    {
        // Ordinances keep their own importer (OrdinancesController.bulk-upload) and template.
        private static readonly BulkModule[] Modules =
        {
            new BarangayBulkModule(),
            new UserBulkModule(),
            new ViolatorBulkModule(),
            new ViolationBulkModule(),
            new PaymentBulkModule(),
            new SeminarBulkModule(),
            new CounselingBulkModule(),
            new CommunityServiceBulkModule(),
            new FiledCaseBulkModule()
        };

        private readonly AppDbContext db;
        public BulkImportService(AppDbContext _db) => db = _db;

        public List<BulkModuleInfoDTO> ListModules() =>
            Modules.Select(m => new BulkModuleInfoDTO { Key = m.Key, Title = m.Title }).ToList();

        private static BulkModule Find(string key) =>
            Modules.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Unknown bulk upload module '{key}'.");

        public byte[] GetTemplate(string module) => BulkTemplateBuilder.Build(Find(module));

        public async Task<BulkImportResultDTO> ImportAsync(string module, Stream workbook, string uploader)
        {
            var m = Find(module);
            var result = new BulkImportResultDTO { Module = m.Key };
            var ctx = new BulkContext(result, uploader);

            XLWorkbook wb;
            try { wb = new XLWorkbook(workbook); }
            catch { throw new ArgumentException("The file is not a valid Excel (.xlsx) workbook."); }

            using (wb)
            {
                var ws = wb.Worksheets.FirstOrDefault(w => string.Equals(w.Name, m.SheetName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"The workbook has no '{m.SheetName}' sheet. Please use the provided template.");

                var rows = ReadRows(m, ws, ctx);
                if (rows.Count == 0 && ctx.ErrorCount == 0)
                    throw new ArgumentException("The template has no data rows.");
                result.RowsRead = rows.Count;

                if (ctx.ErrorCount == 0) await m.StageAsync(db, rows, ctx);
            }

            if (ctx.ErrorCount > 0)
            {
                // Nothing may be saved; drop everything that was staged.
                db.ChangeTracker.Clear();
                result.Counts.Clear();
                result.Credentials.Clear();
                return result;
            }

            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                throw new InvalidOperationException("The database rejected the upload (for example duplicate data). Nothing was imported.");
            }
            return result;
        }

        private static List<BulkRow> ReadRows(BulkModule m, IXLWorksheet ws, BulkContext ctx)
        {
            var columns = m.Columns.ToDictionary(c => c.Key);
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var map = new Dictionary<string, int>();
            for (int c = 1; c <= lastCol; c++)
            {
                var key = BulkColumn.Normalize(ws.Cell(1, c).GetString());
                if (columns.ContainsKey(key) && !map.ContainsKey(key)) map[key] = c;
            }

            var missing = m.Columns.Where(c => c.Required && !map.ContainsKey(c.Key)).Select(c => c.Header.TrimEnd(' ', '*')).ToList();
            if (missing.Count > 0)
                throw new ArgumentException("Missing required column(s) in row 1: " + string.Join(", ", missing) + ". Please use the provided template.");

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > m.MaxRows)
                throw new ArgumentException($"Too many rows. Upload at most {m.MaxRows:N0} rows at a time.");

            var rows = new List<BulkRow>();
            for (int r = 2; r <= lastRow; r++)
            {
                var xr = ws.Row(r);
                if (xr.IsEmpty(XLCellsUsedOptions.Contents)) continue;
                var cells = map.ToDictionary(kv => kv.Key, kv => xr.Cell(kv.Value));
                rows.Add(new BulkRow(r, cells, columns, ctx));
            }
            return rows;
        }
    }
}
