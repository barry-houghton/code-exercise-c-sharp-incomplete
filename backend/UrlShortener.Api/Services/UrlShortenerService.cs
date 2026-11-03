using Microsoft.EntityFrameworkCore;
using Slugify;
using UrlShortener.Api.Data;
using UrlShortener.Api.Exceptions;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public class UrlShortenerService : IUrlShortenerService
{
    private readonly AppDbContext _db;

    public UrlShortenerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShortenUrlResponse> ShortenAsync(ShortenUrlRequest request, string baseUrl)
    {
        var alias = string.IsNullOrWhiteSpace(request.CustomAlias)
            ? GenerateAlias(request.FullUrl)
            : request.CustomAlias.Trim();

        if (!IsValidAlias(alias))
            throw new InvalidAliasException("Alias may only contain letters, numbers, and hyphens (2–64 characters).");

        var exists = await _db.ShortenedUrls.AnyAsync(u => u.Alias == alias);
        if (exists)
            throw new DuplicateAliasException($"The alias '{alias}' is already taken.");

        var entry = new ShortenedUrl
        {
            Alias = alias,
            FullUrl = request.FullUrl
        };

        _db.ShortenedUrls.Add(entry);
        await _db.SaveChangesAsync();

        return new ShortenUrlResponse
        {
            Alias = alias,
            FullUrl = request.FullUrl,
            ShortUrl = $"{baseUrl}/{alias}"
        };
    }

    public async Task<string?> GetFullUrlAsync(string alias)
    {
        var entry = await _db.ShortenedUrls
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Alias == alias);

        return entry?.FullUrl;
    }

    public async Task<IEnumerable<UrlListItem>> GetAllAsync(string baseUrl)
    {
        return await _db.ShortenedUrls
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UrlListItem
            {
                Alias = u.Alias,
                FullUrl = u.FullUrl,
                ShortUrl = $"{baseUrl}/{u.Alias}"
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteAsync(string alias)
    {
        // Method to delete a shortened url from the database based on the alias provided. It returns true if the deletion was successful, and false if no entry was found for the given alias.
        // It first queries the database for an entry with the specified alias. If found, it removes the entry and saves the changes to the database. If not found, it simply returns false.
        // The method uses asynchronous operations to ensure non-blocking database access, which is important for scalability and performance in web applications.
        var snip = await _db.ShortenedUrls.FirstOrDefaultAsync(u => u.Alias == alias);

        if (snip is null)
            return false;

        _db.ShortenedUrls.Remove(snip);
        await _db.SaveChangesAsync();

        return true;
    }

    // --- Private helpers ---

    private static string GenerateAlias(string fullUrl)
    {
        // remove the protocol and slugify the fullUrl
        if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
            throw new InvalidUrlException("The URL is invalid.");

        var urlWithoutProtocol = uri.Host + uri.PathAndQuery;

        var slug = new SlugHelper(
            new SlugHelperConfiguration
            {
                StringReplacements = { { ".", "-"} },
                MaximumLength = 64,
            })
            .GenerateSlug(urlWithoutProtocol);

        return slug;
    }

    private static bool IsValidAlias(string alias)
    {
        if (alias.Length < 2 || alias.Length > 64)
            return false;

        return alias.All(c => char.IsLetterOrDigit(c) || c == '-');
    }
}
