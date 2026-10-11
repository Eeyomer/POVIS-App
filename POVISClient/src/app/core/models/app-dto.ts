// ── Enums ────────────────────────────────────────────────────────────────
export type Gender = 'Male' | 'Female';
export type Division = 'Bpd' | 'Pnp' | 'Cenro' | 'Other';
export type ViolationStatus =
  | 'Pending' | 'Paid' | 'ForSeminar' | 'ForCounseling' | 'ForCommunityService'
  | 'CaseFiled' | 'Dismissed' | 'Closed' | 'DidNotComply' | 'WarningIssued';
export type OffenseLevel = 'First' | 'Second' | 'Third' | 'Fourth';
export type OffenderType = 'Individual' | 'JuridicalPerson' | 'Minor' | 'ParentOrGuardian' | 'Establishment';
export type BusinessPermitAction = 'None' | 'Suspension' | 'Cancellation';
export type SeminarType = 'SmokingCessation' | 'VapingCessation' | 'SolidWasteManagement';
export type ComplianceStatus = 'Scheduled' | 'Ongoing' | 'Completed' | 'DidNotComply';
export type CaseStatus = 'Filed' | 'Dismissed' | 'Closed';

// ── Barangays ────────────────────────────────────────────────────────────
export interface BarangayDTO {
  barangayId: string;
  name: string;
  district?: number | null;
  isActive: boolean;
}

export interface AddBarangayDTO {
  name: string;
  district?: number | null;
}

export interface UpdateBarangayDTO {
  barangayId: string;
  name: string;
  district?: number | null;
  isActive: boolean;
}

// ── Violators ────────────────────────────────────────────────────────────
export interface ViolatorDTO {
  violatorId: string;
  lastName: string;
  firstName: string;
  middleName?: string | null;
  suffix?: string | null;
  gender: Gender;
  birthday?: string | null;
  address?: string | null;
  barangayId?: string | null;
  barangayName?: string | null;
  city?: string | null;
  presentedValidId?: string | null;
  contactNumber?: string | null;
  violationCount: number;
}

export interface AddViolatorDTO {
  lastName: string;
  firstName: string;
  middleName?: string | null;
  suffix?: string | null;
  gender: Gender;
  birthday?: string | null;
  address?: string | null;
  barangayId?: string | null;
  city?: string | null;
  presentedValidId?: string | null;
  contactNumber?: string | null;
}

export interface UpdateViolatorDTO extends AddViolatorDTO {
  violatorId: string;
}

export interface ViolatorHistoryDTO {
  violationId: string;
  ovrNumber: string;
  dateOfOccurrence: string;
  ordinanceShortName: string;
  offenseLevel: OffenseLevel;
  status: ViolationStatus;
}

// ── Violations ───────────────────────────────────────────────────────────
export interface ViolationDTO {
  violationId: string;
  violatorId: string;
  violatorFullName: string;
  ovrNumber: string;
  dateOfSubmission: string;
  division: Division;
  ageAtApprehension?: number | null;
  dateOfOccurrence: string;
  timeOfOccurrence?: string | null;
  placeOfOccurrence?: string | null;
  barangayOfOccurrenceId?: string | null;
  barangayOfOccurrenceName?: string | null;
  witness?: string | null;
  apprehendingOfficer?: string | null;
  ordinanceId: string;
  ordinanceShortName: string;
  ordinanceSectionId?: string | null;
  ordinanceSectionCode?: string | null;
  offenseLevel: OffenseLevel;
  isWarningTicket: boolean;
  status: ViolationStatus;
  remarks?: string | null;
  remarks2?: string | null;
  hasPayment: boolean;
  hasFiledCase: boolean;
  isReviewed: boolean;
  reviewedAt?: string | null;
  reviewedBy?: string | null;
}

export interface AddViolationDTO {
  violatorId: string;
  ovrNumber: string;
  dateOfSubmission: string;
  division: Division;
  ageAtApprehension?: number | null;
  dateOfOccurrence: string;
  timeOfOccurrence?: string | null;
  placeOfOccurrence?: string | null;
  barangayOfOccurrenceId?: string | null;
  witness?: string | null;
  apprehendingOfficer?: string | null;
  ordinanceId: string;
  ordinanceSectionId?: string | null;
  isWarningTicket: boolean;
  remarks?: string | null;
  remarks2?: string | null;
}

export interface UpdateViolationDTO {
  violationId: string;
  dateOfSubmission: string;
  division: Division;
  ageAtApprehension?: number | null;
  dateOfOccurrence: string;
  timeOfOccurrence?: string | null;
  placeOfOccurrence?: string | null;
  barangayOfOccurrenceId?: string | null;
  witness?: string | null;
  apprehendingOfficer?: string | null;
  ordinanceSectionId?: string | null;
  status: ViolationStatus;
  remarks?: string | null;
  remarks2?: string | null;
}

export interface ViolationFilterDTO {
  ordinanceId?: string | null;
  barangayId?: string | null;
  status?: ViolationStatus | null;
  dateFrom?: string | null;
  dateTo?: string | null;
  search?: string | null;
  page: number;
  pageSize: number;
}

