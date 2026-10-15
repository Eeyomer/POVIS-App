namespace POVISAPI.Services.Reports
{
    public enum ColumnKind { Text, Integer, Money, Percent, Date, Center }

    public class ReportColumn
    {
        public string Header { get; init; } = string.Empty;
        /// <summary>Relative width (1 ≈ a short number column, 6 ≈ a long label).</summary>
        public double Width { get; init; } = 1;
        public ColumnKind Kind { get; init; } = ColumnKind.Text;
    }

    public class ReportTable
    {
        public string? Heading { get; init; }
        public List<ReportColumn> Columns { get; init; } = new();
        public List<object?[]> Rows { get; init; } = new();
        /// <summary>Optional bold footer row (same length as Columns).</summary>
        public object?[]? TotalRow { get; init; }
        /// <summary>Optional second footer row (e.g. percentages).</summary>
        public object?[]? SecondTotalRow { get; init; }
        public string EmptyMessage { get; init; } = "No records for this period.";
    }

    /// <summary>One worksheet in the Excel file / one run of pages in the PDF.</summary>
    public class ReportSheet
    {
        public string Name { get; init; } = "Report";
        /// <summary>Centered, bold lines printed between the logos.</summary>
        public List<string> TitleLines { get; init; } = new();
        /// <summary>Left-aligned lines printed above the tables.</summary>
        public List<string> InfoLines { get; init; } = new();
        public List<ReportTable> Tables { get; init; } = new();
        public List<string> Notes { get; init; } = new();
        /// <summary>Use A3 landscape (wide matrices) instead of A4 landscape.</summary>
        public bool Wide { get; init; }
    }

    public class ReportDocument
    {
        public string FileBaseName { get; init; } = "report";
        public List<ReportSheet> Sheets { get; init; } = new();
    }

    public static class ReportAssets
    {
        private static readonly Lazy<byte[]> _logo = new(() => Load("pasig-logo.png"));
        private static readonly Lazy<byte[]> _banner = new(() => Load("pod-banner.png"));

        /// <summary>Pasig City seal + "Lungsod ng Pasig" wordmark.</summary>
        public static byte[] Logo => _logo.Value;
        /// <summary>"Peace and Order Department – Bantay Pasig Division" banner.</summary>
        public static byte[] Banner => _banner.Value;

        public static (int Width, int Height) Size(byte[] png) =>
            ((png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19],
             (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23]);

        private static byte[] Load(string name) =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Resources", name));
    }
}
