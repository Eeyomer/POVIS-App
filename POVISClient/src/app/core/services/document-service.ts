import { Injectable } from '@angular/core';
import { Observable, firstValueFrom } from 'rxjs';
import JSZip from 'jszip';
import { CommunityServiceDTO, CounselingDTO, SeminarRecordDTO, SeminarType, ViolationDTO } from '../models/app-dto';
import { OrdinancesService } from './ordinances-service';
import { PaymentsService } from './payments-service';
import { ViolationsService } from './violations-service';
import { ViolatorsService } from './violators-service';

export interface GeneratedDocument {
  blob: Blob;
  fileName: string;
  title: string;
}

type Fields = Record<string, string | null | undefined>;

/** Shown where the record has no value, so the printed form can still be filled in by hand. */
const BLANK = '____________________';

const SEMINAR_LABELS: Record<SeminarType, { subject: string; name: string }> = {
  SmokingCessation: { subject: 'SMOKING', name: 'Smoking Cessation Seminar' },
  VapingCessation: { subject: 'VAPING', name: 'Vaping Cessation Seminar' },
  SolidWasteManagement: { subject: 'SOLID WASTE MANAGEMENT', name: 'Solid Waste Management Seminar' },
};

/**
 * Fills the Word templates in /templates (their blanks are {{tokens}}) with the data of a seminar,
 * counseling or community service record, entirely in the browser.
 */
@Injectable({ providedIn: 'root' })
export class DocumentService {
  constructor(
    private violations: ViolationsService,
    private violators: ViolatorsService,
    private ordinances: OrdinancesService,
    private payments: PaymentsService,
  ) {}

  seminar(record: SeminarRecordDTO): Promise<GeneratedDocument> {
    const label = SEMINAR_LABELS[record.seminarType];
    return this.build('seminar-endorsement', 'Seminar Endorsement', record.ovrNumber, record.violationId, (c) => ({
      seminarType: label.subject,
      seminarSuffix: record.seminarType === 'SolidWasteManagement' ? ' SEMINAR' : ' CESSATION SEMINAR',
      requiredCompliance: label.name + (record.requiredDays ? ` (${record.requiredDays} day/s)` : ''),
      ...c,
    }));
  }

  counseling(record: CounselingDTO): Promise<GeneratedDocument> {
    return this.build('counseling-request', 'Counseling Request', record.ovrNumber, record.violationId, (c) => c);
  }

  communityService(record: CommunityServiceDTO): Promise<GeneratedDocument> {
    const hours = record.requiredHours ?? null;
    return this.build('community-service-endorsement', 'Community Service Endorsement', record.ovrNumber, record.violationId, (c) => ({
      hoursWords: hours != null ? numberToWords(hours) : null,
      hoursNumber: hours != null ? String(hours) : null,
      ...c,
    }));
  }

  /**
   * Certification of Compliance and Closure. Only valid once the OVR is paid, since it cites the
   * official receipt; it is the same document for a seminar, counseling or community service record.
   */
  async closure(record: { violationId: string; ovrNumber: string }): Promise<GeneratedDocument> {
    const payment = await first(this.payments.getByViolationId(record.violationId));
    if (!payment) throw new Error('This OVR has no recorded payment yet, so a closure certificate cannot be issued.');

    return this.build('closure-certificate', 'Closure Certificate', record.ovrNumber, record.violationId, (c) => ({
      ...c,
      occurrenceTimePlace: [c['occurrenceTime'], c['place']].filter(Boolean).join(', '),
      section: c['section'] ? `Section ${c['section']}` : null,
      receiptNumber: payment.receiptNumber,
      receiptDate: formatDate(payment.dateOfPayment),
      issuedDate: formatIssuedDate(new Date()),
    }));
  }

  /** A blank form template (e.g. the Pledge of Commitment), unfilled, so it can be previewed as-is. */
  async template(file: string, title: string): Promise<GeneratedDocument> {
    const response = await fetch(`templates/${file}.docx`);
    if (!response.ok) throw new Error('The document template could not be loaded.');
    return { blob: await response.blob(), title, fileName: `${title}.docx` };
  }

