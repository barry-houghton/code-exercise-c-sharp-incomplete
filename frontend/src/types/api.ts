export interface ShortenUrlRequest {
  fullUrl: string;
  customAlias?: string;
}

export interface ShortenUrlResponse {
  alias: string;
  fullUrl: string;
  shortUrl: string;
}

export interface PagedResponse<T> {
  items: T[];
  pageIndex: number;
  pageSize: number;
  totalCount: number;
}

export const SortDirection = 
{
  'desc': 0,
  'asc': 1
} as const;

export type SortDirection = (typeof SortDirection)[keyof typeof SortDirection]; // 0 | 1

export interface UrlListItem {
  alias: string;
  fullUrl: string;
  shortUrl: string;
  createdAt: string; // ISO 8601 date string
}

export interface ApiError {
  error: string;
}
