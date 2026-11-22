using System.ComponentModel.DataAnnotations;
using POVISAPI.Models.Entity.Enums;

namespace POVISAPI.Models.DTO
{
    public class IncentiveFilterDTO
    {
        /// <summary>daily | weekly | monthly | annual | custom (uses From/To). Default: monthly.</summary>
        public string Period { get; set; } = "monthly";

        /// <summary>Any date inside the wanted day/week (Mon-Sun)/month/year. Default: today (Manila).</summary>
        public DateOnly? Date { get; set; }

        /// <summary>Only for Period = custom.</summary>
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        /// <summary>Ignored for Apprehending Officers, who only ever see their own incentives.</summary>
        public Guid? OfficerId { get; set; }
        public Division? Division { get; set; }
        public string? Search { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
        public int Page { get; set; } = 1;

        [Range(1, 200, ErrorMessage = "Page size must be between 1 and 200")]
        public int PageSize { get; set; } = 20;
    }

    /// <summary>One incentive: a paid OVR and what the apprehending officer earned on it.</summary>
    public class IncentiveRowDTO
    {
        public string PaymentId { get; set; } = string.Empty;
        public string ViolationId { get; set; } = string.Empty;
        public DateOnly DateOfPayment { get; set; }
        public string? ReceiptNumber { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Percentage frozen when the payment was recorded (10 = 10%).</summary>
        public decimal IncentiveRate { get; set; }
        public decimal IncentiveAmount { get; set; }

        public string? OfficerId { get; set; }
        public string OfficerName { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;

        // OVR details
        public string OvrNumber { get; set; } = string.Empty;
        public string ViolatorFullName { get; set; } = string.Empty;
        public DateOnly DateOfOccurrence { get; set; }
        public string? PlaceOfOccurrence { get; set; }
        public string Ordinance { get; set; } = string.Empty;
        public string? Section { get; set; }
        public string OffenseLevel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class IncentiveOfficerTotalDTO
    {
        public string? OfficerId { get; set; }
        public string OfficerName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalIncentive { get; set; }
    }

    public class IncentiveListDTO
    {
        public string Period { get; set; } = string.Empty;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalIncentive { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<IncentiveRowDTO> Items { get; set; } = new();
        /// <summary>Per-officer breakdown of the whole filtered period (not just the current page).</summary>
        public List<IncentiveOfficerTotalDTO> OfficerTotals { get; set; } = new();
    }
}
