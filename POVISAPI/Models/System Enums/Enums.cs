namespace POVISAPI.Models.Entity.Enums;

public enum Gender
{
    Male = 1,
    Female = 2
}

/// <summary>Apprehending unit (DIVISION column: BPD, PNP, etc.)</summary>
public enum Division
{
    POD = 1,   // Barangay Police / Public Order Division
    BPD = 2,
    ALD = 3,
    PSD = 4,
    PNP = 5,
    SOG = 6,
    Other = 99
}

/// <summary>Overall state of a violation record. Covers VIOLATORS, WARNING TICKET,
/// PAID, FILED CASE and "DID NOT COMPLY" statuses seen across the sheets.</summary>
public enum ViolationStatus
{
    Pending = 1,            // recorded, not yet resolved
    Paid = 2,
    ForSeminar = 3,         // opted for cessation seminar in lieu of fine
    ForCounseling = 4,      // reach-out minors intervention
    ForCommunityService = 5,
    CaseFiled = 6,
    Dismissed = 7,
    Closed = 8,             // CASE CLOSED (paid after filing, completed seminar, etc.)
    DidNotComply = 9,
    WarningIssued = 10      // WARNING TICKET sheet
}

/// <summary>1st–4th offense (Ord. 04 S.2016 and Ord. 45 S.1995 go up to a 4th offense).</summary>
public enum OffenseLevel
{
    First = 1,
    Second = 2,
    Third = 3,
    Fourth = 4
}

/// <summary>Who the penalty applies to (Ord. 22 S.2002 distinguishes Individual vs
/// Juridical Person; Ord. 74 S.2023 distinguishes Minor / Establishment / Other Individuals).</summary>
public enum OffenderType
{
    Individual = 1,
    JuridicalPerson = 2,
    Minor = 3,
    ParentOrGuardian = 4,
    Establishment = 5
}

/// <summary>Action against a business permit included in some penalties.</summary>
public enum BusinessPermitAction
{
    None = 0,
    Suspension = 1,
    Cancellation = 2
}

public enum SeminarType
{
    SmokingCessation = 1,   // SMOKING SEMINAR sheet
    VapingCessation = 2,    // SEMINAR VAPING sheet
    SolidWasteManagement = 3 // Ord. 01 S.2018 seminar requirement
}

public enum ComplianceStatus
{
    Scheduled = 1,
    Ongoing = 2,
    Completed = 3,
    DidNotComply = 4
}

public enum CaseStatus
{
    Filed = 1,
    Dismissed = 2,
    Closed = 3   // e.g. CASE CLOSED / PAID after prosecution decision
}

/// <summary>
/// MISPersonnel   - manages user accounts and roles, system maintenance, backups, restoration, technical logs.
/// OVRSupervisor  - Supervisor of the OVR Section: manages operational records, reviews/approves submissions,
///                  updates compliance and case status, generates reports.
/// ApprehendingOfficer - searches records and submits new violation information (mobile-compatible).
/// PNPBarangayStaff - PNP/Barangay Staff: authorized read-only resident, violation, and compliance verification.
/// </summary>
public enum Role
{
    MISPersonnel = 1,
    OVRSupervisor = 2,
    ApprehendingOfficer = 3,
    PNPBarangayStaff = 4
}