export interface ApplicablePenaltyDTO {
  offenseLevel: OffenseLevel;
  fineAmount?: number | null;
  penaltyText: string;
}

// ── Ordinances ───────────────────────────────────────────────────────────
export interface OrdinancePenaltyDTO {
  ordinancePenaltyId: string;
  ordinanceSectionId?: string | null;
  offenseLevel: OffenseLevel;
  offenderType?: OffenderType | null;
  fineAmount?: number | null;
  seminarDays?: number | null;
  communityServiceHours?: number | null;
  communityServiceDays?: number | null;
  requiresCounseling: boolean;
  hasImprisonment: boolean;
  imprisonmentTerm?: string | null;
  businessPermitAction: BusinessPermitAction;
  isConjunctive: boolean;
  penaltyText: string;
}

export interface AddOrdinancePenaltyDTO {
  ordinanceSectionId?: string | null;
  offenseLevel: OffenseLevel;
  offenderType?: OffenderType | null;
  fineAmount?: number | null;
  seminarDays?: number | null;
  communityServiceHours?: number | null;
  communityServiceDays?: number | null;
  requiresCounseling: boolean;
  hasImprisonment: boolean;
  imprisonmentTerm?: string | null;
  businessPermitAction: BusinessPermitAction;
  isConjunctive: boolean;
  penaltyText: string;
}

export interface OrdinanceSectionDTO {
  ordinanceSectionId: string;
  sectionCode: string;
  description?: string | null;
  penalties: OrdinancePenaltyDTO[];
}

export interface AddOrdinanceSectionDTO {
  sectionCode: string;
  description?: string | null;
}

export interface OrdinanceDTO {
  ordinanceId: string;
  ordinanceNumber: string;
  series: number;
  title: string;
  shortName: string;
  isExecutiveOrder: boolean;
  isActive: boolean;
  sections: OrdinanceSectionDTO[];
  penalties: OrdinancePenaltyDTO[];
}

export interface OrdinanceListDTO {
  ordinanceId: string;
  ordinanceNumber: string;
  series: number;
  shortName: string;
  isActive: boolean;
}

export interface OrdinanceImportErrorDTO {
  row: number;
  message: string;
}

export interface OrdinanceImportResultDTO {
  rowsRead: number;
  ordinancesCreated: number;
  ordinancesExisting: number;
  sectionsCreated: number;
  penaltiesCreated: number;
  penaltiesSkipped: number;
  errors: OrdinanceImportErrorDTO[];
}

export interface AddOrdinanceDTO {
  ordinanceNumber: string;
  series: number;
  title: string;
  shortName: string;
  isExecutiveOrder: boolean;
}

export interface UpdateOrdinanceDTO {
  ordinanceId: string;
  ordinanceNumber: string;
  series: number;
  title: string;
  shortName: string;
  isExecutiveOrder: boolean;
  isActive: boolean;
}

// ── Filed Cases ──────────────────────────────────────────────────────────
export interface FiledCaseDTO {
  filedCaseId: string;
  violationId: string;
  ovrNumber: string;
  violatorFullName: string;
  dateFiled: string;
  npsDocketNumber?: string | null;
  status: CaseStatus;
  dateResolved?: string | null;
  resolutionRemarks?: string | null;
  remarks?: string | null;
}

export interface AddFiledCaseDTO {
  violationId: string;
  dateFiled: string;
  npsDocketNumber?: string | null;
  remarks?: string | null;
}

export interface UpdateFiledCaseDTO {
  filedCaseId: string;
  dateFiled: string;
  npsDocketNumber?: string | null;
  status: CaseStatus;
  dateResolved?: string | null;
  resolutionRemarks?: string | null;
  remarks?: string | null;
}

// ── Payments ─────────────────────────────────────────────────────────────
export interface PaymentDTO {
  paymentId: string;
  violationId: string;
  ovrNumber: string;
  violatorFullName: string;
  dateOfPayment: string;
  amount: number;
  receiptNumber?: string | null;
  paidAt?: string | null;
  remarks?: string | null;
}

export interface AddPaymentDTO {
  violationId: string;
  dateOfPayment: string;
  amount: number;
  receiptNumber?: string | null;
  paidAt?: string | null;
  remarks?: string | null;
}

export interface UpdatePaymentDTO {
  paymentId: string;
  dateOfPayment: string;
  amount: number;
  receiptNumber?: string | null;
  paidAt?: string | null;
  remarks?: string | null;
}

// ── Seminar Records ──────────────────────────────────────────────────────
export interface SeminarRecordDTO {
  seminarRecordId: string;
  violationId: string;
  ovrNumber: string;
  violatorFullName: string;
  seminarType: SeminarType;
  scheduledDate?: string | null;
  requiredDays?: number | null;
  status: ComplianceStatus;
  dateCompleted?: string | null;
  remarks?: string | null;
}

export interface AddSeminarRecordDTO {
  violationId: string;
  seminarType: SeminarType;
  scheduledDate?: string | null;
  requiredDays?: number | null;
  remarks?: string | null;
}

