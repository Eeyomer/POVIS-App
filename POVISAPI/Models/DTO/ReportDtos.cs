namespace POVISAPI.Models.DTO
{
    public class OrdinanceSummaryRowDTO
    {
        public string OrdinanceId { get; set; } = string.Empty;
        public string OrdinanceShortName { get; set; } = string.Empty;
        public int FemaleCount { get; set; }
        public int MaleCount { get; set; }
        public int TotalViolators { get; set; }
        public int SeminarCount { get; set; }
        public int CounselingCount { get; set; }
        public int CommunityServiceCount { get; set; }
        public int DidNotComplyCount { get; set; }
        public int PaidCount { get; set; }
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
        public int TotalPaid { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal TotalAmountUnpaid { get; set; }
    }

    public class BarangayOrdinanceRowDTO
    {
        public string BarangayId { get; set; } = string.Empty;
        public string BarangayName { get; set; } = string.Empty;
        public int? District { get; set; }
        public Dictionary<string, int> CountsByOrdinance { get; set; } = new();
        public int Total { get; set; }
    }

    public class PerBarangayReportDTO
    {
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public List<string> OrdinanceColumns { get; set; } = new();
        public List<BarangayOrdinanceRowDTO> Rows { get; set; } = new();
    }
}
