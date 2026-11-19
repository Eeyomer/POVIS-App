/**
 * Tiny in-memory cache. Entries live until `clear()` / `delete()` is called (the owning service does that on
 * every add/edit/remove), or until `ttlMs` has passed when one is given. Nothing is persisted: a page reload
 * always starts empty.
 */
export class TtlCache<T> {
  private entries = new Map<string, { value: T; storedAt: number }>();

  /** @param ttlMs lifetime of an entry in milliseconds; omit to keep entries until they are cleared. */
  constructor(private readonly ttlMs?: number) {}

  get(key: string): T | undefined {
    const entry = this.entries.get(key);
    if (!entry) return undefined;
    if (this.ttlMs !== undefined && Date.now() - entry.storedAt > this.ttlMs) {
      this.entries.delete(key);
      return undefined;
    }
    return entry.value;
  }

  set(key: string, value: T): void {
    this.entries.set(key, { value, storedAt: Date.now() });
  }

  delete(key: string): void {
    this.entries.delete(key);
  }

  clear(): void {
    this.entries.clear();
  }
}
