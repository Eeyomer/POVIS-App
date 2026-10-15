using System.Globalization;
using ClosedXML.Excel;
using POVISAPI.Data;
using POVISAPI.Models.DTO;

namespace POVISAPI.Services.BulkImport
{
    public enum ColType { Text, Number, Date, Time }

    public sealed record BulkColumn(string Header, bool Required, string Description, string[]? Options = null, ColType Type = ColType.Text)
    {
        /// <summary>Headers are matched on letters and digits only, lowercase: "Ordinance Number *" == "ordinancenumber".</summary>
        public string Key { get; } = Normalize(Header);
        public static string Normalize(string header) =>
            new string(header.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    /// <summary>Describes one module's upload sheet and knows how to validate + stage its rows.</summary>
    public abstract class BulkModule
    {
        public abstract string Key { get; }
        public abstract string Title { get; }
        public abstract string SheetName { get; }
        public virtual int MaxRows => 5000;
        public abstract IReadOnlyList<BulkColumn> Columns { get; }
        /// <summary>Example rows, one string per column (empty = blank). Dates as yyyy-MM-dd.</summary>
        public abstract IReadOnlyList<string[]> Examples { get; }
        /// <summary>"Good to know" entries on the instructions sheet.</summary>
        public virtual IReadOnlyList<(string Title, string Text)> Notes => Array.Empty<(string, string)>();
        /// <summary>What must already exist before this sheet can be uploaded.</summary>
        public virtual string? Prerequisites => null;

        /// <summary>Validate every row and add entities to the context. Do NOT save; the engine saves once if there are no errors.</summary>
        public abstract Task StageAsync(AppDbContext db, IReadOnlyList<BulkRow> rows, BulkContext ctx);
    }

    public sealed class BulkContext
    {
        public BulkContext(BulkImportResultDTO result, string uploader)
        {
            Result = result;
            Uploader = uploader;
        }

        public BulkImportResultDTO Result { get; }
        public string Uploader { get; }
        private const int MaxErrors = 500;

        public void Error(int row, string message)
        {
            if (Result.Errors.Count < MaxErrors) Result.Errors.Add(new BulkImportErrorDTO { Row = row, Message = message });
            ErrorCount++;
        }
        public int ErrorCount { get; private set; }

        public void Count(string label, int by = 1)
        {
            var c = Result.Counts.FirstOrDefault(x => x.Label == label);
            if (c == null) Result.Counts.Add(new BulkImportCountDTO { Label = label, Count = by });
            else c.Count += by;
        }
    }

    /// <summary>One data row of the uploaded sheet, with typed readers that record row-level errors.</summary>
    public sealed class BulkRow
    {
        private readonly Dictionary<string, IXLCell> _cells;
        private readonly Dictionary<string, BulkColumn> _columns;
        private readonly BulkContext _ctx;
        private int _errors;

        public int Number { get; }

        public BulkRow(int number, Dictionary<string, IXLCell> cells, Dictionary<string, BulkColumn> columns, BulkContext ctx)
        {
            Number = number;
            _cells = cells;
            _columns = columns;
            _ctx = ctx;
        }

        public bool HasErrors => _errors > 0;
        public void Error(string message)
        {
            _errors++;
            _ctx.Error(Number, message);
        }

        private string H(string key) => _columns[BulkColumn.Normalize(key)].Header.TrimEnd(' ', '*');
        private IXLCell? Cell(string key) => _cells.TryGetValue(BulkColumn.Normalize(key), out var c) ? c : null;

        public string? Text(string key, int maxLength = 0)
        {
            var cell = Cell(key);
            if (cell == null || cell.IsEmpty()) return null;
            var s = cell.DataType == XLDataType.DateTime ? cell.GetDateTime().ToString("yyyy-MM-dd") : cell.GetFormattedString().Trim();
            if (s.Length == 0) return null;
            if (maxLength > 0 && s.Length > maxLength) Error($"{H(key)} cannot exceed {maxLength} characters.");
            return s;
        }

        public string Required(string key, int maxLength = 0)
        {
            var s = Text(key, maxLength);
            if (s == null) Error($"{H(key)} is required.");
            return s ?? string.Empty;
        }

        public bool Bool(string key)
        {
            var s = Text(key);
            if (s == null) return false;
            switch (s.ToLowerInvariant())
            {
                case "yes": case "y": case "true": case "1": return true;
                case "no": case "n": case "false": case "0": return false;
            }
            Error($"{H(key)} must be Yes or No.");
            return false;
        }

        /// <summary>Yes/No with a default when the cell is blank.</summary>
        public bool Bool(string key, bool whenBlank) => Text(key) == null ? whenBlank : Bool(key);

        public int? Int(string key, int min, int max)
        {
            var cell = Cell(key);
            if (cell == null || cell.IsEmpty()) return null;
            var s = cell.GetFormattedString().Trim();
            if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var d) && d == Math.Floor(d)) return Range((int)d, key, min, max);
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return Range(v, key, min, max);
            Error($"{H(key)} must be a whole number between {min} and {max:N0}.");
            return null;
        }

