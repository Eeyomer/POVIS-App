using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using POVISAPI.Data;
using POVISAPI.Interfaces;
using POVISAPI.Models.DTO;
using POVISAPI.Models.Entity;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Services
{
    public class OrdinanceImportService : IOrdinanceImportService
    {
        private const string SheetName = "Ordinances";
        private const int MaxRows = 2000;

        private readonly AppDbContext dbContext;
        public OrdinanceImportService(AppDbContext _dbContext) => dbContext = _dbContext;

        // Headers are matched on letters only, lowercase, so "Ordinance Number *" == "ordinancenumber".
        private static readonly string[] RequiredHeaders =
            { "ordinancenumber", "series", "title", "shortname" };

        private static readonly string[] PenaltyOptionalHeaders =
        {
            "offendertype", "fineamount", "seminardays", "communityservicehours", "communityservicedays",
            "requirescounseling", "hasimprisonment", "imprisonmentterm", "businesspermitaction", "isconjunctive"
        };

        private static readonly string[] AllHeaders = RequiredHeaders
            .Concat(new[] { "isexecutiveorder", "sectioncode", "sectiondescription", "offenselevel", "penaltytext" })
            .Concat(PenaltyOptionalHeaders)
            .ToArray();

        private sealed class Row
        {
            public int RowNumber;
            public string OrdinanceNumber = "";
            public int Series;
            public string Title = "";
            public string ShortName = "";
            public bool IsExecutiveOrder;
            public string? SectionCode;
            public string? SectionDescription;
            public bool HasPenalty;
            public OffenseLevel OffenseLevel;
            public OffenderType? OffenderType;
            public decimal? FineAmount;
            public int? SeminarDays;
            public int? CommunityServiceHours;
            public int? CommunityServiceDays;
            public bool RequiresCounseling;
            public bool HasImprisonment;
            public string? ImprisonmentTerm;
            public BusinessPermitAction BusinessPermitAction;
            public bool IsConjunctive;
            public string PenaltyText = "";
        }

        public async Task<OrdinanceImportResultDTO> ImportAsync(Stream workbook)
        {
            var result = new OrdinanceImportResultDTO();

            XLWorkbook wb;
            try { wb = new XLWorkbook(workbook); }
            catch { throw new ArgumentException("The file is not a valid Excel (.xlsx) workbook."); }

            using (wb)
            {
                var ws = wb.Worksheets.FirstOrDefault(w => string.Equals(w.Name, SheetName, StringComparison.OrdinalIgnoreCase))
                    ?? throw new ArgumentException($"The workbook has no '{SheetName}' sheet. Please use the provided template.");

                var rows = ReadRows(ws, result);
                if (result.Errors.Count > 0) return result;
                if (rows.Count == 0)
                    throw new ArgumentException("The template has no data rows.");
                result.RowsRead = rows.Count;

                await ApplyAsync(rows, result);
            }
            return result;
        }

        private static List<Row> ReadRows(IXLWorksheet ws, OrdinanceImportResultDTO result)
        {
            var headerRow = ws.Row(1);
            var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            var cols = new Dictionary<string, int>();
            for (int c = 1; c <= lastCol; c++)
            {
                var key = Normalize(headerRow.Cell(c).GetString());
                if (AllHeaders.Contains(key) && !cols.ContainsKey(key)) cols[key] = c;
            }
            var missing = RequiredHeaders.Where(h => !cols.ContainsKey(h)).ToList();
            if (missing.Count > 0)
                throw new ArgumentException("Missing required column(s) in row 1: " + string.Join(", ", missing) + ". Please use the provided template.");

            string? Text(IXLRow r, string key) =>
                cols.TryGetValue(key, out var c) ? NullIfEmpty(r.Cell(c).GetFormattedString()) : null;

            var rows = new List<Row>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow - 1 > MaxRows)
                throw new ArgumentException($"Too many rows. Upload at most {MaxRows} rows at a time.");

            for (int r = 2; r <= lastRow; r++)
            {
                var xr = ws.Row(r);
                if (xr.IsEmpty(XLCellsUsedOptions.Contents)) continue;

                int rowNumber = r;
                void Err(string msg) => result.Errors.Add(new OrdinanceImportErrorDTO { Row = rowNumber, Message = msg });
                int before = result.Errors.Count;

                var row = new Row { RowNumber = r };

                row.OrdinanceNumber = Text(xr, "ordinancenumber") ?? "";
                if (row.OrdinanceNumber.Length == 0) Err("Ordinance Number is required.");
                else if (row.OrdinanceNumber.Length > 20) Err("Ordinance Number cannot exceed 20 characters.");

                if (!int.TryParse(Text(xr, "series"), out var series) || series < 1900 || series > 2100)
                    Err("Series must be a year between 1900 and 2100.");
                row.Series = series;

                row.Title = Text(xr, "title") ?? "";
                if (row.Title.Length == 0) Err("Title is required.");
                else if (row.Title.Length > 500) Err("Title cannot exceed 500 characters.");

                row.ShortName = Text(xr, "shortname") ?? "";
                if (row.ShortName.Length == 0) Err("Short Name is required.");
                else if (row.ShortName.Length > 200) Err("Short Name cannot exceed 200 characters.");

                row.IsExecutiveOrder = ParseBool(Text(xr, "isexecutiveorder"), "Is Executive Order", Err);

                row.SectionCode = Text(xr, "sectioncode");
                if (row.SectionCode is { Length: > 50 }) Err("Section Code cannot exceed 50 characters.");
                row.SectionDescription = Text(xr, "sectiondescription");
                if (row.SectionDescription is { Length: > 500 }) Err("Section Description cannot exceed 500 characters.");
                if (row.SectionCode == null && row.SectionDescription != null)
                    Err("Section Description was given without a Section Code.");

                var levelText = Text(xr, "offenselevel");
                var penaltyText = Text(xr, "penaltytext");
                row.HasPenalty = levelText != null || penaltyText != null
                    || PenaltyOptionalHeaders.Any(k => Text(xr, k) != null);

                if (row.HasPenalty)
                {
                    if (levelText == null) Err("Offense Level is required when a penalty is given.");
                    else if (!TryParseLevel(levelText, out row.OffenseLevel))
                        Err($"Offense Level '{levelText}' is not valid. Use First, Second, Third or Fourth.");

                    if (penaltyText == null) Err("Penalty Text is required when a penalty is given.");
                    else if (penaltyText.Length > 1000) Err("Penalty Text cannot exceed 1,000 characters.");
                    row.PenaltyText = penaltyText ?? "";

                    var typeText = Text(xr, "offendertype");
                    if (typeText != null)
                    {
                        if (TryParseEnum<OffenderType>(typeText, out var ot)) row.OffenderType = ot;
                        else Err($"Offender Type '{typeText}' is not valid. Use Individual, JuridicalPerson, Minor, ParentOrGuardian or Establishment.");
                    }

                    var fineText = Text(xr, "fineamount");
                    if (fineText != null)
                    {
                        if (decimal.TryParse(fineText.Replace("₱", "").Replace(",", "").Trim(), out var fine) && fine >= 0 && fine <= 1_000_000)
                            row.FineAmount = fine;
                        else Err("Fine Amount must be a number between 0 and 1,000,000.");
                    }

                    row.SeminarDays = ParseInt(Text(xr, "seminardays"), "Seminar Days", 0, 365, Err);
                    row.CommunityServiceHours = ParseInt(Text(xr, "communityservicehours"), "Community Service Hours", 0, 1000, Err);
                    row.CommunityServiceDays = ParseInt(Text(xr, "communityservicedays"), "Community Service Days", 0, 365, Err);
                    row.RequiresCounseling = ParseBool(Text(xr, "requirescounseling"), "Requires Counseling", Err);
                    row.HasImprisonment = ParseBool(Text(xr, "hasimprisonment"), "Has Imprisonment", Err);
                    row.ImprisonmentTerm = Text(xr, "imprisonmentterm");
                    if (row.ImprisonmentTerm is { Length: > 300 }) Err("Imprisonment Term cannot exceed 300 characters.");
                    if (row.ImprisonmentTerm != null) row.HasImprisonment = true;
                    row.IsConjunctive = ParseBool(Text(xr, "isconjunctive"), "Is Conjunctive", Err);

                    var permitText = Text(xr, "businesspermitaction");
                    if (permitText != null && !TryParseEnum(permitText, out row.BusinessPermitAction))
                        Err($"Business Permit Action '{permitText}' is not valid. Use None, Suspension or Cancellation.");
                }

                if (result.Errors.Count == before) rows.Add(row);
            }
            return rows;
        }

        private async Task ApplyAsync(List<Row> rows, OrdinanceImportResultDTO result)
        {
            // Rows of the same ordinance must agree on its details.
            foreach (var g in rows.GroupBy(r => (Number: r.OrdinanceNumber.ToLowerInvariant(), r.Series)))
            {
                var first = g.First();
                foreach (var r in g.Skip(1))
                {
                    if (r.Title != first.Title || r.ShortName != first.ShortName || r.IsExecutiveOrder != first.IsExecutiveOrder)
                        result.Errors.Add(new OrdinanceImportErrorDTO
                        {
                            Row = r.RowNumber,
                            Message = $"Ordinance {first.OrdinanceNumber} S.{first.Series} has a different Title / Short Name / Executive Order value than row {first.RowNumber}."
                        });
                }
            }
            if (result.Errors.Count > 0) return;

            var ordinances = (await dbContext.Ordinances
                    .Include(o => o.Sections)
                    .Include(o => o.Penalties)
                    .ToListAsync())
                .ToDictionary(o => (o.OrdinanceNumber.ToLowerInvariant(), o.Series));

            var counted = new HashSet<(string, int)>();
            var seenPenalties = new HashSet<(Guid, Guid?, OffenseLevel, OffenderType?)>();

            foreach (var r in rows)
            {
                var key = (r.OrdinanceNumber.ToLowerInvariant(), r.Series);
                if (!ordinances.TryGetValue(key, out var ordinance))
                {
                    ordinance = new Ordinance
                    {
                        OrdinanceNumber = r.OrdinanceNumber,
                        Series = r.Series,
                        Title = r.Title,
                        ShortName = r.ShortName,
                        IsExecutiveOrder = r.IsExecutiveOrder
                    };
                    dbContext.Ordinances.Add(ordinance);
                    ordinances[key] = ordinance;
                    counted.Add(key);
                    result.OrdinancesCreated++;
                }
                else if (counted.Add(key))
                {
                    result.OrdinancesExisting++;
                }

                OrdinanceSection? section = null;
                if (r.SectionCode != null)
                {
                    section = ordinance.Sections.FirstOrDefault(s =>
                        string.Equals(s.SectionCode, r.SectionCode, StringComparison.OrdinalIgnoreCase));
                    if (section == null)
                    {
                        section = new OrdinanceSection
                        {
                            OrdinanceId = ordinance.Id,
                            SectionCode = r.SectionCode,
                            Description = r.SectionDescription
                        };
                        ordinance.Sections.Add(section);
                        result.SectionsCreated++;
                    }
                }

                if (!r.HasPenalty) continue;

                var sectionId = section?.Id;
                bool duplicate =
                    ordinance.Penalties.Any(p => p.OrdinanceSectionId == sectionId && p.OffenseLevel == r.OffenseLevel && p.OffenderType == r.OffenderType)
                    || !seenPenalties.Add((ordinance.Id, sectionId, r.OffenseLevel, r.OffenderType));
                if (duplicate)
                {
                    result.PenaltiesSkipped++;
                    continue;
                }

                ordinance.Penalties.Add(new OrdinancePenalty
                {
                    OrdinanceId = ordinance.Id,
                    OrdinanceSectionId = sectionId,
                    OffenseLevel = r.OffenseLevel,
                    OffenderType = r.OffenderType,
                    FineAmount = r.FineAmount,
                    SeminarDays = r.SeminarDays,
                    CommunityServiceHours = r.CommunityServiceHours,
                    CommunityServiceDays = r.CommunityServiceDays,
                    RequiresCounseling = r.RequiresCounseling,
                    HasImprisonment = r.HasImprisonment,
                    ImprisonmentTerm = r.ImprisonmentTerm,
                    BusinessPermitAction = r.BusinessPermitAction,
                    IsConjunctive = r.IsConjunctive,
                    PenaltyText = r.PenaltyText
                });
                result.PenaltiesCreated++;
            }

            await dbContext.SaveChangesAsync();
        }

        // ---------- helpers ----------
        private static string Normalize(string header) =>
            new string(header.Where(char.IsLetter).ToArray()).ToLowerInvariant();

        private static string? NullIfEmpty(string? s)
        {
            s = s?.Trim();
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static bool TryParseLevel(string text, out OffenseLevel level)
        {
            switch (text.Trim().ToLowerInvariant())
            {
                case "1": case "1st": case "first": level = OffenseLevel.First; return true;
                case "2": case "2nd": case "second": level = OffenseLevel.Second; return true;
                case "3": case "3rd": case "third": level = OffenseLevel.Third; return true;
                case "4": case "4th": case "fourth": level = OffenseLevel.Fourth; return true;
            }
            level = default;
            return false;
        }

        private static bool TryParseEnum<T>(string text, out T value) where T : struct, Enum
        {
            var cleaned = new string(text.Where(char.IsLetterOrDigit).ToArray());
            return Enum.TryParse(cleaned, ignoreCase: true, out value) && Enum.IsDefined(value);
        }

        private static bool ParseBool(string? text, string field, Action<string> err)
        {
            if (text == null) return false;
            switch (text.Trim().ToLowerInvariant())
            {
                case "yes": case "y": case "true": case "1": return true;
                case "no": case "n": case "false": case "0": return false;
            }
            err($"{field} must be Yes or No.");
            return false;
        }

        private static int? ParseInt(string? text, string field, int min, int max, Action<string> err)
        {
            if (text == null) return null;
            if (int.TryParse(text, out var v) && v >= min && v <= max) return v;
            err($"{field} must be a whole number between {min} and {max:N0}.");
            return null;
        }
    }
}
