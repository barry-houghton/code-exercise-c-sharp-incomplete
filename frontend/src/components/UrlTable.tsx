import type { UrlListItem } from '../types/api';

interface UrlTableProps {
  urls: UrlListItem[];
  onDelete: (alias: string) => void;
}

export function UrlTable({ urls, onDelete }: UrlTableProps) {
  if (urls.length === 0) {
    return <p className="empty-state">No shortened URLs yet.</p>;
  }

  return (
    <div className="url-table-wrapper">
      <table className="url-table">
        <thead>
          <tr>
            <th>Short URL</th>
            <th>Original URL</th>
            <th><span className="sr-only">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          {urls.map((u) => (
            <tr key={u.alias}>
              <td>
                <a href={u.shortUrl} target="_blank" rel="noopener noreferrer" className="short-url-link">
                  {u.shortUrl}
                </a>
              </td>
              <td className="full-url" title={u.fullUrl}>{u.fullUrl}</td>
              <td>
                <button className="btn-danger" onClick={() => onDelete(u.alias)} aria-label={`Delete ${u.alias}`}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}