        private int? Range(int v, string key, int min, int max)
        {
            if (v >= min && v <= max) return v;
            Error($"{H(key)} must be a whole number between {min} and {max:N0}.");
            return null;
        }

        public decimal? Decimal(string key, decimal min, decimal max)
        {
            var cell = Cell(key);
            if (cell == null || cell.IsEmpty()) return null;
            decimal v;
            bool ok;
            if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var d)) { v = (decimal)d; ok = true; }
            else ok = decimal.TryParse(cell.GetFormattedString().Replace("₱", "").Replace("PHP", "", StringComparison.OrdinalIgnoreCase).Replace(",", "").Trim(),
                NumberStyles.Number, CultureInfo.InvariantCulture, out v);
            if (!ok || v < min || v > max)
            {
                Error($"{H(key)} must be a number between {min:N0} and {max:N0}.");
                return null;
            }
            return v;
        }

        private static readonly string[] DateFormats =
        {
            "yyyy-MM-dd", "yyyy/MM/dd", "M/d/yyyy", "MM/dd/yyyy", "M-d-yyyy", "d MMM yyyy", "MMMM d, yyyy", "MMM d, yyyy", "MMMM d yyyy", "dd-MMM-yyyy"
        };

        public DateOnly? Date(string key)
        {
            var cell = Cell(key);
            if (cell == null || cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.DateTime) return DateOnly.FromDateTime(cell.GetDateTime());
            if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var serial) && serial > 1 && serial < 80000)
                return DateOnly.FromDateTime(DateTime.FromOADate(serial));
            var s = cell.GetFormattedString().Trim();
            if (DateOnly.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return d;
            Error($"{H(key)} '{s}' is not a valid date. Use yyyy-mm-dd, e.g. 2026-08-15.");
            return null;
        }

        public DateOnly RequiredDate(string key)
        {
            var d = Date(key);
            if (d == null && !HasErrorFor(key)) Error($"{H(key)} is required.");
            return d ?? default;
        }

        private bool HasErrorFor(string key) => Cell(key) is { } c && !c.IsEmpty();

        public TimeOnly? Time(string key)
        {
            var cell = Cell(key);
            if (cell == null || cell.IsEmpty()) return null;
            if (cell.DataType == XLDataType.DateTime) return TimeOnly.FromDateTime(cell.GetDateTime());
            if (cell.DataType == XLDataType.TimeSpan) return TimeOnly.FromTimeSpan(cell.GetTimeSpan());
            if (cell.DataType == XLDataType.Number && cell.TryGetValue<double>(out var f) && f >= 0 && f < 1)
                return TimeOnly.FromTimeSpan(TimeSpan.FromDays(f));
            var s = cell.GetFormattedString().Trim();
            if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, out var t)) return t;
            Error($"{H(key)} '{s}' is not a valid time. Use hh:mm, e.g. 14:30.");
            return null;
        }

        /// <summary>Case- and space-insensitive enum parse ("Juridical Person" == JuridicalPerson).</summary>
        public T? Choice<T>(string key) where T : struct, Enum
        {
            var s = Text(key);
            if (s == null) return null;
            var cleaned = new string(s.Where(char.IsLetterOrDigit).ToArray());
            if (System.Enum.TryParse<T>(cleaned, true, out var v) && System.Enum.IsDefined(v) && !int.TryParse(cleaned, out _)) return v;
            Error($"{H(key)} '{s}' is not valid. Use {string.Join(", ", System.Enum.GetNames<T>())}.");
            return null;
        }

        public T RequiredChoice<T>(string key) where T : struct, Enum
        {
            var v = Choice<T>(key);
            if (v == null)
            {
                if (Text(key) == null) Error($"{H(key)} is required.");
                return default;
            }
            return v.Value;
        }
    }
}
