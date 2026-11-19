/** 1 -> "1st", 2 -> "2nd", 3 -> "3rd", 4 -> "4th", 11 -> "11th", 21 -> "21st"... */
export function ordinal(level: number | null | undefined): string {
  if (level == null) return '';
  const mod100 = level % 100;
  const suffix = mod100 >= 11 && mod100 <= 13 ? 'th' : ({ 1: 'st', 2: 'nd', 3: 'rd' } as Record<number, string>)[level % 10] ?? 'th';
  return `${level}${suffix}`;
}
