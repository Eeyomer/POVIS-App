namespace POVISAPI.Models.DTO
{
    // ── OVERALL / MONTHLY / SEMI-ANNUAL ordinance summary ───────────────────
    public class OrdinanceSummaryRowDTO
    {
        /// <summary>Unique per ordinance + section + offense bucket (used as a row key).</summary>
        public string OrdinanceId { get; set; } = string.Empty;
        public string OrdinanceShortName { get; set; } = string.Empty;
        /// <summary>Printed row label, e.g. "ORDINANCE NO. 19, S. 2025 NO SMOKING IN PUBLIC PLACES".</summary>
        public string Label { get; set; } = string.Empty;
        public int FemaleCount { get; set; }
        public int MaleCount { get; set; }
        public int TotalViolators { get; set; }
        public int SeminarCount { get; set; }
        public int CounselingCount { get; set; }
        public int CommunityServiceCount { get; set; }
        public int DidNotComplyCount { get; set; }
        public int PaidCount { get; set; }
        /// <summary>Fine per violation from the ordinance penalty schedule (null when non-monetary).</summary>
        public decimal? FineAmount { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal TotalAmountUnpaid { get; set; }
    }

    public class OrdinanceSummaryReportDTO
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public List<OrdinanceSummaryRowDTO> Rows { get; set; } = new();
        public int TotalFemale { get; set; }
        public int TotalMale { get; set; }
        public int TotalViolators { get; set; }
        public int TotalSeminar { get; set; }
        public int TotalCounseling { get; set; }
        public int TotalCommunityService { get; set; }
        public int TotalDidNotComply { get; set; }
        public int TotalPaid { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal TotalAmountUnpaid { get; set; }
    }

    // ── % PER BRGY & PER ORDINANCES ─────────────────────────────────────────
    public class BarangayOrdinanceRowDTO
    {
        public string BarangayId { get; set; } = string.Empty;
        public string BarangayName { get; set; } = string.Empty;
        public int? District { get; set; }
        public Dictionary<string, int> CountsByOrdinance { get; set; } = new();
        public int Total { get; set; }
        /// <summary>Share of all violations in the range (0–1).</summary>
        public double Share { get; set; }
    }

    public class PerBarangayReportDTO
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public List<string> OrdinanceColumns { get; set; } = new();
        public List<BarangayOrdinanceRowDTO> Rows { get; set; } = new();
        public Dictionary<string, int> TotalsByOrdinance { get; set; } = new();
        public int GrandTotal { get; set; }
    }

    // ── TALAKAYAN SEMINAR PER BRGY ──────────────────────────────────────────
    public class SeminarViolationRowDTO
    {
        public DateOnly Date { get; set; }
        public string Division { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string OvrNumber { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string OrdinanceCode { get; set; } = string.Empty;
        public string OrdinanceName { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }

    public class SeminarEnforcerRowDTO
    {
        public string Name { get; set; } = string.Empty;
        public string DeputationNumber { get; set; } = string.Empty;
        public int Apprehensions { get; set; }
    }

    public class SeminarBarangaySectionDTO
    {
        public string BarangayId { get; set; } = string.Empty;
        public string BarangayName { get; set; } = string.Empty;
        public int Recorded { get; set; }
        public int Complied { get; set; }
        public int NotComplied { get; set; }
        public int CaseFiled { get; set; }
        public int Dismissed { get; set; }
        public List<string> Divisions { get; set; } = new();
        public List<SeminarViolationRowDTO> Rows { get; set; } = new();
        public List<SeminarEnforcerRowDTO> Enforcers { get; set; } = new();
    }

    public class SeminarBarangayReportDTO
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public List<SeminarBarangaySectionDTO> Barangays { get; set; } = new();
    }

    // ── POD / PNP / BSF INCENTIVES ──────────────────────────────────────────
    public class IncentivePaymentRowDTO
    {
        public DateOnly DateOfPayment { get; set; }
        public string Division { get; set; } = string.Empty;
        public string Deputized { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Incentive { get; set; }
    }

    public class IncentiveEnforcerTotalDTO
    {
        public string Deputized { get; set; } = string.Empty;
        public decimal TotalIncentive { get; set; }
    }

    public class IncentiveDivisionDTO
    {
        public string Division { get; set; } = string.Empty;
        public List<IncentivePaymentRowDTO> Payments { get; set; } = new();
        public List<IncentiveEnforcerTotalDTO> EnforcerTotals { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public decimal TotalIncentive { get; set; }
    }

    public class IncentiveReportDTO
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public decimal Rate { get; set; }
        public List<IncentiveDivisionDTO> Divisions { get; set; } = new();
    }

    // ── DEPUTIZED ENFORCERS ─────────────────────────────────────────────────
    public class EnforcerRowDTO
    {
        public string Office { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeputationNumber { get; set; } = string.Empty;
    }

    public class EnforcerListDTO
    {
        public List<EnforcerRowDTO> Rows { get; set; } = new();
    }
}