export interface UpdateSeminarRecordDTO {
  seminarRecordId: string;
  seminarType: SeminarType;
  scheduledDate?: string | null;
  requiredDays?: number | null;
  status: ComplianceStatus;
  dateCompleted?: string | null;
  remarks?: string | null;
}

// ── Community Service Records ────────────────────────────────────────────
export interface CommunityServiceDTO {
  communityServiceRecordId: string;
  violationId: string;
  ovrNumber: string;
  violatorFullName: string;
  requiredHours?: number | null;
  requiredDays?: number | null;
  scheduledDate?: string | null;
  dateCompleted?: string | null;
  status: ComplianceStatus;
  assignedLocation?: string | null;
  remarks?: string | null;
}

export interface AddCommunityServiceDTO {
  violationId: string;
  requiredHours?: number | null;
  requiredDays?: number | null;
  scheduledDate?: string | null;
  assignedLocation?: string | null;
  remarks?: string | null;
}

export interface UpdateCommunityServiceDTO {
  communityServiceRecordId: string;
  requiredHours?: number | null;
  requiredDays?: number | null;
  scheduledDate?: string | null;
  dateCompleted?: string | null;
  status: ComplianceStatus;
  assignedLocation?: string | null;
  remarks?: string | null;
}

// ── Counseling Records ───────────────────────────────────────────────────
export interface CounselingDTO {
  counselingRecordId: string;
  violationId: string;
  ovrNumber: string;
  violatorFullName: string;
  interventionType?: string | null;
  conductedBy?: string | null;
  scheduledDate?: string | null;
  dateCompleted?: string | null;
  status: ComplianceStatus;
  parentOrGuardianPresent: boolean;
  remarks?: string | null;
}

export interface AddCounselingDTO {
  violationId: string;
  interventionType?: string | null;
  conductedBy?: string | null;
  scheduledDate?: string | null;
  parentOrGuardianPresent: boolean;
  remarks?: string | null;
}

export interface UpdateCounselingDTO {
  counselingRecordId: string;
  interventionType?: string | null;
  conductedBy?: string | null;
  scheduledDate?: string | null;
  dateCompleted?: string | null;
  status: ComplianceStatus;
  parentOrGuardianPresent: boolean;
  remarks?: string | null;
}

// ── Auth ─────────────────────────────────────────────────────────────────
export type Role = 'MISPersonnel' | 'OVRSupervisor' | 'ApprehendingOfficer' | 'PNPBarangayStaff';

export const ROLE_LABELS: Record<Role, string> = {
  MISPersonnel: 'MIS Personnel',
  OVRSupervisor: 'Supervisor of the OVR Section',
  ApprehendingOfficer: 'Apprehending Officer',
  PNPBarangayStaff: 'PNP/Barangay Staff',
};

export interface LoginDTO {
  username: string;
  password: string;
}

export interface RegisterDTO {
  username: string;
  password: string;
  role: Role;
}

export interface User {
  id: string;
  username: string;
  role: Role;
  isActive: boolean;
  deputationNumber?: string | null;
  createdAt: string;
}

export interface CreateUserDTO {
  username: string;
  password: string;
  role: Role;
  deputationNumber?: string | null;
}

export interface UpdateUserDTO {
  id: string;
  role: Role;
  isActive: boolean;
  deputationNumber?: string | null;
}

export interface ResetPasswordDTO {
  newPassword: string;
}

// ── Reports ──────────────────────────────────────────────────────────────
export interface OrdinanceSummaryRow {
  ordinanceId: string;
  ordinanceShortName: string;
  femaleCount: number;
  maleCount: number;
  totalViolators: number;
  seminarCount: number;
  counselingCount: number;
  communityServiceCount: number;
  didNotComplyCount: number;
  paidCount: number;
  totalAmountPaid: number;
  totalAmountUnpaid: number;
}

export interface OrdinanceSummaryReport {
  from: string;
  to: string;
  rows: OrdinanceSummaryRow[];
  totalFemale: number;
  totalMale: number;
  totalViolators: number;
  totalPaid: number;
  totalAmountPaid: number;
  totalAmountUnpaid: number;
}

export interface BarangayOrdinanceRow {
  barangayId: string;
  barangayName: string;
  district?: number | null;
  countsByOrdinance: Record<string, number>;
  total: number;
}

export interface PerBarangayReport {
  from: string;
  to: string;
  ordinanceColumns: string[];
  rows: BarangayOrdinanceRow[];
}

// ── System (MIS Personnel) ──────────────────────────────────────────────
export interface SystemHealth {
  databaseConnected: boolean;
  serverTimeUtc: string;
  totalUsers: number;
  totalViolators: number;
  totalViolations: number;
  totalTechnicalLogs: number;
}

export interface TechnicalLog {
  id: string;
  timestamp: string;
  method: string;
  path: string;
  statusCode: number;
  durationMs: number;
  username?: string | null;
  ipAddress?: string | null;
}

export interface TechnicalLogFilter {
  search?: string | null;
  method?: string | null;
  statusCode?: number | null;
  from?: string | null;
  to?: string | null;
  page?: number;
  pageSize?: number;
}
