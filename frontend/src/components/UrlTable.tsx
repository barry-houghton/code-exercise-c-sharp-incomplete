import ReactPaginate from 'react-paginate';
import type { UrlListItem } from '../types/api';

interface UrlTableProps {
  urls: UrlListItem[];
  currentPage: number; // Current page (0-indexed)
  pageCount: number; // Total number of pages
  onDelete: (alias: string) => void;
  onPageChange: (page: number) => void;
}

export function UrlTable({ urls, currentPage, pageCount, onDelete, onPageChange }: UrlTableProps) {
  if (urls.length === 0) {
    return <p className="empty-state">No shortened URLs yet.</p>;
  }

  function handlePageClick(selectedItem: { selected: number; }): void {
    onPageChange(selectedItem.selected);
  }

  return (
    <><div className="url-table-wrapper">
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
                  {u.alias}
                </a>
              </td>
              <td>
                <span className="full-url" title={u.fullUrl}>{u.fullUrl}</span>
              </td>
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
    
    <ReactPaginate
      previousLabel={"Previous"}
      nextLabel={"Next"}
      breakLabel={"..."}
      breakClassName={"break-me"}
      pageCount={pageCount} // Total number of pages
      marginPagesDisplayed={2} // How many pages to show at the beginning and end
      pageRangeDisplayed={3} // How many pages to show around the current page
      onPageChange={handlePageClick} // What happens when a page is clicked
      forcePage={currentPage} // This can be used to force a specific page to be selected
      containerClassName={"pagination"} // CSS class for the pagination container
      activeClassName={"active"} // CSS class for the active page
    /></>
  );
}