import { useState, useCallback, useEffect } from 'react';
import { api } from '../services/api';
import { ShortenUrlResponse, SortDirection, UrlListItem } from '../types/api';

interface UseUrlShortenerReturn {
  urls: UrlListItem[];
  currentPage: number; // Current page (0-indexed)
  pageCount: number; // Total number of pages
  sortDirection: SortDirection;
  loading: boolean;
  error: string | null;
  lastCreated: ShortenUrlResponse | null;
  shorten: (fullUrl: string, customAlias?: string) => Promise<void>;
  deleteUrl: (alias: string) => Promise<void>;
  changePage: (page: number) => void;
  changeSortDirection: (direction: SortDirection) => void;
  clearLastCreated: () => void;
}

export function useUrlShortener(): UseUrlShortenerReturn {
  const [urls, setUrls] = useState<UrlListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [lastCreated, setLastCreated] = useState<ShortenUrlResponse | null>(null);
  const [pageCount, setPageCount] = useState(0); // Total number of pages
  const [currentPage, setCurrentPage] = useState(0); // Current page (0-indexed)
  const [sortDirection, setSortDirection] = useState<SortDirection>(SortDirection.desc); // Default to descending order
  const snipsPerPage = 5; // Number of snips per page

  const fetchAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api.listAll(currentPage, snipsPerPage, sortDirection);
      setUrls(data.items);
      setPageCount(Math.ceil(data.totalCount / data.pageSize));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load URLs.');
    } finally {
      setLoading(false);
    }
  }, [currentPage, sortDirection]);

  useEffect(() => {
    fetchAll();
  }, [currentPage, sortDirection, fetchAll]);

  const shorten = useCallback(async (fullUrl: string, customAlias?: string) => {
    setError(null);
    setLastCreated(null);
    try {
      const result = await api.shorten({ fullUrl, customAlias: customAlias || undefined });
      setLastCreated(result);
      setCurrentPage(0);
      await fetchAll();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to shorten URL.');
    }
  }, [fetchAll]);

  const deleteUrl = useCallback(async (alias: string) => {
    setError(null);
    try {
      await api.delete(alias);
      setUrls((prev) => prev.filter((u) => u.alias !== alias));
      await fetchAll();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to delete URL.');
    }
  }, [fetchAll]);

  const clearLastCreated = useCallback(() => setLastCreated(null), []);

  const changePage = useCallback((page: number) => {
    setCurrentPage(page);
  }, []);

  const changeSortDirection = useCallback((direction: SortDirection) => {
    setSortDirection(direction);
    setCurrentPage(0);
  }, []);

  return { urls, currentPage, pageCount, sortDirection, loading, error, lastCreated, shorten, deleteUrl, changePage, changeSortDirection, clearLastCreated };
}