  download(doc: GeneratedDocument): void {
    const url = URL.createObjectURL(doc.blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = doc.fileName;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }

  private async build(
    template: string,
    title: string,
    ovrNumber: string,
    violationId: string,
    extra: (common: Fields) => Fields,
  ): Promise<GeneratedDocument> {
    const common = await this.commonFields(violationId);
    const blob = await this.fill(`templates/${template}.docx`, extra(common));
    return { blob, title, fileName: `${title} - ${ovrNumber}.docx` };
  }

  /** Fields shared by all three letters, gathered from the violation, violator, ordinance and penalty. */
  private async commonFields(violationId: string): Promise<Fields> {
    const violation = await first(this.violations.getById(violationId));
    if (!violation) throw new Error('The violation of this record could not be loaded.');

    const [violator, ordinance, penalty] = await Promise.all([
      first(this.violators.getById(violation.violatorId)),
      first(this.ordinances.getById(violation.ordinanceId)),
      first(this.violations.getApplicablePenalty(violationId)),
    ]);

    return {
      date: formatDate(new Date()),
      violatorName: violator ? fullName(violator) : violation.violatorFullName,
      violatorAddress: violator ? [violator.address, violator.barangayName, violator.city].filter(Boolean).join(', ') : null,
      contactNumber: violator?.contactNumber,
      ovrNumber: violation.ovrNumber,
      officerName: violation.apprehendingOfficer,
      deputationNumber: violation.apprehendingOfficerDeputationNumber,
      occurrenceDate: formatDate(violation.dateOfOccurrence),
      occurrenceTime: formatTime(violation.timeOfOccurrence),
      place: violation.placeOfOccurrence,
      barangay: violation.barangayOfOccurrenceName,
      ordinanceReference: ordinance
        ? `${ordinance.isExecutiveOrder ? 'Executive Order No.' : 'City Ordinance No.'} ${ordinance.ordinanceNumber}, Series of ${ordinance.series}`
        : null,
      ordinanceNumber: ordinance?.ordinanceNumber,
      ordinanceSeries: ordinance ? String(ordinance.series) : null,
      section: violation.ordinanceSectionCode,
      ordinanceName: ordinance?.shortName ?? violation.ordinanceShortName,
      fineAmount: penalty?.fineAmount != null ? penalty.fineAmount.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : null,
    };
  }

  private async fill(templateUrl: string, fields: Fields): Promise<Blob> {
    const response = await fetch(templateUrl);
    if (!response.ok) throw new Error('The document template could not be loaded.');

    const zip = await JSZip.loadAsync(await response.arrayBuffer());
    const file = zip.file('word/document.xml');
    if (!file) throw new Error('The document template is invalid.');

    const xml = (await file.async('string'))
      // Keep leading/trailing spaces of filled-in text.
      .replace(/<w:t>/g, '<w:t xml:space="preserve">')
      .replace(/\{\{(\w+)\}\}/g, (_, key: string) => escapeXml(fields[key]?.toString().trim() ? fields[key]!.toString() : BLANK));
    zip.file('word/document.xml', xml);

    return zip.generateAsync({
      type: 'blob',
      mimeType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      compression: 'DEFLATE',
    });
  }
}

function first<T>(source: Observable<T>): Promise<T> {
  return firstValueFrom(source);
}

function escapeXml(value: string): string {
  return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function fullName(v: { firstName: string; middleName?: string | null; lastName: string; suffix?: string | null }): string {
  return [v.firstName, v.middleName, v.lastName, v.suffix].filter(Boolean).join(' ');
}

function formatDate(value: Date | string | null | undefined): string | null {
  if (!value) return null;
  const date = typeof value === 'string' ? new Date(`${value.slice(0, 10)}T00:00:00`) : value;
  if (isNaN(date.getTime())) return null;
  return date.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
}

/** 11 Nov 2026 -> "11th day of November 2026" */
function formatIssuedDate(date: Date): string {
  const day = date.getDate();
  const suffix = day % 10 === 1 && day !== 11 ? 'st' : day % 10 === 2 && day !== 12 ? 'nd' : day % 10 === 3 && day !== 13 ? 'rd' : 'th';
  return `${day}${suffix} day of ${date.toLocaleDateString('en-US', { month: 'long' })} ${date.getFullYear()}`;
}

/** "14:30:00" -> "2:30 PM" */
function formatTime(value: string | null | undefined): string | null {
  const match = value?.match(/^(\d{1,2}):(\d{2})/);
  if (!match) return value ?? null;
  const hours = Number(match[1]);
  return `${hours % 12 || 12}:${match[2]} ${hours < 12 ? 'AM' : 'PM'}`;
}

const ONES = ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten', 'eleven', 'twelve',
  'thirteen', 'fourteen', 'fifteen', 'sixteen', 'seventeen', 'eighteen', 'nineteen'];
const TENS = ['', '', 'twenty', 'thirty', 'forty', 'fifty', 'sixty', 'seventy', 'eighty', 'ninety'];

function numberToWords(n: number): string {
  if (!Number.isInteger(n) || n < 0 || n > 999) return String(n);
  if (n < 20) return ONES[n];
  if (n < 100) return TENS[Math.floor(n / 10)] + (n % 10 ? `-${ONES[n % 10]}` : '');
  return `${ONES[Math.floor(n / 100)]} hundred` + (n % 100 ? ` ${numberToWords(n % 100)}` : '');
}
