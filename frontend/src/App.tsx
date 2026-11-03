import { useUrlShortener } from './hooks/useUrlShortener';
import { ShortenForm } from './components/ShortenForm';
import { ResultBanner } from './components/ResultBanner';
import { UrlTable } from './components/UrlTable';

export default function App() {
  const { urls, loading, error, lastCreated, shorten, deleteUrl, clearLastCreated } =
    useUrlShortener();

  return (
    <div className="app">
      <header className="app-header">
        <div className="app-header__inner">
          <div className="logo">
            <span className="logo__mark" aria-hidden="true">⌁</span>
            <span className="logo__name">Snip</span>
          </div>
          <p className="tagline">Long URLs, made short.</p>
        </div>
      </header>

      <main className="app-main">
        <section className="card shorten-card" aria-labelledby="shorten-heading">
          <h2 id="shorten-heading" className="card__title">
            Shorten a URL
          </h2>
          <ShortenForm onSubmit={shorten} />
        </section>

        {lastCreated && (
          <ResultBanner result={lastCreated} onDismiss={clearLastCreated} />
        )}

        {error && (
          <div className="banner banner--error" role="alert">
            <strong>Error:</strong> {error}
          </div>
        )}

        <section className="card" aria-labelledby="list-heading">
          <h2 id="list-heading" className="card__title">
            All shortened URLs
            {urls.length > 0 && (
              <span className="badge">{urls.length}</span>
            )}
          </h2>
          {loading && urls.length === 0 ? (
            <p className="empty-state">Loading…</p>
          ) : (
            <UrlTable urls={urls} onDelete={deleteUrl} />
          )}
        </section>
      </main>

      <footer className="app-footer">
        <p>Snip - URL Shortener</p>
      </footer>
    </div>
  );
}
