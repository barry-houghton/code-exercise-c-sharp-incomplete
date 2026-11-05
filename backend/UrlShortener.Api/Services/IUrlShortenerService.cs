using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    Task<ShortenUrlResponse> ShortenAsync(ShortenUrlRequest request, string baseUrl);
    Task<string?> GetFullUrlAsync(string alias);
    Task<PagedResult<UrlListItem>> GetAllAsync(string baseUrl, int pageIndex = 1, int pageSize = 10, SortOrder sortOrder = SortOrder.CreatedByDesc);
    Task<bool> DeleteAsync(string alias);
}
