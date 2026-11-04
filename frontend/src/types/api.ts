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

export interface UrlListItem {
  alias: string;
  fullUrl: string;
  shortUrl: string;
}

export interface ApiError {
  error: string;
}
