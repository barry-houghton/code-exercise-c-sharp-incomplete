import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { UrlTable } from '../UrlTable';
import type { UrlListItem } from '../../types/api';

const mockUrls: UrlListItem[] = [
  { alias: 'abc', fullUrl: 'https://example.com', shortUrl: 'http://localhost:8080/abc', createdAt: '2026-11-01T10:00:00Z' },
  { alias: 'def', fullUrl: 'https://another.com/path', shortUrl: 'http://localhost:8080/def', createdAt: '2026-11-02T10:00:00Z' },
];

function renderTable(urls: UrlListItem[], onDelete = vi.fn()) {
  return render(
    <UrlTable urls={urls} currentPage={0} pageCount={1} onPageChange={vi.fn()} onDelete={onDelete} />,
  );
}

describe('UrlTable', () => {
  it('shows empty state when there are no URLs', () => {
    renderTable([]);
    expect(screen.getByText(/no shortened urls yet/i)).toBeInTheDocument();
  });

  it('renders a row for each URL, linking the alias to the short URL', () => {
    renderTable(mockUrls);
    expect(screen.getByRole('link', { name: 'abc' })).toHaveAttribute('href', 'http://localhost:8080/abc');
    expect(screen.getByRole('link', { name: 'def' })).toHaveAttribute('href', 'http://localhost:8080/def');
    expect(screen.getByText('https://example.com')).toBeInTheDocument();
    expect(screen.getByText('https://another.com/path')).toBeInTheDocument();
  });

  it('calls onDelete with the alias when delete is clicked', () => {
    const onDelete = vi.fn();
    renderTable(mockUrls, onDelete);
    fireEvent.click(screen.getAllByRole('button', { name: /delete/i })[0]);
    expect(onDelete).toHaveBeenCalledWith('abc');
  });
});
