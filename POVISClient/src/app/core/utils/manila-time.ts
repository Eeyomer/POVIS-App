/** The system runs on Philippine time (Asia/Manila, UTC+8, no DST) regardless of the browser's timezone. */
export const MANILA_TZ = 'Asia/Manila';

/** Offset string for Angular's DatePipe timezone argument. */
export const MANILA_OFFSET = '+0800';

function manilaParts(date: Date): { year: string; month: string; day: string; hour: number } {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: MANILA_TZ,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    hourCycle: 'h23',
  }).formatToParts(date);
  const get = (type: string) => parts.find((p) => p.type === type)?.value ?? '';
  return { year: get('year'), month: get('month'), day: get('day'), hour: Number(get('hour')) };
}

/** Today's date in Manila as yyyy-MM-dd. */
export function manilaToday(now = new Date()): string {
  const p = manilaParts(now);
  return `${p.year}-${p.month}-${p.day}`;
}

/** First day of the current month in Manila as yyyy-MM-dd. */
export function manilaFirstDayOfMonth(now = new Date()): string {
  const p = manilaParts(now);
  return `${p.year}-${p.month}-01`;
}

/** Current hour (0-23) in Manila. */
export function manilaHour(now = new Date()): number {
  return manilaParts(now).hour;
}

export function manilaYear(now = new Date()): number {
  return Number(manilaParts(now).year);
}
