namespace POVISAPI.Models.Entity;

/// <summary>
/// Payment of a fine. Source: PAID sheet (DATE OF PAYMENT, AMOUNT columns) plus
/// remarks like "Paid at CENRO PC No. AA 2447378". One payment per violation;
/// dual-violation tickets (e.g. ₱3,300 = smoking + littering) stay as one payment
/// against a single OVR, with the breakdown noted in Remarks.
/// </summary>
public class Payment : BaseEntity, IArchivable
{
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public Guid ViolationId { get; set; }
    public Violation Violation { get; set; } = null!;

    public DateOnly DateOfPayment { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Official receipt / PC number when available.</summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>Where it was paid (e.g. CENRO, Treasurer's Office).</summary>
    public string? PaidAt { get; set; }

    public string? Remarks { get; set; }

    /// <summary>Incentive percentage (e.g. 10 = 10%) that applied to the apprehending officer when this payment was
    /// recorded. A snapshot: later changes to the system-wide rate never alter it.</summary>
    public decimal IncentiveRate { get; set; }

    /// <summary>Incentive earned on this payment = Amount x IncentiveRate / 100, frozen at the snapshot rate.</summary>
    public decimal IncentiveAmount { get; set; }
}
