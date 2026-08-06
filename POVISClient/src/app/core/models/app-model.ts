export interface APIResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errorCode?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
